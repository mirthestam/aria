using Aria.Core.Extraction;
using GObject;

namespace Aria.Infrastructure;

// This class wraps an Id in a GObject.Object,
// allowing it to be used as the content of GTK value objects.
// This makes it straightforward to pass an Id through mechanisms
// such as drag-and-drop operations or GTK actions.

[Subclass<GObject.Object>]
public partial class GId
{
    public GId(Id id) : this()
    {
        Id = id;
    }

    public Id Id { get; }
}