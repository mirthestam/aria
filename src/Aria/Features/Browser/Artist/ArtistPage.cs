using Adw;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
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

    private ArtistInfo _artist;
    [Connect("artist-stack")] private Stack _artistStack;
    private SignalListItemFactory _signalListItemFactory;
    public event Action<AlbumInfo, ArtistInfo>? AlbumSelected;

    partial void Initialize()
    {
        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            var item = (ListItem)args.Object;
            var child = new AlbumListItem();
            var dragSource = DragSource.New();
            dragSource.Actions = DragAction.Copy;
            dragSource.OnDragBegin += AlbumOnDragBegin;
            dragSource.OnPrepare += AlbumOnPrepare;
            child.AddController(dragSource);            
            item.SetChild(child);
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            var modelItem = (AlbumModel)listItem.GetItem()!;
            var widget = (AlbumListItem)listItem.GetChild()!;
            widget.Initialize(modelItem);
        };

        _albumsListStore = ListStore.New(AlbumModel.GetGType());
        _albumsSelection = SingleSelection.New(_albumsListStore);
        _albumsGridView.SetFactory(_signalListItemFactory);
        _albumsGridView.SetModel(_albumsSelection);

        _albumsGridView.OnActivate += AlbumsGridViewOnOnActivate;
    }

    private ContentProvider? AlbumOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (AlbumListItem)sender.GetWidget()!;
        var wrapper = new GId(widget.Model!.Album.Id!);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }

    private void AlbumOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
        var widget = (AlbumListItem)sender.GetWidget()!;
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

    public void ShowArtist(ArtistInfo artistInfo, IReadOnlyList<AlbumModel> albumModels)
    {
        _albumsListStore.RemoveAll();

        _artist = artistInfo;
        SetTitle(artistInfo.Name);

        foreach (var album in albumModels) _albumsListStore.Append(album);
    }

    private void AlbumsGridViewOnOnActivate(GridView sender, GridView.ActivateSignalArgs args)
    {
        if (_albumsSelection.SelectedItem is not AlbumModel selectedModel) return;

        AlbumSelected?.Invoke(selectedModel.Album, _artist);
    }
}