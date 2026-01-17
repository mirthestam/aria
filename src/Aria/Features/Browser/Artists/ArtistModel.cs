using Aria.Core.Library;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Browser.Artists;

[Subclass<Object>]
public partial class ArtistModel
{
    public ArtistModel(ArtistInfo artist) : this()
    {
        Artist = artist;
    }

    public ArtistInfo Artist { get; }
}