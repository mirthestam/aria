using Aria.Core.Player;
using Gdk;
using GObject;
using Graphene;
using Gtk;
using Box = Gtk.Box;
using Range = Gtk.Range;

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

    [Connect("elapsed-popover")] private Popover _elapsedPopover;
    [Connect("elapsed-popover-label")] private Label _elapsedPopoverLabel;

    private EventControllerMotion _motionController;
    private TimeSpan _shownDuration;
    
    private CancellationTokenSource _seekCts;

    public event SeekRequestedAsyncHandler? SeekRequested;
    
    partial void Initialize()
    {
        _motionController = new EventControllerMotion();
        _elapsedScale.AddController(_motionController);
        _elapsedScale.OnChangeValue += ElapsedScaleOnOnChangeValue;

        _elapsedPopover.SetParent(_elapsedScale);

        _motionController.OnMotion += MotionControllerOnOnMotion;
        _motionController.OnLeave += MotionControllerOnOnLeave;
    }

    private bool ElapsedScaleOnOnChangeValue(Range sender, Range.ChangeValueSignalArgs args)
    {
        var seconds = args.Value;
        var target = TimeSpan.FromSeconds(seconds);

        _seekCts?.Cancel();
        _seekCts?.Dispose();
        _seekCts = new CancellationTokenSource();
        var ct = _seekCts.Token;

        _ = SeekRequested?.Invoke(target, ct) ?? Task.CompletedTask;

        // This false allows GTK to update the slider
        return false;
    }
    
    private void MotionControllerOnOnLeave(EventControllerMotion sender, EventArgs args)
    {
        _elapsedPopover.Popdown();
    }

    private void MotionControllerOnOnMotion(EventControllerMotion sender, EventControllerMotion.MotionSignalArgs args)
    {
        try
        {
            // Calculate the hovered time
            _elapsedScale.GetRangeRect(out var rangeRect);
            var duration = _elapsedScale.Adjustment!.Upper;

            var x = Math.Clamp(args.X, 0, rangeRect.Width);

            var isRtl = _elapsedScale.GetDirection() == TextDirection.Rtl;
            var elapsedSeconds = isRtl
                ? (rangeRect.Width - x) / rangeRect.Width * duration
                : x / rangeRect.Width * duration;

            elapsedSeconds = Math.Clamp(elapsedSeconds, 0, duration);
            var timeSpan = TimeSpan.FromSeconds(elapsedSeconds);

            // Update the label with the formatted time
            _elapsedPopoverLabel.Label_ = timeSpan.ToString(@"mm\:ss");

            // (Re)position the popover
            const int yOffset = 12;
            var rect = new Rectangle
            {
                X = (int)Math.Round(rangeRect.X + x),
                Y = (int)Math.Round((double)(rangeRect.Y - yOffset)),
                Width = 1,
                Height = 1
            };

            _elapsedPopover.SetPointingTo(rect);

            if (!_elapsedPopover.Visible)
                _elapsedPopover.Popup();
        }
        catch
        {
            _elapsedPopover.Popdown();
        }
    }

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
        _elapsedScale.Visible = playerState switch
        {
            PlaybackState.Unknown or PlaybackState.Stopped => false,
            PlaybackState.Playing or PlaybackState.Paused => true,
            _ => _elapsedScale.Visible
        };

        _mediaControls.SetPlaybackState(playerState);
    }
}