using Aria.Core;

namespace Aria.MusicServers.MPD;

/// <summary>
/// In MPD, players are implemented as partitions.
/// </summary>
public class PlayerId(string name) : Id.TypedId<string>(name, "PLR");