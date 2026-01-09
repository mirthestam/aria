using Adw;
using GObject;
using Gtk;

namespace Aria.Main.Welcome;

[Subclass<ActionRow>]
[Template<AssemblyResource>("Aria.Main.Welcome.ConnectionListItem.ui")]
public partial class ConnectionListItem
{
    [Connect("title-label")] private Label _titleLabel;
    [Connect("subtitle-label")] private Label _subtitleLabel;

    public Guid ConnectionId { get; }

    public ConnectionListItem(ConnectionModel model) : this()
    {
        ConnectionId = model.Id;
        _titleLabel.SetLabel(model.DisplayName);
        _subtitleLabel.SetLabel(model.ConnectionText);
    }
}