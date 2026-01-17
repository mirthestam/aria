using Aria.Core.Library;

namespace Aria.Infrastructure.Tagging;

public sealed class SongTagParserContext : TagParserContext
{
    /// <summary>
    /// Details about the song that have been collected so far
    /// </summary>
    public required SongInfo Song { get; init; }
}