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
public partial class BackendConnection(
    IMessenger messenger,
    ILogger<BackendConnection> logger,
    ITagParser tagParser,
    Player player,
    Queue queue,
    Session session,
    Library library)
    : Infrastructure.BackendConnection
{
    public override bool IsConnected => session.IsConnected;

    public override IPlayer Player => player;

    public override IQueue Queue => queue;

    public override ILibrary Library => library;

    public override ITagParser TagParser { get; } = tagParser;

    public void SetCredentials(Credentials credentials)
    {
        session.Credentials = credentials;
    }

    public override async Task InitializeAsync()
    {
        session.ConnectionChanged += (a, b) =>
        {
            ConnectionState state;
            if (session.IsConnected)
                state = ConnectionState.Connected;
            else if (session.IsConnecting)
                state = ConnectionState.Connecting;
            else
                state = ConnectionState.Disconnected;

            messenger.Send(new ConnectionChangedMessage(state));
        };

        session.IdleResponseReceived += SessionOnIdleResponseReceived;
        session.StatusChanged += SessionOnStatusChanged;

        await session.InitializeAsync();
    }

    public override async Task DisconnectAsync()
    {
        await session.DisconnectAsync();
    }

    private async void SessionOnStatusChanged(object? sender, StatusChangedEventArgs e)
    {
        try
        {
            await player.UpdateFromStatusAsync(e.Status);
            await queue.UpdateFromStatusAsync(e.Status);
        }
        catch (Exception exception)
        {
            LogErrorUpdatingPlayerAndPlaylistFromStatus(exception);
        }
    }

    private void SessionOnIdleResponseReceived(object? sender, IdleResponseEventArgs e)
    {
        var subsystems = e.Message;

        if (!subsystems.Contains("playlist") &&  !subsystems.Contains("player") && !subsystems.Contains("mixer") && !subsystems.Contains("output") &&
            !subsystems.Contains("options") && !subsystems.Contains("update")) return;

        if (subsystems.Contains("playlist"))
        {
            // Playlist changes!
        }

        if (subsystems.Contains("update"))
            messenger.Send(new LibraryUpdatedMessage());
        
        _ = session.UpdateStatusAsync(ConnectionType.Idle);
    }

    [LoggerMessage(LogLevel.Error, "Error updating player and playlist from status")]
    partial void LogErrorUpdatingPlayerAndPlaylistFromStatus(Exception e);
}