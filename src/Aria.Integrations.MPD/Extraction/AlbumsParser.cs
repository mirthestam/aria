using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Backends.MPD.Extraction;

public class AlbumsParser(ITagParser tagParser)
{
    public IEnumerable<AlbumInfo> GetAlbums(IReadOnlyList<Tag> tags)
    {
        var parsedResults = new List<(AlbumInfo Album, AlbumTrackInfo Track)>();
        var currentTrackTags = new List<Tag>();

        foreach (var tag in tags)
        {
            // If we encounter a new 'file', and we already have data, parse the previous track
            if (tag.Name.Equals("file", StringComparison.OrdinalIgnoreCase) && currentTrackTags.Count > 0)
            {
                // This tag indicates a new track.

                // Store our AlbumInfo and TrackInfo pair
                parsedResults.Add(ParseAlbumInformation(currentTrackTags));
                currentTrackTags.Clear();
            }

            currentTrackTags.Add(tag);
        }

        // Don't forget the very last track in the stream
        if (currentTrackTags.Count > 0)
        {
            parsedResults.Add(ParseAlbumInformation(currentTrackTags));
        }
        
        // 2. Group by Album ID and consolidate all individual tracks into full AlbumInfo objects
        var albums =  parsedResults
            .GroupBy(res => res.Album.Id)
            .Select(group =>
            {
                var templateAlbum = group.First().Album;

                // Collect and deduplicate all tracks found for this album
                var consolidatedTracks = group
                    .Select(x => x.Track)
                    .DistinctBy(t => t.Track.Id)
                    .ToList();

                // Merge all credits found across all tracks in this group
                var allAlbumArtists = group.SelectMany(x => x.Album.CreditsInfo.AlbumArtists)
                    .DistinctBy(a => a.Id)
                    .ToList();

                var allArtists = group.SelectMany(x => x.Album.CreditsInfo.Artists)
                    .DistinctBy(a => a.Artist.Id)
                    .ToList();
                
                var firstAlbumTrack = consolidatedTracks.First();
                
                return templateAlbum with
                {
                    Assets = firstAlbumTrack.Track.Assets, // Take the assets from the first track
                    Tracks = consolidatedTracks,
                    CreditsInfo = templateAlbum.CreditsInfo with
                    {
                        AlbumArtists = allAlbumArtists,
                        Artists = allArtists
                    }
                };
            });

        return albums;
    }

    private (AlbumInfo Album, AlbumTrackInfo Track) ParseAlbumInformation(List<Tag> trackTags)
    {
        var albumTrackInfo = tagParser.ParseAlbumTrackInformation(trackTags);

        var track = albumTrackInfo.Track;

        if (track.FileName != null)
        {
            // For MPD we want to look up album art by filename
            albumTrackInfo = albumTrackInfo with
            {
                Track = albumTrackInfo.Track with
                {
                    Assets =
                    [
                        new AssetInfo
                        {
                            Id = new AssetId(track.FileName),
                            Type = AssetType.FrontCover
                        }
                    ]
                }
            };
        }
        
        var albumInfo = tagParser.ParseAlbumInformation(trackTags);

        return (albumInfo, albumTrackInfo);
    }
}