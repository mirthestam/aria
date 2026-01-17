using Aria.Core.Library;

namespace Aria.Core.Extraction;

public sealed class AlbumIdentificationContext : IdentificationContext
{
    /// <summary>
    /// Details about the album that have been collected so far
    /// </summary>
    public required AlbumInfo Album { get; init; }
}