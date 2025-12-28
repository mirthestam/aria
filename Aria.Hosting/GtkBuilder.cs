using Gio;
using GObject;
using Action = System.Action;
using Type = System.Type;

namespace Aria.Hosting;

public class GtkBuilder : IGtkBuilder
{
    public List<Action> GTypeInitializers { get; } = new();
    public string ApplicationId { get; set; }
    public ApplicationFlags ApplicationFlags { get; set; } = ApplicationFlags.FlagsNone;
    public GtkApplicationType GtkApplicationType { get; set; } = GtkApplicationType.Gtk;
    public Type WindowType { get; set; }

    public void WithGType<T>() where T : GTypeProvider
    {
        GTypeInitializers.Add(() => T.GetGType());
    }
}