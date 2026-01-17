using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;

namespace Aria.Infrastructure;

public interface IPlayerSource : IPlayer
{
    event Action<PlayerStateChangedFlags>? StateChanged;    
}

public interface IQueueSource : IQueue
{
    event Action<QueueStateChangedFlags>? StateChanged;    
}

public interface ILibrarySource : ILibrary
{
    event Action? Updated;    
}