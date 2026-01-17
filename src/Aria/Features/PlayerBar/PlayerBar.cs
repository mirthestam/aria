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

    // TODO: The mini-player progress bar is visible only in numeric mode, which is turned off.

    public void QueueStateChanged(QueueStateChangedFlags flags, IAria api)
    {
        if (!flags.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;
        
        var song = api.QueueProxy.CurrentSong;

        var titleText = song?.Title ?? "Unnamed song";
        if (song?.Work?.ShowMovement ?? false)
            // For  these kind of works, we ignore the
            titleText = $"{song.Work.MovementName} ({song.Work.MovementNumber} {song.Title} ({song.Work.Work})";


        var credits = song?.CreditsInfo;
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

    public void PlayerStateChanged(PlayerStateChangedFlags flags, IAria api)
    {
        if (flags.HasFlag(PlayerStateChangedFlags.PlaybackState))
        {
            var elapsedBarVisible = api.PlayerProxy.State switch
            {
                PlaybackState.Unknown or PlaybackState.Stopped => false,
                PlaybackState.Playing or PlaybackState.Paused => true,
                _ => false
            };

            _progressBar.Visible = elapsedBarVisible;
        }

        if (flags.HasFlag(PlayerStateChangedFlags.Progress))
            _progressBar.Fraction =
                api.PlayerProxy.Progress.Elapsed.TotalSeconds / api.PlayerProxy.Progress.Duration.TotalSeconds;
    }
}