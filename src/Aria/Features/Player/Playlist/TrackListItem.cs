using GObject;
using Gtk;

namespace Aria.Features.Player.Playlist;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.Playlist.TrackListItem.ui")]
public partial class TrackListItem
{
    [Connect("composer-label")] private Label _composerLabel;
    [Connect("duration-label")] private Label _durationLabel;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;

    public void Update(TrackModel model)
    {
        _titleLabel.SetLabel(model.Title);
        _subTitleLabel.SetLabel(model.Subtitle);
        _composerLabel.SetLabel(model.ComposerLine);
        _subTitleLabel.Visible = !string.IsNullOrEmpty(model.Subtitle);
        _composerLabel.Visible = !string.IsNullOrEmpty(model.ComposerLine);

        if (model.Duration == TimeSpan.Zero)
        {
            _durationLabel.SetLabel("—:—");    
        }
        else
        {
            var duration = model.Duration.TotalHours >= 1
                ? model.Duration.ToString(@"h\:mm\:ss")
                : model.Duration.ToString(@"mm\:ss");
            _durationLabel.SetLabel(duration);            
        }
    }
}