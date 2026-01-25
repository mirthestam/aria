using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;

namespace Aria.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class QueueProxy : IQueueSource
{
    public event Action<QueueStateChangedFlags>? StateChanged;
    
    private IQueueSource? _innerQueue;

    public Id Id => _innerQueue?.Id ?? null!;
    public int Length => _innerQueue?.Length ?? 0;

    public PlaybackOrder Order => _innerQueue?.Order ?? PlaybackOrder.Default;
    public ShuffleSettings Shuffle => _innerQueue?.Shuffle ?? ShuffleSettings.Default;
    public RepeatSettings Repeat => _innerQueue?.Repeat ?? RepeatSettings.Default;
    public ConsumeSettings Consume => _innerQueue?.Consume ?? ConsumeSettings.Default;

    public Task SetShuffleAsync(bool enabled) => _innerQueue?.SetShuffleAsync(enabled) ?? Task.CompletedTask;
    public Task SetRepeatAsync(bool enabled) => _innerQueue?.SetRepeatAsync(enabled) ?? Task.CompletedTask;
    public Task SetConsumeAsync(bool enabled) => _innerQueue?.SetConsumeAsync(enabled) ?? Task.CompletedTask;
    public Task<IEnumerable<TrackInfo>> GetTracksAsync() => _innerQueue?.GetTracksAsync() ?? Task.FromResult(Enumerable.Empty<TrackInfo>());

    public TrackInfo? CurrentTrack => _innerQueue?.CurrentTrack;
    
    public Task PlayAsync(int index) => _innerQueue?.PlayAsync(index) ?? Task.CompletedTask;
    public Task PlayAsync(AlbumInfo album, EnqueueAction action) => _innerQueue?.PlayAsync(album, action) ?? Task.CompletedTask;
    public Task PlayAsync(TrackInfo track, EnqueueAction action) => _innerQueue?.PlayAsync(track, action) ?? Task.CompletedTask;

    internal void Attach(IQueueSource queue)
    {
        if (_innerQueue != null) Detach();
        _innerQueue = queue;
        _innerQueue.StateChanged += InnerQueueOnStateChanged;
    }
    
    internal void Detach()
    {
        _innerQueue?.StateChanged -= InnerQueueOnStateChanged;
        _innerQueue = null;
    }
    
    private void InnerQueueOnStateChanged(QueueStateChangedFlags flags)
    {
        StateChanged?.Invoke(flags);
    }    
}