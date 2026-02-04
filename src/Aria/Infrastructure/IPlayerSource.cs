using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;

namespace Aria.Infrastructure;

public interface IPlayerSource : IPlayer
{
    event EventHandler<PlayerStateChangedEventArgs>? StateChanged;    
}

public interface IQueueSource : IQueue
{
    event EventHandler<QueueStateChangedEventArgs>? StateChanged;    
}

public interface ILibrarySource : ILibrary
{
    event EventHandler? Updated;    
}