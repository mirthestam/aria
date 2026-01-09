using CodeProject.ObjectPool;
using MpcNET;

namespace Aria.MusicServers.MPD;

public record CommandResult<T>(bool IsSuccess, T? Content);

public sealed class ConnectionScope(PooledObjectWrapper<MpcConnection> wrapper) : IDisposable
{
    private bool _isDisposed;

    public async Task<CommandResult<T>> SendCommandAsync<T>(IMpcCommand<T> command)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(ConnectionScope));
                
        var response = await wrapper.InternalResource.SendAsync(command);
        
        return response is { IsResponseValid: true }
            ? new CommandResult<T>(true, response.Response.Content)
            : new CommandResult<T>(false, default);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        wrapper.Dispose(); 
        _isDisposed = true;
    }
}