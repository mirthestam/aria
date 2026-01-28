using Aria.Core.Library;

namespace Aria.Core.Extraction;

/// <summary>
/// A parser for extracting information from track metadata (tags and their values)
/// </summary>
public interface ITagParser
{
    /// <summary>
    /// Parses all  track-related information from the tags
    /// </summary>
    TrackInfo ParseTrackInformation(IReadOnlyList<Tag> tags);
 
    /// <summary>
    /// Parses all queue-track-related information from the tags
    /// </summary>
    QueueTrackInfo ParseQueueTrackInformation(IReadOnlyList<Tag> tags);
    
    /// <summary>
    /// Parses all album-track-related information from the tags
    /// </summary>
    AlbumTrackInfo ParseAlbumTrackInformation(IReadOnlyList<Tag> tags);
    
    /// <summary>
    ///     Parses all album-related information from the tags
    /// </summary>
    AlbumInfo ParseAlbumInformation(IReadOnlyList<Tag> tags);
}