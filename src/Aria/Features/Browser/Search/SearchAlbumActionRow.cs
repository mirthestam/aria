using Adw;
using Aria.Core.Extraction;
using GObject;

namespace Aria.Features.Browser.Search;

[Subclass<ActionRow>]
public partial class SearchAlbumActionRow
{
    public static SearchAlbumActionRow NewWith(Id id)
    {
        var row = NewWithProperties([]);
        row.AlbumId = id;
        return row;
    }
    
    public Id AlbumId { get; private set; }
}