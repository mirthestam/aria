using Aria.Core;
using Aria.Core.Library;

namespace Aria.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class SessionLibrary : ILibrary
{
    private ILibrary? _active;

    public Task<IEnumerable<ArtistInfo>> GetArtists()
    {
        return _active?.GetArtists() ?? Task.FromResult(Enumerable.Empty<ArtistInfo>());
    }

    public Task<IEnumerable<AlbumInfo>> GetAlbums()
    {
        return _active?.GetAlbums() ?? Task.FromResult(Enumerable.Empty<AlbumInfo>());
    }

    public Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId)
    {
        return _active?.GetAlbums(artistId) ?? Task.FromResult(Enumerable.Empty<AlbumInfo>());
    }

    internal void Attach(ILibrary library)
    {
        if (_active != null) Detach();
        _active = library;
    }

    internal void Detach()
    {
        _active = null;
    }
}