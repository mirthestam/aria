using Aria.Core;

namespace Aria.MusicServers.MPD;

/// <summary>
///     MPD songs are identified by their file name.
/// </summary>
public class SongId(string fileName) : Id.TypedId<string>(fileName, "SNG");