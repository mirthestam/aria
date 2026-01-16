using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;

namespace Aria.Core;

public interface IPlaybackApi
{
    public IPlayer Player { get; }

    public IQueue Queue { get; }

    public ILibrary Library { get; }
}