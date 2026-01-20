using Aria.Core.Library;

namespace Aria.Core.Extraction;

public sealed class TrackIdentificationContext : IdentificationContext
{
    /// <summary>
    /// Details about the track that have been collected so far
    /// </summary>
    public required TrackInfo Track { get; init; }
}