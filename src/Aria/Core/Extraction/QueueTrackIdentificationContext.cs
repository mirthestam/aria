using Aria.Core.Library;

namespace Aria.Core.Extraction;

public sealed class QueueTrackIdentificationContext : IdentificationContext
{
    /// <summary>
    /// Details about the track that have been collected so far
    /// </summary>
    public required QueueTrackInfo Track { get; init; }
    
    public required IReadOnlyList<Tag> Tags { get; init; }
}