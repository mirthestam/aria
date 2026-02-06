using Adw;
using Aria.Core.Extraction;
using GObject;

namespace Aria.Features.Browser.Album;

[Subclass<ActionRow>]
public partial class AlbumTrackRow
{

    public Id TrackId { get; private set;  }

    public static AlbumTrackRow NewForTrackId(Id trackId)
    {
        var row = NewWithProperties([]);
        row.TrackId = trackId;
        return row;
    }
}