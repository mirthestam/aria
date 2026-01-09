namespace Aria.Core.Library;

public record SongCreditsInfo
{
    public IReadOnlyList<SongArtistInfo> Artists { get; init; } = [];

    public IReadOnlyList<ArtistInfo> AlbumArtists { get; init; } = [];
}