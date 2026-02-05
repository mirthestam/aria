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
    [Connect("artist-stack")] private Stack _artistStack;

    [Connect("gesture-click")] private GestureClick _gestureClick;
    [Connect("album-popover-menu")] private PopoverMenu _albumPopoverMenu;

    private ArtistInfo _artist;

    private ListStore _albumsListStore;
    private SingleSelection _albumsSelection;
    private SignalListItemFactory _signalListItemFactory;
    
    private AlbumModel? _contextMenuItem;

    public event Action<AlbumInfo, ArtistInfo>? AlbumSelected;

    partial void Initialize()
    {
        InitializeGridView();
        InitializeActions();
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
        Clear();

        _artist = artistInfo;
        SetTitle(artistInfo.Name);

        foreach (var album in albumModels) _albumsListStore.Append(album);
    }

    private void InitializeGridView()
    {
        _signalListItemFactory = new SignalListItemFactory();

        _albumsListStore = ListStore.New(AlbumModel.GetGType());
        _albumsSelection = SingleSelection.New(_albumsListStore);
        _albumsGridView.SetFactory(_signalListItemFactory);
        _albumsGridView.SetModel(_albumsSelection);

        _signalListItemFactory.OnSetup += OnSignalListItemFactoryOnOnSetup;
        _signalListItemFactory.OnBind += OnSignalListItemFactoryOnOnBind;

        _albumsGridView.SingleClickActivate = true; // TODO: Move to .UI
        _albumsGridView.OnActivate += AlbumsGridViewOnOnActivate;
        _gestureClick.OnPressed += GestureClickOnOnPressed;
    }
    
    private void Clear()
    {
        foreach (var dragSource in _albumDragSources)
        {
            dragSource.OnDragBegin -= AlbumOnDragBegin;
            dragSource.OnPrepare -= AlbumOnPrepare;
        }

        _albumDragSources.Clear();

        _albumsListStore.RemoveAll();
    }
    
    private void GestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        // The grid is in single click activate mode.
        // That means that hover changes the selection.
        // The user 'is' able to hover even when the context menu is shown.
        // Therefore, I remember the hovered item at the moment the menu was shown.

        // To be honest, this is probably not the 'correct' approach
        // as right-clicking outside an item also invokes this logic.

        // But it works and I have been unable to find out the correct way.

        var selected = _albumsSelection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        _contextMenuItem = (AlbumModel)_albumsListStore.GetObject(selected)!;

        var rect = new Rectangle
        {
            X = (int)Math.Round(args.X),
            Y = (int)Math.Round(args.Y),
        };

        _albumPopoverMenu.SetPointingTo(rect);

        if (!_albumPopoverMenu.Visible)
            _albumPopoverMenu.Popup();
    }

    private static void OnSignalListItemFactoryOnOnBind(SignalListItemFactory _,
        SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (AlbumModel)listItem.GetItem()!;
        var widget = (AlbumListItem)listItem.GetChild()!;
        widget.Initialize(modelItem);
    }
}