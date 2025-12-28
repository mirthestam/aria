using Aria.Core.Library;

namespace Aria.Core.Player;

/// <summary>
/// Controls the player and provides basic information about its state.
/// </summary>
public interface IPlayer
{
    /// <summary>
    /// The identity of the player. Some backends support having multiple players. 
    /// </summary>
    public Id Id { get; }

    public int? Volume { get; }

    public bool SupportsVolume { get; }

    public PlaybackState State { get; }

    /// <summary>
    ///     The number of seconds to Crossfaded between song changes
    /// </summary>
    public int? XFade { get; }

    /// <summary>
    ///     Whether this player supports crossfading
    /// </summary>
    public bool CanXFade { get; }

    public PlaybackProgress Progress { get; }

    public SongInfo? CurrentSong { get; }
    Task PlayAsync();

    Task PauseAsync();

    Task NextAsync();

    Task PreviousAsync();

    Task StopAsync();
}