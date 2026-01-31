using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure.Extraction;

namespace Aria.Backends.MPD.Extraction;

public static class TagParserExtensions
{
    extension(ITagParser parser)
    {
        /// <remarks>MPD provides long lists of tags, separated by 'file' tags.</remarks>
        public IEnumerable<QueueTrackInfo> ParseQueueTracksInformation(IReadOnlyList<Tag> tags)
        {
            var currentTrackTags = new List<Tag>();
            foreach (var tag in tags)
            {
                // Each 'file' key marks the start of a new track in the response stream
                if (tag.Name.Equals(PicardTagNames.FileTags.File, StringComparison.OrdinalIgnoreCase) && currentTrackTags.Count > 0)
                {
                    var track = parser.ParseQueueTrackInformation(currentTrackTags);
                    yield return track;
                    currentTrackTags.Clear();
                }
                
                currentTrackTags.Add(tag);
            }

            if (currentTrackTags.Count <= 0) yield break;
            {
                var track = parser.ParseQueueTrackInformation(currentTrackTags);
                yield return track;
                currentTrackTags.Clear();
            }
        }
    }
}