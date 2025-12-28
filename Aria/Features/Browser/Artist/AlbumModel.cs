using Aria.Core;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Browser.Artist;

[Subclass<Object>]
public partial class AlbumModel
{
    public AlbumModel(Id id, string displayName) : this()
    {
        Id = id;
        DisplayName = displayName;
    }

    public Id Id { get; set; }
    public string DisplayName { get; set; }
}