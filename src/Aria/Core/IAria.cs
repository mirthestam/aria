using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;

namespace Aria.Core;

public interface IAria
{
    public IPlayer Player { get; }

    public IQueue Queue { get; }

    public ILibrary Library { get; }
}