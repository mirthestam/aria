using Aria.Core;
using Aria.Infrastructure.Tagging;

namespace Aria.MusicServers.MPD;

public class IdFactory : IIdFactory
{
    public Id CreateSongId(SongTagParserContext context) => SongId.FromContext(context);

    public Id CreateArtistId(ArtistTagParserContext context) => ArtistId.FromContext(context);

    public Id CreateAlbumId(AlbumTagParserContext context) => AlbumId.FromContext(context);
}