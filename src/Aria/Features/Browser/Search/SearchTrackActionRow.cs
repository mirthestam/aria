using Adw;
using Aria.Core.Extraction;
using GObject;

namespace Aria.Features.Browser.Search;

[Subclass<ActionRow>]
public partial class SearchTrackActionRow
{
    public static SearchTrackActionRow NewFor(Id id)
    {
        var row = NewWithProperties([]);
        row.TrackId = id;
        return row;
    }
    
    public Id TrackId { get; private set; }
}