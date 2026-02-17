using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure;

public abstract class BaseLibrary : ILibrarySource 
{
    public virtual event EventHandler? Updated;
    public abstract Task InspectLibraryAsync(CancellationToken ct = default);
    public abstract Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default);
    public abstract Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default);
    
    public abstract Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default);
    public abstract Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default);

    public abstract Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default);
    public abstract Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default);    
    
    public abstract Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default);

    public abstract Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default);

    public abstract Task DeletePlaylistAsync(Id id, CancellationToken cancellationToken = default);

    public virtual Task BeginRefreshAsync() => Task.CompletedTask;

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