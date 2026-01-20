using Aria.Core.Extraction;

namespace Aria.Backends.MPD.Extraction;

public class IdFactory : IIdFactory
{
    public Id CreateTrackId(TrackIdentificationContext context) => TrackId.FromContext(context);

    public Id CreateArtistId(ArtistIdentificationContext context) => ArtistId.FromContext(context);

    public Id CreateAlbumId(AlbumIdentificationContext context) => AlbumId.FromContext(context);
}