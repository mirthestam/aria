using Aria.Core.Library;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Browser.Playlists;

[Subclass<Object>]
public partial class PlaylistModel
{
    public static PlaylistModel NewForPlaylistInfo(PlaylistInfo playlist)
    {
        var model = NewWithProperties([]);
        model.Playlist = playlist;
        return model;
    }

    public PlaylistInfo Playlist { get; private set; }
}