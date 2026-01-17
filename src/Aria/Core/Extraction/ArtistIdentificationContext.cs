using Aria.Core.Library;

namespace Aria.Core.Extraction;

public sealed class ArtistIdentificationContext : IdentificationContext
{
    /// <summary>
    /// Details about the artist that have been collected so far
    /// </summary>
    public required ArtistInfo Artist { get; init; }
}