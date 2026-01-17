using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Aria.Infrastructure.Tagging;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Infrastructure;

public abstract class BaseBackendConnection(
    IPlayer player, 
    IQueue queue, 
    IMessenger messenger,
    ILibrary library) : IBackendConnection
{
    public virtual bool IsConnected => false;

    public IPlayer Player => player;
    public IQueue Queue => queue;
    public ILibrary Library => library;
    public IMessenger Messenger => messenger;
    public ITagParser TagParser { get; protected set; } = null!;

    public virtual async Task InitializeAsync()
    {
        UpdateConnectionState(ConnectionState.Connecting);
        await Task.Delay(500);
        UpdateConnectionState(ConnectionState.Connected);
    }

    public virtual async Task DisconnectAsync()
    {
        await Task.CompletedTask;
        UpdateConnectionState(ConnectionState.Disconnected);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void SetTagParser(ITagParser tagParser)
    {
        ArgumentNullException.ThrowIfNull(tagParser);
        TagParser = tagParser;
    }
    
    protected void UpdateConnectionState(ConnectionState state)
    {
        messenger.Send(new ConnectionChangedMessage(state));
    }    
}