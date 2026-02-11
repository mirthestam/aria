using GObject;
using Gtk;

namespace Aria.Features.Browser.Playlists;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Playlists.PlaylistNameCell.ui")]
public partial class PlaylistNameCell
{
    [Connect("title-label")] private Label _titleLabel;
    [Connect("subtitle-label")] private Label _subTitleLabel;

    public void Bind(PlaylistModel model)
    {
        Model = model;
        _titleLabel.Label_ = model.Playlist.Name;
        _subTitleLabel.Label_ = model.Credits;
    }
    
    public PlaylistModel Model { get; private set; }
}