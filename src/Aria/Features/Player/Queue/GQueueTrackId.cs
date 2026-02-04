using Aria.Core.Extraction;
using GObject;

namespace Aria.Features.Player.Queue;

[Subclass<GObject.Object>]
public partial class GQueueTrackId
{
    public GQueueTrackId(Id id) : this()
    {
        Id = id;
    }
    public Id Id { get; set; }
}