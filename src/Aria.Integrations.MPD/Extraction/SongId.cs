using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

/// <summary>
///     MPD songs are identified by their file name.
/// </summary>
public class SongId(string fileName) : Id.TypedId<string>(fileName, "SNG")
{
    public static Id FromContext(SongIdentificationContext context)
    {
        return new SongId(context.Song.FileName ?? throw new InvalidOperationException("Song has no file name"));
    }    
}