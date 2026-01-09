using Aria.Core;
using Aria.Core.Library;

namespace Aria.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class SessionLibrary : ILibrary
{
    private static readonly ILibrary Empty = new EmptyLibrary();
    private ILibrary _active = Empty;

    public Task<IEnumerable<ArtistInfo>> GetArtists() => _active.GetArtists();

    public Task<IEnumerable<AlbumInfo>> GetAlbums() => _active.GetAlbums();

    public Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId) => _active.GetAlbums(artistId);

    public Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token) => _active.GetAlbumResourceStreamAsync(resourceId, token);

    internal void Attach(ILibrary library) => _active = library ?? Empty;
    internal void Detach() => _active = Empty;

    private class EmptyLibrary : ILibrary
    {
        public Task<IEnumerable<ArtistInfo>> GetArtists() => Task.FromResult(Enumerable.Empty<ArtistInfo>());
        public Task<IEnumerable<AlbumInfo>> GetAlbums() => Task.FromResult(Enumerable.Empty<AlbumInfo>());
        public Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId) => Task.FromResult(Enumerable.Empty<AlbumInfo>());
        public Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token) => Task.FromResult(Stream.Null);
    }
}