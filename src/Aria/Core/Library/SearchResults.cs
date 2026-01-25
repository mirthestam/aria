using Aria.Features.Browser.Artist;

namespace Aria.Core.Library;

public sealed record SearchResults()
{
    public IReadOnlyList<AlbumInfo> Albums { get; init; } = Array.Empty<AlbumInfo>();
    public IReadOnlyList<ArtistInfo> Artists { get; init; } = Array.Empty<ArtistInfo>();
    public IReadOnlyList<TrackInfo> Tracks { get; init; } = Array.Empty<TrackInfo>();
    
    public static SearchResults Empty => new();
}