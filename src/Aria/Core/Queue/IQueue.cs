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
    Task<IEnumerable<TrackInfo>> GetTracksAsync();
    
    /// <summary>
    /// Gets detailed information about the currently playing track.
    /// </summary>
    public TrackInfo? CurrentTrack { get; }
    
    public Task PlayAsync(int index);
    public Task PlayAsync(AlbumInfo album, EnqueueAction action);
    public Task PlayAsync(TrackInfo track, EnqueueAction action);
}