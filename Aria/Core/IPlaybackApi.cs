using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;

namespace Aria.Core;

public interface IPlaybackApi
{
    public IPlayer Player { get; }

    public IPlaylist Playlist { get; }

    public ILibrary Library { get; }
}