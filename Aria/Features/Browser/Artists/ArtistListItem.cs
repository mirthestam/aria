using GObject;
using Gtk;

namespace Aria.Features.Browser.Artists;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Artists.ArtistListItem.ui")]
public partial class ArtistListItem
{
    [Connect("name-label")] private Label _nameLabel;

    public void Update(ArtistModel model)
    {
        _nameLabel.SetLabel(model.DisplayName);
    }
}