using System.Collections.Concurrent;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Microsoft.Extensions.Caching.Memory;

namespace Aria.Infrastructure.Caching;


public sealed class QueryCacheLibrarySource : ILibrarySource, IDisposable
{
    // The UI should be agnostic to load to the backend, and backend implementations
    // should likewise not need to manage load concerns directly.
    // To achieve this, this is a sliding query cache layer.
    // This allows the UI to request data in bursts without overwhelming
    // the underlying backends.
    
    private readonly ILibrarySource _inner;
    private readonly MemoryCache _cache;
    private readonly TimeSpan _slidingWindow;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    public event EventHandler? Updated;
    
    public Task InspectLibraryAsync(CancellationToken ct = default)
    {
        return _inner.InspectLibraryAsync(ct);
    }

    public QueryCacheLibrarySource(ILibrarySource inner, TimeSpan slidingWindow)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _slidingWindow = slidingWindow;
        _cache = new MemoryCache(new MemoryCacheOptions());
        _inner.Updated += InnerOnUpdated;
    }

    private void InnerOnUpdated(object? sender, EventArgs e)
    {
        Clear();
        Updated?.Invoke(sender, e);
    }
    
    public Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token) => _inner.GetAlbumResourceStreamAsync(resourceId, token);

    public Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        return _inner.GetPlaylistsAsync(cancellationToken);
    }

    public Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default)
    {
        return _inner.GetPlaylistAsync(playlistId, cancellationToken);
    }

    public async Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default)
    {
        var key = $"id:{id}";

        return await GetOrCreateAsync(
            key,
            ct => _inner.GetItemAsync(id, ct),
            cancellationToken).ConfigureAwait(false);        
    }

    public Task DeletePlaylistAsync(Id id, CancellationToken cancellationToken = default)
    {
        return _inner.DeletePlaylistAsync(id, cancellationToken);
    }

    public Task BeginRefreshAsync()
    {
        return _inner.BeginRefreshAsync();
    }

    public async Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default)
    {
        var key = $"id:{albumId}";

        return await GetOrCreateAsync(
            key,
            ct => _inner.GetAlbumAsync(albumId, ct),
            cancellationToken).ConfigureAwait(false);
    }
    
    public async Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        // We can treat this data as full albums.
        const string key = "albums:all";

        var list = await GetOrCreateAsync(
            key,
            async ct => (await _inner.GetAlbumsAsync(ct).ConfigureAwait(false)).ToArray(),
            cancellationToken).ConfigureAwait(false);

        return list;
    }

    public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default)
    {
        return _inner.GetArtistsAsync(query, cancellationToken);
    }
    
    public async Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default)
    {
        var key = $"id:{artistId}";

        return await GetOrCreateAsync(
            key,
            ct => _inner.GetArtistAsync(artistId, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default)
    {
        const string key = "artists:all";

        var list = await GetOrCreateAsync(
            key,
            async ct => (await _inner.GetArtistsAsync(ct).ConfigureAwait(false)).ToArray(),
            cancellationToken).ConfigureAwait(false);

        return list;
    }
    
    public async Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default)
    {
        // Do not treat this data as complete albums.
        // Artist-specific albums may be incomplete and only include information relevant to that artist.
        // For example, tracks featuring other artists may be omitted.
        var key = $"albums:artist:{artistId}";

        var list = await GetOrCreateAsync(
            key,
            async ct => (await _inner.GetAlbumsAsync(artistId, ct).ConfigureAwait(false)).ToArray(),
            cancellationToken).ConfigureAwait(false);

        return list;
    }

    private async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is not null)
                return cached;

            var value = await factory(ct).ConfigureAwait(false);

            _cache.Set(key, value, new MemoryCacheEntryOptions
            {
                SlidingExpiration = _slidingWindow
            });

            return value;
        }
        finally
        {
            gate.Release();
        }
    }
    
    public Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default) => _inner.SearchAsync(query, cancellationToken);

    public void Dispose()
    {
        _inner.Updated -= InnerOnUpdated;
        _cache.Dispose();

        foreach (var gate in _gates.Values)
            gate.Dispose();
    }
    
    private void Clear() => _cache.Compact(1.0);
}