using Aria.Core;
using Aria.Core.Library;

namespace Aria.Infrastructure.Tagging;

/// <summary>
/// A parser for extracting information from song metadata (tags and their values)
/// </summary>
public interface ITagParser
{
    /// <summary>
    /// Parses all  song-related information from the tags
    /// </summary>
    SongInfo ParseSongInformation(IReadOnlyList<Tag> tags);
    
    /// <summary>
    ///     Parses all album-related information from the tags
    /// </summary>
    AlbumInfo ParseAlbumInformation(IReadOnlyList<Tag> tags);
}