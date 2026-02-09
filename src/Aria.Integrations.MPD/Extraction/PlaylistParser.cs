using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Backends.MPD.Extraction;

public class PlaylistParser(ITagParser tagParser)
{
    public IEnumerable<AlbumTrackInfo> GetPlaylist(IReadOnlyList<Tag> tags)
    {
        var parsedResults = new List<AlbumTrackInfo>();
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

        return parsedResults;
    }

    private AlbumTrackInfo ParseAlbumInformation(List<Tag> trackTags)
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
                            Id = new AssetId(albumTrackInfo.Track.FileName),
                            Type = AssetType.FrontCover
                        }
                    ]
                }
            };
        }
        
        return albumTrackInfo;
    }
}