using Aria.Core;
using Aria.Core.Library;

namespace Aria.Infrastructure;

public abstract class BaseLibrary : ILibrary 
{
    public virtual Task<IEnumerable<ArtistInfo>> GetArtists() => Task.FromResult(Enumerable.Empty<ArtistInfo>());
    public virtual Task<IEnumerable<AlbumInfo>> GetAlbums() => Task.FromResult(Enumerable.Empty<AlbumInfo>());
    public virtual Task<IEnumerable<AlbumInfo>> GetAlbums(Id artistId) => Task.FromResult(Enumerable.Empty<AlbumInfo>());

    public virtual async Task<Stream> GetAlbumResourceStreamAsync(Id id, CancellationToken ct)
    {
        return await GetDefaultAlbumResourceStreamAsync();
    }
    
    protected static async Task<Stream> GetDefaultAlbumResourceStreamAsync()
    {
        var assembly = typeof(BaseLibrary).Assembly;

        await using var stream = assembly.GetManifestResourceStream("Aria.Resources.vinyl-record.png");

        if (stream == null)
        {
            return Stream.Null;
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var buffer = ms.ToArray();

        return new MemoryStream(buffer);
    }    
}