namespace Aria.Core.Library;

public sealed record AlbumInfo
{
    public required Id Id { get; init; }

    /// <summary>
    /// The Title of the album
    /// </summary>
    public required string Title { get; init; }

    public AlbumCreditsInfo CreditsInfo { get; init; } = new();
}