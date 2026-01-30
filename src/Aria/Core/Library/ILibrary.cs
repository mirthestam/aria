using Aria.Core.Extraction;

namespace Aria.Core.Library;

public enum ArtistSort
{
    ByName
}

public sealed record ArtistQuery
{
    /// <summary>
    /// If set, only returns artists that have at least one of the specified role flags (OR/ANY semantics).
    /// Example: Composer|Conductor returns artists that are either composer or conductor (or both).
    /// </summary>    
    public ArtistRoles? RequiredRoles { get; init; }

    public ArtistSort Sort { get; init; } = ArtistSort.ByName;

    public static ArtistQuery All { get; } = new();
}

/// <summary>
/// Provides access to the library
/// </summary>
public interface ILibrary
{
    /// <summary>
    /// Gets the artists from the library
    /// </summary>
    Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the artists from the library using a query
    /// </summary>
    Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets detailed information about the specified artist.
    /// </summary>
    Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets basic information about all albums in the library.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about all albums where the specified artist participates on.
    /// </summary>
    Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a resource stream from the library based on the specified resource identifier.
    /// </summary>
    Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about the specified album.
    /// </summary>
    Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches the library amongst tracks, albums etc that match the specified query.
    /// </summary>
    Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default);
    
    Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default);
}