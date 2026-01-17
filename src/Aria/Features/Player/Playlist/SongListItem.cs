using GObject;
using Gtk;

namespace Aria.Features.Player.Playlist;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.Playlist.SongListItem.ui")]
public partial class SongListItem
{
    [Connect("composer-label")] private Label _composerLabel;
    [Connect("duration-label")] private Label _durationLabel;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;

    public void Update(SongModel model)
    {
        _titleLabel.SetLabel(model.Title);
        _subTitleLabel.SetLabel(model.Subtitle);
        _composerLabel.SetLabel(model.ComposerLine);
        _subTitleLabel.Visible = !string.IsNullOrEmpty(model.Subtitle);
        _composerLabel.Visible = !string.IsNullOrEmpty(model.ComposerLine);

        var duration = model.Duration.TotalHours >= 1
            ? model.Duration.ToString(@"h\:mm\:ss")
            : model.Duration.ToString(@"mm\:ss");
        _durationLabel.SetLabel(duration);
    }
}