using Gtk;

namespace Aria.Hosting;

public class GtkContext : IGtkContext
{
    public bool IsLifetimeLinked { get; set; } = false;
    public Application Application { get; set; } = null!;
    public bool IsRunning { get; set; }
}