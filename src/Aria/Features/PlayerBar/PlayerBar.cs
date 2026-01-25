using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using GObject;
using Gtk;

namespace Aria.Features.PlayerBar;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.PlayerBar.PlayerBar.ui")]
public partial class PlayerBar
{
    [Connect("elapsed-bar")] private ProgressBar _progressBar;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    [Connect("media-controls")] private Player.MediaControls _mediaControls;
    
    public void SetCurrentTrack(TrackInfo? trackInfo)
    {
        if (trackInfo == null)
        {
            _titleLabel.Label_ = "";
            _subTitleLabel.Label_ = "";
            _progressBar.Fraction = 0;
            return;
        }
        
        var titleText = trackInfo?.Title ?? "Unnamed tracks";
        if (trackInfo?.Work?.ShowMovement ?? false)
            // For  these kind of works, we ignore the
            titleText = $"{trackInfo.Work.MovementName} ({trackInfo.Work.MovementNumber} {trackInfo.Title} ({trackInfo.Work.Work})";


        var credits = trackInfo?.CreditsInfo;
        var subTitleText = "";

        if (credits != null)
        {
            var artists = string.Join(", ", credits.OtherArtists.Select(x => x.Artist.Name));

            var details = new List<string>();
            var conductors = string.Join(", ", credits.Conductors.Select(x => x.Artist.Name));
            if (!string.IsNullOrEmpty(conductors))
                details.Add($"conducted by {conductors}");

            var composers = string.Join(", ", credits.Composers.Select(x => x.Artist.Name));
            if (!string.IsNullOrEmpty(composers))
                details.Add($"composed by {composers}");

            subTitleText = artists;
            if (details.Count > 0) subTitleText += $" ({string.Join(", ", details)})";
        }

        _titleLabel.Label_ = titleText;
        _subTitleLabel.Label_ = subTitleText;
    }
    
    public void SetPlaybackState(PlaybackState playerState)
    {
        var elapsedBarVisible = playerState switch
        {
            PlaybackState.Unknown or PlaybackState.Stopped => false,
            PlaybackState.Playing or PlaybackState.Paused => true,
            _ => false
        };

        _progressBar.Visible = elapsedBarVisible;
        _mediaControls.SetPlaybackState(playerState);
    }

    public void SetProgress(TimeSpan progressElapsed, TimeSpan progressDuration)
    {
        if (progressDuration == TimeSpan.Zero)
        {
            _progressBar.Fraction = 1;
        }
        else
        {
            _progressBar.Fraction = progressElapsed.TotalSeconds / progressDuration.TotalSeconds;            
        }
    }
}