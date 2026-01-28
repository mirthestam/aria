using Aria.Core.Extraction;

namespace Aria.Core.Library;

public record QueueTrackInfo
{
    /// <summary>
    /// The identity of the entry in the queue.
    /// </summary>
    public Id? Id { get; init; }
    
    /// <summary>
    /// The position of the track in the queue.
    /// </summary>
    public required int Position { get; init;}
    
    /// <summary>
    /// Represents a track within an album, containing metadata and associated details.
    /// </summary>
    public required TrackInfo Track { get; init; }
}