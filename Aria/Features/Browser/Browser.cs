using Aria.Core;
using Aria.Core.Library;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using GObject;
using Gtk;
using Box = Gtk.Box;

namespace Aria.Features.Browser;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Browser.ui")]
public partial class Browser
{
    public enum BrowserPages
    {
        Browser,
        EmptyCollection
    }

    private const string EmptyCollectionPageName = "empty-collection-stack-page";
    private const string BrowserPageName = "browser-stack-page";

    [Connect("browser-stack")] private Stack _browserStack;
    [Connect("browser-artist-page")] private ArtistPage _artistPage;
    [Connect("artists-page")] private ArtistsPage _artistsPage;

    public ArtistPage ArtistPage => _artistPage;
    
    public ArtistsPage ArtistsPage => _artistsPage;

    public void TogglePage(BrowserPages page)
    {
        var pageName = page switch
        {
            BrowserPages.Browser => BrowserPageName,
            BrowserPages.EmptyCollection => EmptyCollectionPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        _browserStack.VisibleChildName = pageName;
    }

    public async Task UpdateLibrary(ILibrary playerApiLibrary)
    {
        var artists = await playerApiLibrary.GetArtists();
        ArtistsPage.RefreshArtists(artists);
    }
}