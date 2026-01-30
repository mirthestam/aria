using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Infrastructure.Connection;

public abstract class BaseBackendConnection(
    IPlayerSource player,
    IQueueSource queue,
    ILibrarySource library,
    IIdProvider idProvider) : IBackendConnection
{
    public virtual event Action<ConnectionState>? ConnectionStateChanged;    
    
    public virtual bool IsConnected => false;
    public ITagParser TagParser { get; protected set; } = null!;

    public IPlayerSource Player => player;
    public IQueueSource Queue => queue;
    public ILibrarySource Library => library;
    public IIdProvider IdProvider => idProvider;

    public virtual Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        OnConnectionStateChanged(ConnectionState.Connecting);
        OnConnectionStateChanged(ConnectionState.Connected);
        
        return Task.CompletedTask;
    }

    public virtual Task DisconnectAsync()
    {
        OnConnectionStateChanged(ConnectionState.Disconnected);

        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Sets the tag parser for the backend connection. This parser is used to extract
    /// metadata information from track tags.
    /// </summary>
    public void SetTagParser(ITagParser tagParser)
    {
        ArgumentNullException.ThrowIfNull(tagParser);
        TagParser = tagParser;
    }
    
    protected void OnConnectionStateChanged(ConnectionState flags)
    {
        ConnectionStateChanged?.Invoke(flags);
    }        

    ~BaseBackendConnection()
    {
        Console.WriteLine("BackendConnection is FINALIZED");
    }
}