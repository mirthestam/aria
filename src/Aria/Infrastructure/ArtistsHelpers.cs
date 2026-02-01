using Aria.Core.Library;

namespace Aria.Infrastructure;

public static class SharedArtistHelper
{
    public static IEnumerable<TrackArtistInfo> GetSharedArtists(IReadOnlyList<AlbumTrackInfo> tracks)
    {
        if (tracks.Count == 0) return [];

        return tracks
                   .Select(t => t.Track.CreditsInfo.Artists)
                   .Aggregate((IEnumerable<TrackArtistInfo>?)null, (common, current) =>
                       common == null
                           ? current
                           : common.IntersectBy(current.Select(a => a.Artist.Id), a => a.Artist.Id))
               ?? [];
    }

    public static IEnumerable<TrackArtistInfo> GetUniqueSongArtists(TrackInfo trackInfo,
        List<AlbumTrackInfo> albumTracks)
    {
        var sharedGuestArtists = GetSharedArtists(albumTracks);

        return trackInfo.CreditsInfo.Artists
            .ExceptBy(sharedGuestArtists.Select(s => s.Artist.Id), a => a.Artist.Id);
    }
}

public static class AlbumCreditExtensions
{
    extension(AlbumInfo albumInfo)
    {
        [Obsolete]
        public IEnumerable<TrackArtistInfo> GetSharedArtists()
        {
            if (albumInfo.Tracks.Count == 0) return [];

            return albumInfo.Tracks
                       .Select(t => t.Track.CreditsInfo.Artists)
                       .Aggregate((IEnumerable<TrackArtistInfo>?)null, (common, current) =>
                           common == null
                               ? current
                               : common.IntersectBy(current.Select(a => a.Artist.Id), a => a.Artist.Id))
                   ?? [];
        }

        [Obsolete]
        public IEnumerable<TrackArtistInfo> GetSharedGuestArtists()
        {
            var albumArtistIds = albumInfo.CreditsInfo.AlbumArtists.Select(aa => aa.Id).ToHashSet();

            return albumInfo.GetSharedArtists()
                .Where(ca => !albumArtistIds.Contains(ca.Artist.Id));
        }
    }
}