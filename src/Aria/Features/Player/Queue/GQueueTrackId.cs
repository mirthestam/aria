using Aria.Core.Extraction;
using GObject;

namespace Aria.Features.Player.Queue;

[Subclass<GObject.Object>]
public partial class GQueueTrackId
{
    public static GQueueTrackId NewWithId(Id id)
    {
        var item = NewWithProperties([]);
        item.Id = id;
        return item;
    }

    public Id Id { get; private set; }
}