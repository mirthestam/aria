using Adw;
using Aria.Core.Extraction;
using GObject;

namespace Aria.Features.Browser.Album;

[Subclass<ActionRow>]
public partial class AlbumTrackRow
{
    public AlbumTrackRow(Id trackId) : this()
    {
        TrackId = trackId;
    }
    public Id TrackId { get; set;  }
}