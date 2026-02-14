using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;

namespace Aria.Core.Queue;

/// <summary>
/// Controls the active queue (or sometimes playlist) and provides basic information about its state.
/// </summary>
public interface IQueue
{
    // TODO: Add a settings option to store the user's preferred default action.    
    public const EnqueueAction DefaultEnqueueAction = EnqueueAction.EnqueueEnd;
    
    public Id Id { get; }
    public uint Length { get; }

    PlaybackOrder Order { get; }
    ShuffleSettings Shuffle { get; }
    RepeatSettings Repeat { get; }
    ConsumeSettings Consume { get; }

    Task SetShuffleAsync(bool enabled);
    Task SetRepeatAsync(RepeatMode repeatMode);
    Task SetConsumeAsync(bool enabled);
    
    /// <summary>
    /// Gets detailed information about the tracks in this queue
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<QueueTrackInfo>> GetTracksAsync();
    
    /// <summary>
    /// Gets detailed information about the currently playing track.
    /// </summary>
    public QueueTrackInfo? CurrentTrack { get; }
    
    public Task EnqueueAsync(Info item, EnqueueAction action);
    public Task EnqueueAsync(IEnumerable<Info> items, EnqueueAction action);
    
    public Task EnqueueAsync(Info item, uint index);

    public Task MoveAsync(Id sourceTrackId, uint targetPlaylistIndex);

    public Task ClearAsync();
    
    /// <summary>
    /// Removes the specified track from the queue.
    /// </summary>
    /// <param name="id">The ID of the queue track</param>
    public Task RemoveTrackAsync(Id id);
}