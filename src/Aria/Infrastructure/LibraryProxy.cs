using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class LibraryProxy : ILibrarySource
{
    private ILibrarySource? _innerLibrary;

    public event EventHandler? Updated;
    
    public Task InspectLibraryAsync(CancellationToken ct = default)
    {
        return _innerLibrary?.InspectLibraryAsync(ct) ?? Task.CompletedTask;
    }

    public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetArtistsAsync(query, cancellationToken) ?? Task.FromResult<IEnumerable<ArtistInfo>>([]);
    }

    public Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetArtistAsync(artistId, cancellationToken) ?? Task.FromResult<ArtistInfo?>(null);
    }

    public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetArtistsAsync(cancellationToken) ?? Task.FromResult<IEnumerable<ArtistInfo>>([]);
    }

    public Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetAlbumsAsync(cancellationToken) ?? Task.FromResult<IEnumerable<AlbumInfo>>([]);
    }

    public Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetAlbumsAsync(artistId, cancellationToken) ?? Task.FromResult<IEnumerable<AlbumInfo>>([]); 
    }

    public Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token)
    {
        return _innerLibrary?.GetAlbumResourceStreamAsync(resourceId, token) ?? Task.FromResult(Stream.Null);
    }

    public Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetAlbumAsync(albumId, cancellationToken) ?? Task.FromResult<AlbumInfo?>(null);
    }

    public Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.SearchAsync(query, cancellationToken) ?? Task.FromResult(new SearchResults());
    }

    public Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetPlaylistsAsync(cancellationToken) ?? Task.FromResult<IEnumerable<PlaylistInfo>>([]);
    }

    public Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetPlaylistAsync(playlistId, cancellationToken) ?? Task.FromResult<PlaylistInfo?>(null);
    }

    public Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.GetItemAsync(id, cancellationToken) ?? Task.FromResult<Info?>(null);
    }

    public Task DeletePlaylistAsync(Id id, CancellationToken cancellationToken = default)
    {
        return _innerLibrary?.DeletePlaylistAsync(id, cancellationToken) ?? Task.CompletedTask;
    }

    public Task BeginRefreshAsync()
    {
        return _innerLibrary?.BeginRefreshAsync() ?? Task.CompletedTask;
    }

    internal void Attach(ILibrarySource library)
    {
        _innerLibrary = library;
        _innerLibrary.Updated += InnerLibraryOnUpdated;
    }
    
    internal void Detach()
    {
        _innerLibrary?.Updated -= InnerLibraryOnUpdated;
        _innerLibrary = null;
    }

    private void InnerLibraryOnUpdated(object? sender, EventArgs e)
    {
        Updated?.Invoke(sender, e);
    }
}