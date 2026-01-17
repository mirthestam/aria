namespace Aria.Core.Extraction;

public interface IIdFactory
{
    Id CreateSongId(SongIdentificationContext context);
    Id CreateArtistId(ArtistIdentificationContext context);
    Id CreateAlbumId(AlbumIdentificationContext context);
}