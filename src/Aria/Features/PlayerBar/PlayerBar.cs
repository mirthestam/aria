using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.PlayerBar;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.PlayerBar.PlayerBar.ui")]
public partial class PlayerBar
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("elapsed-bar")] private ProgressBar _progressBar;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    [Connect("media-controls")] private Player.MediaControls _mediaControls;
    
    public event EventHandler<Id> EnqueueRequested;    
    
    partial void Initialize()
    {
        // Add the drop target
        var type = GObject.Type.Object;        
        var idWrapperDropTarget = DropTarget.New(type, DragAction.Copy);
        idWrapperDropTarget.OnDrop += IdWrapperDropTargetOnOnDrop;
        AddController(idWrapperDropTarget);
    }

    public void LoadCover(Texture texture)
    {
        _coverPicture.Visible = true;
        _coverPicture.SetPaintable(texture);
    }

    public void ClearCover()
    {
        _coverPicture.Visible = false;
        _coverPicture.SetPaintable(null);
    }    
    
    public void SetCurrentTrack(TrackInfo? trackInfo)
    {
        if (trackInfo == null)
        {
            _titleLabel.Label_ = "";
            _subTitleLabel.Label_ = "";
            _progressBar.Fraction = 0;
            return;
        }
        
        var titleText = trackInfo.Title;
        if (trackInfo.Work?.ShowMovement ?? false)
            // For  these kind of works, we ignore the
            titleText = $"{trackInfo.Work.MovementName} ({trackInfo.Work.MovementNumber} {trackInfo.Title} ({trackInfo.Work.Work})";


        var credits = trackInfo.CreditsInfo;

        var artists = string.Join(", ", credits.OtherArtists.Select(x => x.Artist.Name));

        var details = new List<string>();
        var conductors = string.Join(", ", credits.Conductors.Select(x => x.Artist.Name));
        if (!string.IsNullOrEmpty(conductors))
            details.Add($"conducted by {conductors}");

        var composers = string.Join(", ", credits.Composers.Select(x => x.Artist.Name));
        if (!string.IsNullOrEmpty(composers))
            details.Add($"composed by {composers}");

        var subTitleText = artists;
        if (details.Count > 0) subTitleText += $" ({string.Join(", ", details)})";

        _titleLabel.Label_ = titleText;
        _subTitleLabel.Label_ = subTitleText;
    }
    
    public void SetPlaybackState(PlaybackState playerState)
    {
        _progressBar.Visible = playerState switch
        {
            PlaybackState.Unknown or PlaybackState.Stopped => false,
            PlaybackState.Playing or PlaybackState.Paused => true,
            _ => false
        };
        
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
    
    private bool IdWrapperDropTargetOnOnDrop(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something onto the mini bar.
        var value = args.Value.GetObject();
        if (value is not GId gId) return false;
        
        EnqueueRequested(this, gId.Id);

        return true;
    }
}