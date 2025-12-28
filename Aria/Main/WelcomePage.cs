using GObject;
using Gtk;
using Box = Gtk.Box;
using Button = Gtk.Button;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Aria.Main;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Main.WelcomePage.ui")]
public partial class WelcomePage
{
    [Connect("connect-button")] private Button _connectButton;

    public event EventHandler? ConnectClicked;

    partial void Initialize()
    {
        // TODO refactor to actions ?
        _connectButton.OnClicked += (_, _) => ConnectClicked?.Invoke(this, EventArgs.Empty);
    }
}