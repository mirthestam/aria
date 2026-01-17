using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

/// <summary>
///     MPD Artists are identified by their name.
/// </summary>
/// <remarks>
///     MPD Unfortunately does not have any kind of disambiguation for artists
/// </remarks>
public class ArtistId(string artistName) : Id.TypedId<string>(artistName, "ART")
{
    public static Id FromContext(ArtistIdentificationContext context)
    {
        return new ArtistId(context.Artist.Name);
    }
}