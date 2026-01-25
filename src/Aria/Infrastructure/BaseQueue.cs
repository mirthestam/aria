using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;

namespace Aria.Infrastructure;

public abstract class BaseQueue : IQueueSource
{
    public virtual event Action<QueueStateChangedFlags>? StateChanged;
    
    public virtual Id Id { get; protected set; } = Id.Empty;

    public virtual int Length  { get; protected set; }

    public virtual PlaybackOrder Order { get; protected set; } = PlaybackOrder.Default;

    public virtual ShuffleSettings Shuffle  { get; protected set; }

    public virtual RepeatSettings Repeat  { get; protected set; }

    public virtual ConsumeSettings Consume  { get; protected set; }

    public virtual TrackInfo? CurrentTrack  { get; protected set; }

    public virtual Task SetShuffleAsync(bool enabled) => Task.CompletedTask;

    public virtual Task SetRepeatAsync(bool enabled) => Task.CompletedTask;

    public virtual Task SetConsumeAsync(bool enabled) => Task.CompletedTask;

    public virtual Task<IEnumerable<TrackInfo>> GetTracksAsync() => Task.FromResult(Enumerable.Empty<TrackInfo>());

    public virtual Task PlayAsync(int index) => Task.CompletedTask;
    
    public virtual Task PlayAsync(AlbumInfo album, EnqueueAction action) => Task.CompletedTask;

    public virtual Task PlayAsync(TrackInfo track, EnqueueAction action) => Task.CompletedTask;
    
    protected void OnStateChanged(QueueStateChangedFlags flags)
    {
        StateChanged?.Invoke(flags);
    }        
}