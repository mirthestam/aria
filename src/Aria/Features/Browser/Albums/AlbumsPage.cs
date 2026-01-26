using Adw;
using Aria.Core.Library;
using Gdk;
using GObject;
using Gtk;
using GId = Aria.Infrastructure.GId;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Albums;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Albums.AlbumsPage.ui")]
public partial class AlbumsPage
{
    [Connect("albums-grid-view")] private GridView _albumsGridView;
    private ListStore _albumsListStore;
    private SingleSelection _albumsSelection;
    [Connect("artist-stack")] private Stack _artistStack;
    private SignalListItemFactory _signalListItemFactory;

    public event Action<AlbumInfo, ArtistInfo>? AlbumSelected;

    partial void Initialize()
    {
        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            var item = (ListItem)args.Object;
            var child = new AlbumsAlbumListItem();
            var dragSource = DragSource.New();
            dragSource.Actions = DragAction.Copy;
            dragSource.OnDragBegin += AlbumOnOnDragBegin;
            dragSource.OnPrepare += AlbumOnPrepare;
            child.AddController(dragSource);

            item.SetChild(child);
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

    private void AlbumOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
        var widget = (AlbumsAlbumListItem)sender.GetWidget()!;
        var cover = widget.Model!.CoverTexture;
        if (cover == null) return;

        var coverPicture = Picture.NewForPaintable(cover);
        coverPicture.AddCssClass("cover");
        coverPicture.CanShrink = true;
        coverPicture.ContentFit = ContentFit.ScaleDown;
        coverPicture.AlternativeText = widget.Model.Album.Title;

        var clamp = Clamp.New();
        clamp.MaximumSize = 96;
        clamp.SetChild(coverPicture);

        var dragIcon = DragIcon.GetForDrag(args.Drag);
        dragIcon.SetChild(clamp);
    }

    private ContentProvider? AlbumOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (AlbumsAlbumListItem)sender.GetWidget()!;
        var wrapper = new GId(widget.Model!.Album.Id!);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }

    public void ShowAlbums(IReadOnlyList<AlbumsAlbumModel> albums)
    {
        _albumsListStore.RemoveAll();
        var albumsList = albums.ToList();
        foreach (var album in albumsList) _albumsListStore.Append(album);
    }

    private void AlbumsGridViewOnOnActivate(GridView sender, GridView.ActivateSignalArgs args)
    {
        if (_albumsSelection.SelectedItem is not AlbumsAlbumModel selectedModel) return;

        // We just use the first album artist as the artist to show in the hierarchy. 
        AlbumSelected?.Invoke(selectedModel.Album, selectedModel.Album.CreditsInfo.AlbumArtists[0]);
    }
}