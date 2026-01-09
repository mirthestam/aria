using Aria.Core;

namespace Aria.MusicServers.MPD;

/// <summary>
/// Represents a unique identifier for a resource.
/// </summary>
public class AlbumArtId(SongId songId) : Id.TypedId<SongId>(songId, "COV");