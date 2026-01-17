using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Infrastructure;

namespace Aria.Core.Connection;

public interface IBackendConnection : IDisposable
{
    IPlayerSource Player { get; }

    IQueueSource Queue { get; }

    ILibrarySource Library { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync();
    
    public event Action<ConnectionState>? ConnectionStateChanged;    
}