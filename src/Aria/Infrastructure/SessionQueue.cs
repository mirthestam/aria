using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Playlist;

namespace Aria.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class SessionQueue : IQueue
{
    private IQueue? _active;

    public Id Id => _active?.Id ?? null!;
    public int Length => _active?.Length ?? 0;

    public PlaybackOrder Order => _active?.Order ?? PlaybackOrder.Default;
    public ShuffleSettings Shuffle => _active?.Shuffle ?? ShuffleSettings.Default;
    public RepeatSettings Repeat => _active?.Repeat ?? RepeatSettings.Default;
    public ConsumeSettings Consume => _active?.Consume ?? ConsumeSettings.Default;

    public Task SetShuffleAsync(bool enabled) => _active?.SetShuffleAsync(enabled) ?? Task.CompletedTask;
    public Task SetRepeatAsync(bool enabled) => _active?.SetRepeatAsync(enabled) ?? Task.CompletedTask;
    public Task SetConsumeAsync(bool enabled) => _active?.SetConsumeAsync(enabled) ?? Task.CompletedTask;
    public Task<IEnumerable<SongInfo>> GetSongsAsync() => _active?.GetSongsAsync() ?? Task.FromResult(Enumerable.Empty<SongInfo>());

    public SongInfo? CurrentSong => _active?.CurrentSong;
    public Task PlayAsync(int index) => _active?.PlayAsync(index) ?? Task.CompletedTask;
    public Task PlayAlbum(AlbumInfo album) => _active?.PlayAlbum(album) ?? Task.CompletedTask;
    public Task EnqueueAlbum(AlbumInfo album) => _active?.EnqueueAlbum(album) ?? Task.CompletedTask;

    internal void Attach(IQueue queue)
    {
        if (_active != null) Detach();
        _active = queue;
    }

    internal void Detach()
    {
        _active = null;
    }
}