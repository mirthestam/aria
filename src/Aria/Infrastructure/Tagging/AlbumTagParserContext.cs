using Aria.Core.Library;

namespace Aria.Infrastructure.Tagging;

public sealed class AlbumTagParserContext : TagParserContext
{
    /// <summary>
    /// Details about the album that have been collected so far
    /// </summary>
    public required AlbumInfo Album { get; init; }
}