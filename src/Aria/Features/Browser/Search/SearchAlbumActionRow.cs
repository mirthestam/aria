using Adw;
using Aria.Core.Extraction;
using GObject;

namespace Aria.Features.Browser.Search;

[Subclass<ActionRow>]
public partial class SearchAlbumActionRow
{
    public SearchAlbumActionRow(Id id) : this()
    {
        AlbumId = id;
    }
    public Id AlbumId { get; }
}