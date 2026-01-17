using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

/// <summary>
/// Represents a unique identifier for a resource.
/// </summary>
public class AssetId(string fileName) : Id.TypedId<string>(fileName, "IMG");