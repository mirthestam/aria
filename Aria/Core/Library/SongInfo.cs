namespace Aria.Core.Library;

public record SongInfo
{
    public required Id Id { get; init; }

    /// <summary>
    ///     The duration  of the song
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    ///     the song title.
    /// </summary>
    public required string Title { get; init; }

    public SongCreditsInfo CreditsInfo { get; init; } = new();
    
    public WorkInfo? Work { get; init; } 
}