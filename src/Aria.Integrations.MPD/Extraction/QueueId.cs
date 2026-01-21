using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

public class QueueId(int fileName) : Id.TypedId<int>(fileName, Key)
{
    public const string Key = "PLT";    
}