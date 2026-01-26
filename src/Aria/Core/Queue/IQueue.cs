using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Core.Queue;

/// <summary>
/// Controls the active queue (or sometimes playlist) and provides basic information about its state.
/// </summary>
public interface IQueue
{
    // TODO: Add a settings option to store the user's preferred default action.    
    public const EnqueueAction DefaultEnqueueAction = EnqueueAction.EnqueueEnd;
    
    public Id Id { get; }
    public int Length { get; }

    PlaybackOrder Order { get; }
    ShuffleSettings Shuffle { get; }
    RepeatSettings Repeat { get; }
    ConsumeSettings Consume { get; }

    Task SetShuffleAsync(bool enabled);
    Task SetRepeatAsync(bool enabled);
    Task SetConsumeAsync(bool enabled);
    
    /// <summary>
    /// Gets detailed information about the tracks in this queue
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<QueueTrackInfo>> GetTracksAsync();
    
    /// <summary>
    /// Gets detailed information about the currently playing track.
    /// </summary>
    public TrackInfo? CurrentTrack { get; }
    
    /// <summary>
    /// Plays the track at the specified index in the queue.
    /// </summary>
    public Task PlayAsync(int index);
    
    public Task EnqueueAsync(Info item, EnqueueAction action);
    public Task EnqueueAsync(Info item, int index);
}