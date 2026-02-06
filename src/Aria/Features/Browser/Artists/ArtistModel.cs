using Aria.Core.Library;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Browser.Artists;

[Subclass<Object>]
public partial class ArtistModel
{
    public static ArtistModel NewForArtistInfo(ArtistInfo artist, ArtistNameDisplay nameDisplay)
    {
        var model = NewWithProperties([]);
        model.Artist = artist;
        model.NameDisplay = nameDisplay;
        return model;
    }

    public ArtistInfo Artist { get; private set; }
    public ArtistNameDisplay NameDisplay { get; private set; }
}