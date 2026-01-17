using Aria.Core.Extraction;

namespace Aria.Core.Library;

public sealed record AlbumInfo : IHasAssets
{
    public Id? Id { get; init; }

    /// <summary>
    /// The Title of the album
    /// </summary>
    public required string Title { get; init; }

    public required DateTime? ReleaseDate { get; init; }
    
    public AlbumCreditsInfo CreditsInfo { get; init; } = new();

    /// <summary>
    /// Optional list of songs.
    /// </summary>
    /// <remarks>Can be empty if this information is not loaded.</remarks>
    public IReadOnlyList<SongInfo> Songs { get; init; } = [];
    
    public IReadOnlyCollection<AssetInfo> Assets { get; init; } = [];
}