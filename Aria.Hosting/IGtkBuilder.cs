using Gio;
using GObject;
using Type = System.Type;

namespace Aria.Hosting;

public interface IGtkBuilder
{
    string ApplicationId { get; set; }
    ApplicationFlags ApplicationFlags { get; set; }
    GtkApplicationType GtkApplicationType { get; set; }
    Type WindowType { get; set; }
    void WithGType<T>() where T : GTypeProvider;
}