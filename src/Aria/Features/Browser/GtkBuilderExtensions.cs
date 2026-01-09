using Aria.Features.Browser.Album;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Search;
using Aria.Hosting;

namespace Aria.Features.Browser;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithBrowserGTypes()
        {
            builder.WithGType<AlbumPage>();
            builder.WithGType<ArtistPage>();
            builder.WithGType<EmptyPage>();
            builder.WithGType<BrowserEmptyPage>();
            builder.WithGType<ArtistModel>();
            builder.WithGType<ArtistListItem>();
            builder.WithGType<ArtistsPage>();
            builder.WithGType<BrowserHost>();
            builder.WithGType<SearchPage>();
            builder.WithGType<BrowserPage>();
            builder.WithGType<Albums.AlbumsAlbumListItem>();
            builder.WithGType<Albums.AlbumsAlbumModel>();
            builder.WithGType<Albums.AlbumsPage>();
        }
    }
}