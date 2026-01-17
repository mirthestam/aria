using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;

namespace Aria.Core;

public interface IAria
{
    public IPlayer PlayerProxy { get; }

    public IQueue QueueProxy { get; }

    public ILibrary LibraryProxy { get; }
}