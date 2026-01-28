using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure.Extraction;

namespace Aria.Backends.MPD.Extraction;

public static class TagParserExtensions
{
    extension(ITagParser parser)
    {
        /// <remarks>MPD provides long lists of tags, separated by 'file' tags.</remarks>
        public IEnumerable<QueueTrackInfo> ParseTracksInformation(IReadOnlyList<Tag> tags)
        {
            var position = 0;
            var id = 0;
            
            var currentTrackTags = new List<Tag>();
            foreach (var tag in tags)
            {
                if (tag.Name.Equals(MPDTags.QueueTags.Id, StringComparison.OrdinalIgnoreCase))
                {
                    id = int.Parse(tag.Value);
                    continue;
                }
                
                // MPD returns the track position in the queue as a 'tag' on the song
                if (tag.Name.Equals(MPDTags.QueueTags.Position, StringComparison.OrdinalIgnoreCase))
                {
                    position = int.Parse(tag.Value);
                    continue;
                }
                
                // Each 'file' key marks the start of a new track in the response stream
                if (tag.Name.Equals(MPDTags.FileTags.File, StringComparison.OrdinalIgnoreCase) && currentTrackTags.Count > 0)
                {
                    var track = parser.ParseTrackInformation(currentTrackTags);
                    yield return new QueueTrackInfo
                    {
                        Id = new QueueTrackId(id),
                        Position = position,
                        Track = track
                    };
                    currentTrackTags.Clear();
                }
                
                currentTrackTags.Add(tag);
            }

            if (currentTrackTags.Count <= 0) yield break;
            {
                var track = parser.ParseTrackInformation(currentTrackTags);
                yield return new QueueTrackInfo
                {
                    Id = new QueueTrackId(id),
                    Position = position,
                    Track = track
                };
                currentTrackTags.Clear();
            }
        }
    }
}