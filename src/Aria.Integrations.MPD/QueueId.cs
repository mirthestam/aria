using Aria.Core;

namespace Aria.MusicServers.MPD;

public class QueueId(int fileName) : Id.TypedId<int>(fileName, "PLT");