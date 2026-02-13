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
    
    [Connect("albums-grid-view")] private GridView _gridView;
    [Connect("artist-stack")] private Stack _artistStack;

    [Connect("gesture-click")] private GestureClick _gestureClick;
    [Connect("gesture-long-press")] private GestureLongPress _gestureLongPress;
    [Connect("album-popover-menu")] private PopoverMenu _albumPopoverMenu;

    private ArtistInfo _artist;

    private ListStore _listStore;
    private SingleSelection _singleSelection;
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

        foreach (var album in albumModels) _listStore.Append(album);
    }

    private void InitializeGridView()
    {
        _signalListItemFactory = SignalListItemFactory.NewWithProperties([]);

        _listStore = ListStore.New(AlbumModel.GetGType());
        _singleSelection = SingleSelection.New(_listStore);
        _gridView.SetFactory(_signalListItemFactory);
        _gridView.SetModel(_singleSelection);

        _signalListItemFactory.OnSetup += OnSignalListItemFactoryOnOnSetup;
        _signalListItemFactory.OnBind += OnSignalListItemFactoryOnOnBind;

        _gridView.SingleClickActivate = true; // TODO: Move to .UI
        _gridView.OnActivate += GridViewOnOnActivate;
        
        _gestureClick.OnPressed += GestureClickOnOnPressed;
        _gestureLongPress.OnPressed += GestureLongPressOnOnPressed;
    }
    
    private void Clear()
    {
        foreach (var dragSource in _albumDragSources)
        {
            dragSource.OnDragBegin -= AlbumOnDragBegin;
            dragSource.OnPrepare -= AlbumOnPrepare;
        }

        _albumDragSources.Clear();

        // Dispose models (and their owned textures) before removing them from the store
        var n = (int)_listStore.GetNItems();
        for (var i = n - 1; i >= 0; i--)
        {
            var obj = _listStore.GetObject((uint)i);
            if (obj is not AlbumModel model) continue;

            model.CoverTexture = null;
            model.Dispose();
        }        
        _listStore.RemoveAll();
    }

    private void ShowContextMenu(double x, double y)
    {
        // The grid is in single click activate mode.
        // That means that hover changes the selection.
        // The user 'is' able to hover even when the context menu is shown.
        // Therefore, I remember the hovered item at the moment the menu was shown.

        // To be honest, this is probably not the 'correct' approach
        // as right-clicking outside an item also invokes this logic.

        // But it works, and I have been unable to find out the correct way.

        var selected = _singleSelection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        _contextMenuItem = (AlbumModel)_listStore.GetObject(selected)!;

        var rect = new Rectangle
        {
            X = (int)Math.Round(x),
            Y = (int)Math.Round(y),
        };

        _albumPopoverMenu.SetPointingTo(rect);

        if (!_albumPopoverMenu.Visible)
            _albumPopoverMenu.Popup();        
    }
    
    private void GestureLongPressOnOnPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        ShowContextMenu(args.X, args.Y);
    }
    
    private void GestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        ShowContextMenu(args.X, args.Y);
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