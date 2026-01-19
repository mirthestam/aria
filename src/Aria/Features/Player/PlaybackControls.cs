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

    public void SetProgress(TimeSpan songElapsed, TimeSpan songDuration)
    {
        if (_shownDuration != songDuration)
        {
            _elapsedScale.SetRange(0, songDuration.TotalSeconds);
            _shownDuration = songDuration;
        }

        _elapsedScale.SetValue(songElapsed.TotalSeconds);

        _elapsedTimeLabel.Label_ = songElapsed.ToString(@"mm\:ss");
        _remainingTimeLabel.Label_ = (songDuration - songElapsed).ToString(@"mm\:ss");
    }

    public void SetPlaylistInfo(int? playlistCurrentSongIndex, int playlistLength)
    {
        var hasLength = playlistLength > 0;

        if (_playlistProgressLabel.Visible != hasLength)
        {
            _playlistProgressLabel.Visible = hasLength;            
        }
        
        _playlistProgressLabel.Label_ = hasLength
            ? $"{playlistCurrentSongIndex + 1}/{playlistLength}"
            : "0/0";
    }
}