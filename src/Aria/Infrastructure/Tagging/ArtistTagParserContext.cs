using Aria.Core.Library;

namespace Aria.Infrastructure.Tagging;

public sealed class ArtistTagParserContext : TagParserContext
{
    /// <summary>
    /// Details about the artist that have been collected so far
    /// </summary>
    public required ArtistInfo Artist { get; init; }
}