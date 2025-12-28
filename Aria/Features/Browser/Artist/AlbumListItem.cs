using GObject;
using Gtk;

namespace Aria.Features.Browser.Artist;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Artist.AlbumListItem.ui")]
public partial class AlbumListItem
{
    [Connect("name-label")] private Label _nameLabel;

    public void Update(AlbumModel model)
    {
        _nameLabel.SetLabel(model.DisplayName);
    }
}