using Adw;
using Gio;
using GObject;
using Gtk;
using Box = Gtk.Box;
using Button = Gtk.Button;
using ListStore = Gio.ListStore;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Aria.Main.Welcome;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Main.Welcome.WelcomePage.ui")]
public partial class WelcomePage
{
    [Connect("other-button")] private Button _otherButton;
    [Connect("connection-listbox")] private ListBox _connectionListBox;

    public event Action<Guid>? ConnectionSelected;
    
    public void RefreshConnections(IEnumerable<ConnectionModel> connections)
    {
        // Need to unbind the old rows.
        var child = _connectionListBox.GetFirstChild();
        while (child != null)
        {
            if (child is ConnectionListItem row)
            {
                row.OnActivated -= ActionRowOnOnActivated;
            }
            var next = child.GetNextSibling();
            _connectionListBox.Remove(child);
            child = next;
        }        

        _connectionListBox.RemoveAll();
        foreach (var connection in connections)
        {
            var actionRow = new ConnectionListItem(connection);
            actionRow.OnActivated += ActionRowOnOnActivated;
            _connectionListBox.Append(actionRow);
        }
    }

    private void ActionRowOnOnActivated(ActionRow sender, EventArgs args)
    {
        if (sender is ConnectionListItem row)
        {
            ConnectionSelected?.Invoke(row.ConnectionId);
        }
    }
}