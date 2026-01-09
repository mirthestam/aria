using Adw;
using Aria.Core;
using Aria.Core.Library;
using Aria.Features.Browser.Album;
using Aria.Features.Browser.Albums;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Search;
using GObject;
using Gtk;

namespace Aria.Features.Browser;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.BrowserPage.ui")]
public partial class BrowserPage
{
    [Connect("browser-nav-view")] private Adw.NavigationView _browserNavigationView;
    [Connect("search-page")] private SearchPage _searchPage;    
    
    [Connect("library-nav-split-view")] private NavigationSplitView _libraryNavigationSplitView;
    [Connect("library-artist-list")] private ArtistsPage _libraryArtistsPage;    

    [Connect("library-nav-view")] private NavigationView _libraryNavigationView;
    [Connect("library-artist-detail")] private ArtistPage _libraryArtistPage;
    [Connect("library-albums")] private AlbumsPage _libraryAlbumsPage;
    
    private const string LibraryPageName = "library-nav-page";
    private const string SearchPageName = "search-nav-page";
    
    public ArtistPage LibraryArtistPage => _libraryArtistPage;
    public ArtistsPage LibraryArtistsPage => _libraryArtistsPage;
    public AlbumsPage LibraryAlbumsPage => _libraryAlbumsPage;
    public SearchPage SearchPage => _searchPage;
    public NavigationSplitView NavigationSplitView => _libraryNavigationSplitView;
    
    public void StartSearch()
    {
        if (_browserNavigationView.VisiblePageTag == SearchPageName) return;
        
        _browserNavigationView.PushByTag(SearchPageName);
        SearchPage.Clear();
        
    }
    
    public void ShowArtistDetailRoot()
    {
        _libraryNavigationView.ReplaceWithTags(["library-artist-detail"]);
    }

    public void ShowAllAlbumsRoot()
    {
        // Replace the stack with the albums navigation 'tree' 
        _libraryNavigationView.ReplaceWithTags(["library-albums"]);
    }

    public AlbumPage PushAlbumPage()
    {
        var page = new AlbumPage();
        _libraryNavigationView.Push(page);
        return page;
    }
}