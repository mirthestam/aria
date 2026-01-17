using Aria.Core.Library;

namespace Aria.Features.Browser;

public record ShowAlbumDetailsMessage(AlbumInfo Album, ArtistInfo? Artist = null);