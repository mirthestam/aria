namespace Aria.Core.Extraction;

public interface IIdFactory
{
    Id CreateTrackId(TrackIdentificationContext context);
    Id CreateArtistId(ArtistIdentificationContext context);
    Id CreateAlbumId(AlbumIdentificationContext context);
}