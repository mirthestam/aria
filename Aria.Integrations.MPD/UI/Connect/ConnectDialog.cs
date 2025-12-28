using Adw;
using GObject;
using Gtk;
using Dialog = Adw.Dialog;

namespace Aria.MusicServers.MPD.UI.Connect;

[Subclass<Dialog>]
[Template<AssemblyResource>("Aria.Backends.MPD.UI.Connect.ConnectDialog.ui")]
public partial class ConnectDialog
{
    [Connect("cancel-button")] private Button _cancelButton;
    [Connect("connect-button")] private Button _connectButton;

    [Connect("hostname-row")] private EntryRow _hostEntryRow;
    [Connect("password-row")] private PasswordEntryRow _passwordEntryRow;
    [Connect("port-row")] private SpinRow _portSpinRow;

    [Connect("toast-overlay")] private ToastOverlay _toastOverlay;

    // TODO:  Explore options of GTK Data binding

    public string? Hostname => _hostEntryRow.Text_;

    public int Port => (int)_portSpinRow.Value;

    public string? Password => _passwordEntryRow.Text_;

    public event EventHandler? ConnectClicked;

    //partial void Initialize() => WidgetExtensions.ConnectTemplateChildren(this);

    public event EventHandler? CancelClicked;
}