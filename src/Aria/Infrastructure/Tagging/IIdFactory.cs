using Aria.Core;

namespace Aria.Infrastructure.Tagging;

public interface IIdFactory
{
    Id CreateSongId(SongTagParserContext context);
    Id CreateArtistId(ArtistTagParserContext context);
    Id CreateAlbumId(AlbumTagParserContext context);
}