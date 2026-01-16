using Aria.Core;

namespace Aria.MusicServers.MPD;

/// <summary>
/// Represents a unique identifier for a resource.
/// </summary>
public class AssetId(string fileName) : Id.TypedId<string>(fileName, "IMG");