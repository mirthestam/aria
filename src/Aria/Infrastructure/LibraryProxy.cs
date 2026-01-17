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

    public event Action? Updated;

    public Task<ArtistInfo?> GetArtist(Id artistId, CancellationToken cancellationToken = default) =>
        _innerLibrary.GetArtist(artistId, cancellationToken);

    public Task<IEnumerable<ArtistInfo>> GetArtists(CancellationToken cancellationToken = default) =>
        _innerLibrary.GetArtists(cancellationToken);

    public Task<IEnumerable<AlbumInfo>> GetAlbums(CancellationToken cancellationToken = default) =>
        _innerLibrary.GetAlbums(cancellationToken);

    public Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId, CancellationToken cancellationToken = default) =>
        _innerLibrary.GetAlbums(artistId, cancellationToken);

    public Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token) =>
        _innerLibrary.GetAlbumResourceStreamAsync(resourceId, token);

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

    private void InnerLibraryOnUpdated()
    {
        Updated?.Invoke();
    }

    private class EmptyLibrary : ILibrarySource
    {
        public Task<ArtistInfo?> GetArtist(Id artistId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArtistInfo?>(null);

        public Task<IEnumerable<ArtistInfo>> GetArtists(CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<ArtistInfo>());

        public Task<IEnumerable<AlbumInfo>> GetAlbums(CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<AlbumInfo>());

        public Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<AlbumInfo>());

        public Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token) =>
            Task.FromResult(Stream.Null);

        public event Action? Updated;
    }
}