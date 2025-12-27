namespace Aria.App;

public class MainWindow : Adw.ApplicationWindow
{
    private MainWindow(Gtk.Builder builder, string name) : base(new Adw.Internal.ApplicationWindowHandle(builder.GetPointer(name), false))
    {
        builder.Connect(this);
    }

    public MainWindow() : this(new Gtk.Builder("Aria.App.UI.MainWindow.ui"), "main-window")
    {
    }
}