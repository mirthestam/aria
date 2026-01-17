using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

public class IdFactory : IIdFactory
{
    public Id CreateSongId(SongIdentificationContext context) => SongId.FromContext(context);

    public Id CreateArtistId(ArtistIdentificationContext context) => ArtistId.FromContext(context);

    public Id CreateAlbumId(AlbumIdentificationContext context) => AlbumId.FromContext(context);
}