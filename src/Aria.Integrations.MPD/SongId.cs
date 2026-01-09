using Aria.Core;
using Aria.Infrastructure.Tagging;

namespace Aria.MusicServers.MPD;

/// <summary>
///     MPD songs are identified by their file name.
/// </summary>
public class SongId(string fileName) : Id.TypedId<string>(fileName, "SNG")
{
    public static Id FromContext(SongTagParserContext context)
    {
        return new SongId(context.Song.FileName ?? throw new InvalidOperationException("Song has no file name"));
    }    
}