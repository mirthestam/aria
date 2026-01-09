using Aria.Core;
using Aria.Core.Player;
using GObject;
using Gtk;

namespace Aria.Features.Player;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.PlaybackControls.ui")]
public partial class PlaybackControls
{
    [Connect("elapsed-scale")] private Scale _elapsedScale;
    [Connect("elapsed-time-label")] private Label _elapsedTimeLabel;
    [Connect("playlist-progress-label")] private Label _playlistProgressLabel;
    [Connect("remaining-time-label")] private Label _remainingTimeLabel;
    [Connect("media-controls")] private MediaControls _mediaControls;

    public void PlayerStateChanged(PlayerStateChangedFlags flags, IPlaybackApi api)
    {
        if (flags.HasFlag(PlayerStateChangedFlags.CurrentSong))
        {
            SetPlaylistInfo(api.Playlist.Order.CurrentIndex, api.Playlist.Length);
            SetProgress(api.Player.Progress.Elapsed, api.Player.Progress.Duration);
        }

        if (flags.HasFlag(PlayerStateChangedFlags.Progress))
        {
            SetElapsed(api.Player.Progress.Elapsed);
            SetRemaining(api.Player.Progress.Remaining);
            SetProgress(api.Player.Progress.Elapsed);
        }
    }

    private void SetProgress(TimeSpan songElapsed, TimeSpan songDuration)
    {
        _elapsedScale.SetRange(0, songDuration.TotalSeconds);
    }

    private void SetProgress(TimeSpan songElapsed)
    {
        _elapsedScale.SetValue(songElapsed.TotalSeconds);
    }

    private void SetPlaylistInfo(int? playlistCurrentSongIndex, int playlistLength)
    {
        if (playlistLength == 0)
        {
            _playlistProgressLabel.Label_ = $"N/A";            
        }
        _playlistProgressLabel.Label_ = $"{playlistCurrentSongIndex + 1}/{playlistLength}";
    }

    private void SetElapsed(TimeSpan time)
    {
        _elapsedTimeLabel.Label_ = time.ToString(@"mm\:ss");
    }

    private void SetRemaining(TimeSpan time)
    {
        _remainingTimeLabel.Label_ = time.ToString(@"mm\:ss");
    }
}