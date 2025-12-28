using Aria.Core;

namespace Aria.MusicServers.MPD;

/// <summary>
/// MPD Albums are identified by their name.
/// </summary>
public class AlbumId(string albumName) : Id.TypedId<string>(albumName, "ALB");

// TODO: Albums are not uniquely identified by name alone.
// At a minimum, they should be unique per album artist.I need to consider which aspects of this identification strategy belong to tagging behavior and which are dictated by the underlying backend.