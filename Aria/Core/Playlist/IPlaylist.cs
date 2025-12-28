namespace Aria.Core.Playlist;

/// <summary>
/// Controls the active playlist (or sometimes queue) and provides basic information about its state.
/// </summary>
public interface IPlaylist
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
}