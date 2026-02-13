using Adw;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using Gio;
using GLib;
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

    public enum AlbumSorting
    {
        Title,
        TitleDescending,
        ReleaseDate,
        ReleaseDateDescending
    }

    private const string EmptyPageName = "empty-stack-page";
    private const string ArtistPageName = "artist-stack-page";

    [Connect("albums-grid-view")] private GridView _gridView;
    [Connect("artist-stack")] private Stack _artistStack;

    [Connect("gesture-click")] private GestureClick _gestureClick;
    [Connect("gesture-long-press")] private GestureLongPress _gestureLongPress;
    [Connect("album-popover-menu")] private PopoverMenu _albumPopoverMenu;

    // Raw storage
    private ListStore _listModel;

    // Sorting
    private CustomSorter _sorter;
    private SimpleAction _sorterAction;
    private SortListModel _sortedListModel;
    private AlbumSorting _sorting = AlbumSorting.Title;

    // Selection
    private SingleSelection _singleSelection;

    // Presentation
    private SignalListItemFactory _signalListItemFactory;

    private ArtistInfo _artist;

    private AlbumModel? _contextMenuItem;

    public event Action<AlbumInfo, ArtistInfo>? AlbumSelected;

    partial void Initialize()
    {
        InitializeActions();
        InitializeGridView();
    }

    public void SetActiveSorting(AlbumSorting filter)
    {
        _sorting = filter;

        _sorterAction.SetState(Variant.NewString(filter.ToString()));

        // Tell the actual sorter our preference has changed
        _sorter.Changed(SorterChange.Different);
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

        foreach (var album in albumModels) _listModel.Append(album);
    }

    private void InitializeGridView()
    {
        // Raw Data
        _listModel = ListStore.New(AlbumModel.GetGType());

        // Sorting
        CompareDataFuncT<AlbumModel> sortAlbum = SortAlbum;
        _sorter = CustomSorter.New(sortAlbum);
        _sortedListModel = SortListModel.New(_listModel, _sorter);
        _sorterAction.OnChangeState += SorterActionOnOnChangeState;

        // Selection
        _singleSelection = SingleSelection.New(_sortedListModel);

        // Presentation
        _signalListItemFactory = SignalListItemFactory.NewWithProperties([]);


        _gridView.SetFactory(_signalListItemFactory);
        _gridView.SetModel(_singleSelection);

        _signalListItemFactory.OnSetup += OnSignalListItemFactoryOnOnSetup;
        _signalListItemFactory.OnBind += OnSignalListItemFactoryOnOnBind;

        _gridView.SingleClickActivate = true; // TODO: Move to .UI
        _gridView.OnActivate += GridViewOnOnActivate;

        _gestureClick.OnPressed += GestureClickOnOnPressed;
        _gestureLongPress.OnPressed += GestureLongPressOnOnPressed;
    }

    private int SortAlbum(AlbumModel a, AlbumModel b)
    {
        switch (_sorting)
        {
            default:
            case AlbumSorting.Title:
                return string.Compare(a.Album.Title, b.Album.Title, StringComparison.OrdinalIgnoreCase) switch
                {
                    < 0 => -1,
                    > 0 => 1,
                    _ => 0
                };

            case AlbumSorting.TitleDescending:
                return string.Compare(a.Album.Title, b.Album.Title, StringComparison.OrdinalIgnoreCase) switch
                {
                    < 0 => 1,
                    > 0 => -1,
                    _ => 0
                };
            case AlbumSorting.ReleaseDate:
                return a.Album.ReleaseDate?.CompareTo(b.Album.ReleaseDate) ?? 0;

            case AlbumSorting.ReleaseDateDescending:
                return b.Album.ReleaseDate?.CompareTo(a.Album.ReleaseDate) ?? 0;
        }
    }

    private void SorterActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        var value = args.Value?.GetString(out _);
        var sorting = Enum.TryParse<AlbumSorting>(value, out var parsed)
            ? parsed
            : AlbumSorting.Title;
        SetActiveSorting(sorting);
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
        var n = (int)_listModel.GetNItems();
        for (var i = n - 1; i >= 0; i--)
        {
            var obj = _listModel.GetObject((uint)i);
            if (obj is not AlbumModel model) continue;

            model.CoverTexture = null;
            model.Dispose();
        }

        _listModel.RemoveAll();
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
        _contextMenuItem = (AlbumModel)_listModel.GetObject(selected)!;

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