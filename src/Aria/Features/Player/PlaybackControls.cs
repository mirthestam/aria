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
    [Connect("media-controls")] private MediaControls _mediaControls;
    [Connect("playlist-progress-label")] private Label _playlistProgressLabel;
    [Connect("remaining-time-label")] private Label _remainingTimeLabel;

    private TimeSpan _shownDuration;

    public void SetProgress(TimeSpan trackElapsed, TimeSpan trackDuration)
    {
        if (_shownDuration != trackDuration)
        {
            _elapsedScale.SetRange(0, trackDuration.TotalSeconds);
            _shownDuration = trackDuration;
        }

        _elapsedScale.SetValue(trackElapsed.TotalSeconds);

        _elapsedTimeLabel.Label_ = trackElapsed.ToString(@"mm\:ss");
        _remainingTimeLabel.Label_ = (trackDuration - trackElapsed).ToString(@"mm\:ss");
    }

    public void SetPlaylistInfo(int? playlistCurrentTrackIndex, int playlistLength)
    {
        var hasLength = playlistLength > 0;

        if (_playlistProgressLabel.Visible != hasLength)
        {
            _playlistProgressLabel.Visible = hasLength;            
        }
        
        _playlistProgressLabel.Label_ = hasLength
            ? $"{playlistCurrentTrackIndex + 1}/{playlistLength}"
            : "0/0";
    }

    public void SetPlaybackState(PlaybackState playerState)
    {
        _mediaControls.SetPlaybackState(playerState);
    }
}