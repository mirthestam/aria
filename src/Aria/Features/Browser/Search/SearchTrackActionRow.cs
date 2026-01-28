using Adw;
using Aria.Core.Extraction;
using GObject;

namespace Aria.Features.Browser.Search;

[Subclass<ActionRow>]
public partial class SearchTrackActionRow
{
    public SearchTrackActionRow(Id id) : this()
    {
        TrackId = id;
    }
    public Id TrackId { get; }
}