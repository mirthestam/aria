using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

/// <summary>
///     MPD tracks are identified by their file name.
/// </summary>
public class TrackId(string fileName) : Id.TypedId<string>(fileName, Key)
{
    public const string Key = "TRK";
    
    public static Id FromContext(TrackIdentificationContext context)
    {
        return new TrackId(context.Track.FileName ?? throw new InvalidOperationException("Track has no file name"));
    }

    public static Id Parse(string value)
    {
        return new TrackId(value);
    }
}