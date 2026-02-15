using Aria.Core.Extraction;
using Aria.Infrastructure;
using Aria.Infrastructure.Inspection;

namespace Aria.Core.Connection;

public interface IBackendConnection : IDisposable
{
    IPlayerSource Player { get; }

    IQueueSource Queue { get; }

    ILibrarySource Library { get; }
    
    IIdProvider IdProvider { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync();
    
    public event Action<BackendConnectionState>? ConnectionStateChanged;
}