using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure.Connection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Backends.MPD.Connection;

/// <summary>
/// The MPD implementation of a backend connection
/// </summary>
public partial class BackendConnection(
    ILogger<BackendConnection> logger,
    Player player,
    Queue queue,
    Client client,
    Library library,
    IIdProvider idProvider)
    : BaseBackendConnection( player, queue, library, idProvider)
{
    public override bool IsConnected => client.IsConnected;

    ~BackendConnection()
    {
        Console.WriteLine("MPDBackendConnection is FINALIZED");
    }    
    
    public void SetCredentials(ConnectionConfig connectionConfig)
    {
        client.Config = connectionConfig;
    }
    
    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        client.ConnectionChanged += (a, b) =>
        {
            BackendConnectionState state;
            if (client.IsConnected)
                state = BackendConnectionState.Connected;
            else if (client.IsConnecting)
                state = BackendConnectionState.Connecting;
            else
                state = BackendConnectionState.Disconnected;

            OnConnectionStateChanged(state);
        };

        client.IdleResponseReceived += SessionOnIdleResponseReceived;
        client.StatusChanged += SessionOnStatusChanged;

        await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task DisconnectAsync()
    {
        await client.DisconnectAsync().ConfigureAwait(false);
    }

    private async void SessionOnStatusChanged(object? sender, StatusChangedEventArgs e)
    {
        try
        {
            await player.UpdateFromStatusAsync(e.Status).ConfigureAwait(false);
            await queue.UpdateFromStatusAsync(e.Status).ConfigureAwait(false);
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
            library.ServerUpdated();
        
        _ = client.UpdateStatusAsync(ConnectionType.Idle);
    }

    [LoggerMessage(LogLevel.Error, "Error updating player and playlist from status")]
    partial void LogErrorUpdatingPlayerAndPlaylistFromStatus(Exception e);
}