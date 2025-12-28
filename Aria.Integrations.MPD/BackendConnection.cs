using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using Aria.MusicServers.MPD.Events;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.MusicServers.MPD;

/// <summary>
/// The MPD implementation of a backend connection
/// </summary>
public sealed class BackendConnection : Infrastructure.BackendConnection
{
    private readonly ILogger<BackendConnection> _logger;
    private readonly Library _library;
    private readonly IMessenger _messenger; // TODO use events instead of messaging the infrastructure and backend layers
    private readonly Player _player;
    private readonly Playlist _playlist;
    private readonly Session _session = new();
    
    public BackendConnection(IMessenger messenger, ITagParser tagParser, ILogger<BackendConnection> logger)
    {
        _messenger = messenger;
        TagParser = tagParser;
        _logger = logger;

        _player = new Player(_session, _messenger);
        _playlist = new Playlist(_session);
        _library = new Library(_session, tagParser);
    }

    public override bool IsConnected => _session.IsConnected;

    public override IPlayer Player => _player;

    public override IPlaylist Playlist => _playlist;

    public override ILibrary Library => _library;

    public override ITagParser TagParser { get; }

    public void SetCredentials(Credentials credentials)
    {
        _session.Credentials = credentials;
    }

    public override async Task InitializeAsync()
    {
        _session.ConnectionChanged += (a, b) =>
        {
            ConnectionState state;
            if (_session.IsConnected)
                state = ConnectionState.Connected;
            else if (_session.IsConnecting)
                state = ConnectionState.Connecting;
            else
                state = ConnectionState.Disconnected;

            _messenger.Send(new ConnectionChangedMessage(state));
        };

        _session.IdleResponseReceived += SessionOnIdleResponseReceived;
        _session.StatusChanged += SessionOnStatusChanged;

        await _session.InitializeAsync();
    }

    private async void SessionOnStatusChanged(object? sender, StatusChangedEventArgs e)
    {
        try
        {
            await _player.UpdateFromStatusAsync(e.Status);
            _playlist.UpdateFromStatus(e.Status);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error updating player and playlist from status");
        }
    }

    private void SessionOnIdleResponseReceived(object? sender, IdleResponseEventArgs e)
    {
        var subsystems = e.Message;
        if (subsystems.Contains("playlist"))
        {
            // Queue has changed
            // OnQueueChanged(EventArgs.Empty);
            // TODO: When implementing the playlist, this is where I will get notified on changes
        }

        if (subsystems.Contains("stored_playlist"))
        {
            //The moment when we will support stored playlists, this is where we will get updates.
        }

        if (!subsystems.Contains("player") && !subsystems.Contains("mixer") && !subsystems.Contains("output") &&
            !subsystems.Contains("options") && !subsystems.Contains("update")) return;

        if (subsystems.Contains("update"))
            // The library has been updated.
            // TODO: Use events here instead of messaging
            _messenger.Send(new LibraryUpdatedMessage());
        
        _ = _session.UpdateStatusAsync(ConnectionType.Idle);
    }
}