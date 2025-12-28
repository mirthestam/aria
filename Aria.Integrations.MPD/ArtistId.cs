using Aria.Core;

namespace Aria.MusicServers.MPD;

/// <summary>
///     MPD Artists are identified by their name.
/// </summary>
/// <remarks>
///     MPD Unfortunately does not have any kind of disambiguation for artists
/// </remarks>
public class ArtistId(string artistName) : Id.TypedId<string>(artistName, "ART");