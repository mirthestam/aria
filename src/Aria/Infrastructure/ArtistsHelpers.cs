using Aria.Core.Library;

namespace Aria.Infrastructure;

public static class AlbumCreditExtensions
{
    extension(AlbumInfo albumInfo)
    {
        /// <summary>
        /// Gets the artists that are common to all tracks in the album
        /// </summary>
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

        public IEnumerable<TrackArtistInfo> GetSharedGuestArtists()
        {
            var albumArtistIds = albumInfo.CreditsInfo.AlbumArtists.Select(aa => aa.Id).ToHashSet();

            return albumInfo.GetSharedArtists()
                .Where(ca => !albumArtistIds.Contains(ca.Artist.Id));
        }

        public IEnumerable<TrackArtistInfo> GetUniqueSongArtists(TrackInfo trackInfo)
        {
            var sharedGuestArtists = albumInfo.GetSharedGuestArtists();
            var albumArtistIds = albumInfo.CreditsInfo.AlbumArtists.Select(aa => aa.Id).ToHashSet();

            return trackInfo.CreditsInfo.Artists
                .ExceptBy(sharedGuestArtists.Select(s => s.Artist.Id), a => a.Artist.Id)
                .Where(a => !albumArtistIds.Contains(a.Artist.Id));
        }
    }
}