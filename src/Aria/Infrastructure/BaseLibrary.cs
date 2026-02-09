using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure;

public abstract class BaseLibrary : ILibrarySource 
{
    public virtual event EventHandler? Updated;    
    
    public virtual Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ArtistInfo>());
    public virtual Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ArtistInfo>());
    public virtual Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default) => Task.FromResult(default(ArtistInfo));
    public virtual Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<AlbumInfo>());
    public virtual Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<AlbumInfo>());
    public virtual Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default) => Task.FromResult(default(AlbumInfo));
    public virtual Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default) => Task.FromResult(SearchResults.Empty);
    public virtual Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<PlaylistInfo>());
    public virtual Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default) => Task.FromResult(default(PlaylistInfo));    
    
    public virtual Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default) => Task.FromResult<Info?>(null);

    public virtual async Task<Stream> GetAlbumResourceStreamAsync(Id id, CancellationToken ct)
    {
        return await GetDefaultAlbumResourceStreamAsync(ct).ConfigureAwait(false);
    }
    
    protected static async Task<Stream> GetDefaultAlbumResourceStreamAsync(CancellationToken ct = default)
    {
        var assembly = typeof(BaseLibrary).Assembly;

        await using var stream = assembly.GetManifestResourceStream("Aria.Resources.vinyl-record.png");

        if (stream == null)
        {
            return Stream.Null;
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        var buffer = ms.ToArray();

        return new MemoryStream(buffer);
    }
    
    protected void OnUpdated()
    {
        Updated?.Invoke(this, EventArgs.Empty);
    }            
}