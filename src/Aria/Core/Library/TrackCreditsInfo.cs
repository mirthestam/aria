namespace Aria.Core.Library;

public record TrackCreditsInfo
{
    public IReadOnlyList<TrackArtistInfo> Artists { get; init; } = [];

    public IReadOnlyList<ArtistInfo> AlbumArtists { get; init; } = [];
}