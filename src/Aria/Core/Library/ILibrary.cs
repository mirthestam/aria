using Aria.Core.Extraction;

namespace Aria.Core.Library;

/// <summary>
/// Provides access to the library
/// </summary>
public interface ILibrary
{
    /// <summary>
    /// Gets the artists from the library
    /// </summary>
    Task<IEnumerable<ArtistInfo>> GetArtists(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets detailed information about the specified artist.
    /// </summary>
    Task<ArtistInfo?> GetArtist(Id artistId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets basic information about all albums in the library.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AlbumInfo>> GetAlbums(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about all albums where the specified artist participates on.
    /// </summary>
    Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a resource stream from the library based on the specified resource identifier.
    /// </summary>
    Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about the specified album.
    /// </summary>
    Task<AlbumInfo?> GetAlbum(Id albumId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches the library amongst tracks, albums etc that match the specified query.
    /// </summary>
    Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default);
}