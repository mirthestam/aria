using Adw;
using Aria.Core;
using Aria.Core.Library;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Artist;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Artist.ArtistPage.ui")]
public partial class ArtistPage
{
    public enum ArtistPages
    {
        Artist,
        Empty
    }

    private const string EmptyPageName = "empty-stack-page";
    private const string ArtistPageName = "artist-stack-page";

    [Connect("albums-grid-view")] private GridView _albumsGridView;

    private ListStore _albumsListStore;
    private SingleSelection _albumsSelection;
    [Connect("artist-stack")] private Stack _artistStack;
    private SignalListItemFactory _signalListItemFactory;

    public void TogglePage(ArtistPages page)
    {
        var pageName = page switch
        {
            ArtistPages.Artist => ArtistPageName,
            ArtistPages.Empty => EmptyPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        _artistStack.VisibleChildName = pageName;
    }

    partial void Initialize()
    {
        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            var item = (ListItem)args.Object;
            item.SetChild(new AlbumListItem());
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            var modelItem = (AlbumModel)listItem.GetItem()!;
            var widget = (AlbumListItem)listItem.GetChild()!;
            widget.Update(modelItem);
        };

        _albumsListStore = ListStore.New(AlbumModel.GetGType());
        _albumsSelection = SingleSelection.New(_albumsListStore);
        _albumsGridView.SetFactory(_signalListItemFactory);
        _albumsGridView.SetModel(_albumsSelection);
    }

    // TODO: Review how Euphonica implemented their GridView.  
    // For additional functionality, consider providing view options similar to Files (Nautilus).

    // TODO: I also like their 'Recent' page implementation.  
    // This could serve as a template here: show the first X items with a "More" button.  
    
    public void ShowAlbums(IReadOnlyList<AlbumInfo> albums, Id artistId)
    {
        _albumsListStore.RemoveAll();

        var albumsList = albums.ToList();
         SetTitle(artistId, albumsList);

        // These are albums where this artist actually is an album artist.
        // So we consider these 'their' albums.
        var discography = albumsList.Where(a => a.CreditsInfo.AlbumArtists.Any(ar => ar.Id == artistId)).ToList();

        // These are albums where this artist is not an album artist but appears on the album
        var appearsOn = albumsList.Except(discography);

        // Just show all albums, as we do not have logic yet on the UI to split
        // however, In the future I want to show them separate
        foreach (var album in albumsList.OrderBy(a => a.Title))
        {
            var listViewItem = new AlbumModel(album.Id, album.Title);
            _albumsListStore.Append(listViewItem);
        }
    }

    private void SetTitle(Id artistId, List<AlbumInfo> albumsList)
    {
        // TODO: This fails, because the equality comparer is not implemented (properly) yet
        return;
        
        string artistName;

        var firstAlbum = albumsList.First();
        var albumArtist = firstAlbum.CreditsInfo.AlbumArtists.FirstOrDefault(a => a.Id == artistId);
        if (albumArtist != null)
        {
            artistName = albumArtist.Name;
        }
        else
        {
            var artist = firstAlbum.CreditsInfo.Artists.First(a => a.Artist.Id.Equals(artistId));
            artistName = artist.Artist.Name;
        }

        Title = $"{artistName}";
    }
}