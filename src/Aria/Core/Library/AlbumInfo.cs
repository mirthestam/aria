namespace Aria.Core.Library;

public sealed record AlbumInfo
{
    public Id? Id { get; init; }

    /// <summary>
    /// The Title of the album
    /// </summary>
    public required string Title { get; init; }
    
    public required DateTime? ReleaseDate { get; init; }
    
    // De collectie van alle bijbehorende bestanden/afbeeldingen
    public IReadOnlyCollection<AlbumResource> Resources { get; init; } = [];
    
    public AlbumCreditsInfo CreditsInfo { get; init; } = new();
    
    /// <summary>
    /// Optional list of songs.
    /// </summary>
    /// <remarks>Can be empty if this information is not loaded.</remarks>
    public IReadOnlyList<SongInfo> Songs { get; init; } = [];
    

    // Helper voor de UI die gewoon snel de voorkant wil tonen
    public AlbumResource? FrontCover => Resources.FirstOrDefault(r => r.Type == ResourceType.FrontCover);    
}

public enum ResourceType
{
    FrontCover
}