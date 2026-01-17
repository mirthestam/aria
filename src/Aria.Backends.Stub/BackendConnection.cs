using Aria.Core.Connection;
using Aria.Infrastructure.Connection;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Backends.Stub;

public class BackendConnection(Player player, Queue queue, Library library) : BaseBackendConnection(player, queue, library)
{
    public const int ConnectDelay = 1;
    public const int Delay = 1;

    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        OnConnectionStateChanged(ConnectionState.Connecting);
        await Task.Delay(ConnectDelay, cancellationToken).ConfigureAwait(false);
        OnConnectionStateChanged(ConnectionState.Connected);
    }
}