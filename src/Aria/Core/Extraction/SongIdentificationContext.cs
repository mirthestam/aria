using Aria.Core.Library;

namespace Aria.Core.Extraction;

public sealed class SongIdentificationContext : IdentificationContext
{
    /// <summary>
    /// Details about the song that have been collected so far
    /// </summary>
    public required SongInfo Song { get; init; }
}