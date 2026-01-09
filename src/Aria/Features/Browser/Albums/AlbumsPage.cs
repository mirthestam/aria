using Adw;
using Aria.Core.Library;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Albums;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Albums.AlbumsPage.ui")]
public partial class AlbumsPage
{
    public event Action<AlbumInfo, ArtistInfo>? AlbumSelected;    
    

    [Connect("albums-grid-view")] private GridView _albumsGridView;

    private ListStore _albumsListStore;
    private SingleSelection _albumsSelection;
    [Connect("artist-stack")] private Stack _artistStack;
    private SignalListItemFactory _signalListItemFactory;


    partial void Initialize()
    {
        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            var item = (ListItem)args.Object;
            item.SetChild(new AlbumsAlbumListItem());
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            var modelItem = (AlbumsAlbumModel)listItem.GetItem()!;
            var widget = (AlbumsAlbumListItem)listItem.GetChild()!;
            widget.Initialize(modelItem);
        };

        _albumsListStore = ListStore.New(AlbumsAlbumModel.GetGType());
        _albumsSelection = SingleSelection.New(_albumsListStore);
        _albumsGridView.SetFactory(_signalListItemFactory);
        _albumsGridView.SetModel(_albumsSelection);
        
        _albumsGridView.OnActivate += AlbumsGridViewOnOnActivate;
    }

    private void AlbumsGridViewOnOnActivate(GridView sender, GridView.ActivateSignalArgs args)
    {
        if (_albumsSelection.SelectedItem is not AlbumsAlbumModel selectedModel) return;
        
        // We just use the first album artist as the artist to show in the hierarchy. 
        AlbumSelected?.Invoke(selectedModel.Album, selectedModel.Album.CreditsInfo.AlbumArtists[0]);
    }
    
    public void ShowAlbums(IReadOnlyList<AlbumsAlbumModel> albums)
    {
        _albumsListStore.RemoveAll();
        var albumsList = albums.ToList();
        foreach (var album in albumsList) _albumsListStore.Append(album);
    }
}