namespace Aria.Core.Library;

/// <summary>
/// Provides access to the library
/// </summary>
public interface ILibrary
{
    /// <summary>
    /// Gets the artists from the library
    /// </summary>
    Task<IEnumerable<ArtistInfo>> GetArtists();
    
    /// <summary>
    /// Gets basic information about all albums in the library.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AlbumInfo>> GetAlbums();

    /// <summary>
    /// Gets detailed information about all albums where the specified artist participates on.
    /// </summary>
    Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId);
}