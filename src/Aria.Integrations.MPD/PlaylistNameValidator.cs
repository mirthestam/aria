using Aria.Core.Library;
using Aria.Features.Browser.Playlists;

namespace Aria.Backends.MPD;

public class PlaylistNameValidator : IPlaylistNameValidator
{
    public bool Validate(string name)
    {
        return !string.IsNullOrWhiteSpace(name);
    }
}