using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Playlist;

namespace Aria.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class SessionPlaylist : IPlaylist
{
    private IPlaylist? _active;

    public Id Id => _active?.Id ?? null!;

    public int Length => _active?.Length ?? 0;

    public PlaybackOrder Order => _active?.Order ?? new PlaybackOrder(0, null);

    public ShuffleSettings Shuffle => _active?.Shuffle ?? new ShuffleSettings(false, false);

    public RepeatSettings Repeat => _active?.Repeat ?? new RepeatSettings(false, false, false);

    public ConsumeSettings Consume => _active?.Consume ?? new ConsumeSettings(false, false);

    public Task SetShuffleAsync(bool enabled)
    {
        return _active?.SetShuffleAsync(enabled) ?? Task.CompletedTask;
    }

    public Task SetRepeatAsync(bool enabled)
    {
        return _active?.SetRepeatAsync(enabled) ?? Task.CompletedTask;
    }

    public Task SetConsumeAsync(bool enabled)
    {
        return _active?.SetConsumeAsync(enabled) ?? Task.CompletedTask;
    }

    public Task<IEnumerable<SongInfo>> GetSongsAsync()
    {
        return _active?.GetSongsAsync() ?? Task.FromResult(Enumerable.Empty<SongInfo>());
    }

    internal void Attach(IPlaylist playlist)
    {
        if (_active != null) Detach();
        _active = playlist;
    }

    internal void Detach()
    {
        _active = null;
    }
}