using Aria.Core;
using Aria.Core.Library;

namespace Aria.Features.Browser;

public record ShowAlbumDetailsMessage(AlbumInfo Album, ArtistInfo? Artist = null);
public record ShowArtistDetailsMessage(ArtistInfo Artist);
public record ShowAllAlbumsMessage();