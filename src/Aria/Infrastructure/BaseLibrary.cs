using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Infrastructure;

public abstract class BaseLibrary : ILibrarySource 
{
    public virtual event Action? Updated;    
    
    public virtual Task<IEnumerable<ArtistInfo>> GetArtists(CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ArtistInfo>());
    public virtual Task<ArtistInfo?> GetArtist(Id artistId, CancellationToken cancellationToken = default) => Task.FromResult(default(ArtistInfo));
    public virtual Task<IEnumerable<AlbumInfo>> GetAlbums(CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<AlbumInfo>());
    public virtual Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<AlbumInfo>());
    public virtual Task<AlbumInfo?> GetAlbum(Id albumId, CancellationToken cancellationToken = default) => Task.FromResult(default(AlbumInfo));
    
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
        Updated?.Invoke();
    }            
}