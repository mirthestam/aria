namespace Aria.Core.Library;

public record SongInfo : IHasAssets
{
    public Id? Id { get; init; }

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
    
    public required DateTime? ReleaseDate { get; init; }
    
    public string? FileName { get; init; }

    public IReadOnlyCollection<AssetInfo> Assets { get; init; } = [];
}