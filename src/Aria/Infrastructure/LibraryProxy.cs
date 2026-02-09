using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class LibraryProxy : ILibrarySource
{
    private static readonly ILibrarySource Empty = new EmptyLibrary();
    private ILibrarySource _innerLibrary = Empty;

    public event EventHandler? Updated;

    public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query,
        CancellationToken cancellationToken = default) => _innerLibrary.GetArtistsAsync(query, cancellationToken);

    public Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default) =>
        _innerLibrary.GetArtistAsync(artistId, cancellationToken);

    public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default) =>
        _innerLibrary.GetArtistsAsync(cancellationToken);

    public Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default) =>
        _innerLibrary.GetAlbumsAsync(cancellationToken);

    public Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default) =>
        _innerLibrary.GetAlbumsAsync(artistId, cancellationToken);

    public Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token) =>
        _innerLibrary.GetAlbumResourceStreamAsync(resourceId, token);

    public Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default) =>
        _innerLibrary.GetAlbumAsync(albumId, cancellationToken);

    public Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        _innerLibrary.SearchAsync(query, cancellationToken);

    public Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default) => _innerLibrary.GetPlaylistsAsync(cancellationToken);
    public Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default) => _innerLibrary.GetPlaylistAsync(playlistId, cancellationToken);

    public Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default) =>
        _innerLibrary.GetItemAsync(id, cancellationToken);

    internal void Attach(ILibrarySource library)
    {
        _innerLibrary = library;
        _innerLibrary.Updated += InnerLibraryOnUpdated;
    }


    internal void Detach()
    {
        _innerLibrary.Updated -= InnerLibraryOnUpdated;
        _innerLibrary = Empty;
    }

    private void InnerLibraryOnUpdated(object? sender, EventArgs e)
    {
        Updated?.Invoke(sender, e);
    }

    private class EmptyLibrary : ILibrarySource
    {
        public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ArtistInfo>());

        public Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArtistInfo?>(null);

        public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<ArtistInfo>());

        public Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<AlbumInfo>());

        public Task<IEnumerable<AlbumInfo>>
            GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<AlbumInfo>());

        public Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token) =>
            Task.FromResult(Stream.Null);

        public Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AlbumInfo?>(null);

        public Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult(SearchResults.Empty);

        public Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<PlaylistInfo>());
        public Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default) => Task.FromResult<PlaylistInfo?>(null);

        public Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default)
            => Task.FromResult<Info?>(null);

        public event EventHandler? Updated;
    }
}