using Aria.Core.Library;

namespace Aria.Infrastructure.Tagging;

public abstract class TagParserContext;

public sealed class SongTagParserContext : TagParserContext
{
    /// <summary>
    /// Details about the song that have been collected so far
    /// </summary>
    public required SongInfo Song { get; init; }
}

public sealed class ArtistTagParserContext : TagParserContext
{
    /// <summary>
    /// Details about the artist that have been collected so far
    /// </summary>
    public required ArtistInfo Artist { get; init; }
}

public sealed class AlbumTagParserContext : TagParserContext
{
    /// <summary>
    /// Details about the album that have been collected so far
    /// </summary>
    public required AlbumInfo Album { get; init; }
}