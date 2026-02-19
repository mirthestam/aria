using Aria.Features.Browser.Album;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Playlists;
using Aria.Features.Browser.Search;
using Aria.Features.Browser.Shared;
using Aria.Hosting;

namespace Aria.Features.Browser;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithBrowserGTypes()
        {
            // Shared
            builder.WithGType<AlbumListItem>();
            builder.WithGType<AlbumModel>();
            builder.WithGType<AlbumsGrid>();
            
            // Album
            builder.WithGType<AlbumPage>();       
            builder.WithGType<TrackGroup>();
            builder.WithGType<CreditBox>();            
            
            // Albums
            builder.WithGType<Albums.AlbumsPage>();            
            
            // Artist
            builder.WithGType<ArtistPage>();
            builder.WithGType<EmptyPage>();
            
            // Artists
            builder.WithGType<ArtistsPage>();            
            builder.WithGType<ArtistModel>();
            builder.WithGType<ArtistListItem>();            
            
            // Playlists
            builder.WithGType<PlaylistsPage>();    
            builder.WithGType<PlaylistNameCell>();           
            
            // Search
            builder.WithGType<SearchPage>();            
            
            // Common
            builder.WithGType<BrowserPage>();            
            builder.WithGType<BrowserEmptyPage>();
            builder.WithGType<BrowserHost>();
        }
    }
}