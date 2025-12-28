using Aria.Core;

namespace Aria.MusicServers.MPD;

public class PlaylistId(int fileName) : Id.TypedId<int>(fileName, "PLT");