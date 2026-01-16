using Aria.Core.Library;

namespace Aria.Core.Playlist;

/// <summary>
/// Controls the active queue (or sometimes playlist) and provides basic information about its state.
/// </summary>
public interface IQueue
{
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
    /// Gets detailed information about the songs in this queue
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<SongInfo>> GetSongsAsync();
    
    /// <summary>
    /// Gets detailed information about the currently playing song.
    /// </summary>
    public SongInfo? CurrentSong { get; }
    
    public Task PlayAsync(int index);

    public Task PlayAlbum(AlbumInfo album);
    public Task EnqueueAlbum(AlbumInfo album);
}