using Aria.Core.Library;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Browser.Artists;

[Subclass<Object>]
public partial class ArtistModel
{
    public ArtistModel(ArtistInfo artist, ArtistNameDisplay nameDisplay) : this()
    {
        Artist = artist;
        NameDisplay = nameDisplay;
    }

    public ArtistInfo Artist { get; }
    public ArtistNameDisplay NameDisplay { get; }
}