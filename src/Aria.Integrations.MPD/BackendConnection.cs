using Aria.Core.Library;
using Aria.Infrastructure;
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
    Player player,
    Queue queue,
    Session session,
    Library library)
    : BaseBackendConnection( player, queue, messenger, library)
{
    public override bool IsConnected => session.IsConnected;

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

            UpdateConnectionState(state);
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
            Messenger.Send(new LibraryUpdatedMessage());
        
        _ = session.UpdateStatusAsync(ConnectionType.Idle);
    }

    [LoggerMessage(LogLevel.Error, "Error updating player and playlist from status")]
    partial void LogErrorUpdatingPlayerAndPlaylistFromStatus(Exception e);
}