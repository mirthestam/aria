using Aria.Core.Library;
using Aria.Infrastructure.Tagging;

namespace Aria.MusicServers.MPD;

public static class TagParserExtensions
{
    extension(ITagParser parser)
    {
        /// <remarks>MPD provides long lists of tags, separated by 'file' tags.</remarks>
        public IEnumerable<SongInfo> ParseSongsInformation(IReadOnlyList<Tag> tags)
        {
            var currentSongTags = new List<Tag>();
            foreach (var tag in tags)
            {
                // Each 'file' key marks the start of a new song in the response stream
                if (tag.Name.Equals("file", StringComparison.OrdinalIgnoreCase) && currentSongTags.Count > 0)
                {
                    yield return parser.ParseSongInformation(currentSongTags);
                    currentSongTags.Clear();
                }
                
                currentSongTags.Add(tag);
            }
            
            if (currentSongTags.Count > 0) yield return parser.ParseSongInformation(currentSongTags);
        }
    }
}