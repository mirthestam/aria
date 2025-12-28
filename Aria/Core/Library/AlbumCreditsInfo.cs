namespace Aria.Core.Library;

public record AlbumCreditsInfo
{
    /// <summary>
    ///     Information of all artists somehow participating in this album.
    ///     And, what they did. Is a sum or all  songs, and differs per actual song.
    /// </summary>
    /// <remarks>Can be empty if this information is not loaded.</remarks>
    public IReadOnlyList<SongArtistInfo> Artists { get; init; } = [];

    /// <summary>
    ///     The Album Artists. The same for all tracks in this album
    /// </summary>
    public IReadOnlyList<ArtistInfo> AlbumArtists { get; init; } = [];
}