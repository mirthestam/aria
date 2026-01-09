using Gtk;

namespace Aria.Hosting;

public interface IGtkContext
{
    Application Application { get; set; }
    bool IsRunning { get; set; }
}