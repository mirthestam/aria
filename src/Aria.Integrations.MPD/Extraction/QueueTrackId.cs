using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

public class QueueTrackId(int id) : Id.TypedId<int>(id, Key)
{
    public const string Key = "QUE";
    
    public static Id Parse(int value)
    {
        return new QueueTrackId(value);
    }    
}