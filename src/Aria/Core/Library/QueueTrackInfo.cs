namespace Aria.Core.Library;

public record QueueTrackInfo
{
    /// <summary>
    /// The position of the track in the queue.
    /// </summary>
    public int Position { get; init;}
    
    /// <summary>
    /// Represents a track within an album, containing metadata and associated details.
    /// </summary>
    public required TrackInfo Track { get; init; }
}