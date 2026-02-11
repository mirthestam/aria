using System.Linq;
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

        var topArtists = playlist.Tracks
            .SelectMany(track => track.Track.CreditsInfo.Artists.Select(artist => artist.Artist.Name))
            .GroupBy(artist => artist)
            .OrderByDescending(group => group.Count())
            .Take(3)
            .Select(group => group.Key);

        model.Credits = string.Join(", ", topArtists);

        return model;
    }

    public PlaylistInfo Playlist { get; private set; }

    public string Credits { get; private set; }
}