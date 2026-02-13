using Adw;
using Aria.Infrastructure;
using Gio;
using GLib;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Albums;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Albums.AlbumsPage.ui")]
public partial class AlbumsPage
{
    public enum AlbumSorting
    {
        Title,
        TitleDescending,
        ReleaseDate,
        ReleaseDateDescending
    }    
    
    [Connect("albums-grid-view")] private GridView _gridView;
    [Connect("artist-stack")] private Stack _artistStack;

    [Connect("gesture-click")] private GestureClick _gestureClick;
    [Connect("gesture-long-press")] private GestureLongPress _gestureLongPress;
    [Connect("album-popover-menu")] private PopoverMenu _albumPopoverMenu;
    
    // Raw Storage
    private ListStore _listModel;
    
    // Sorting
    private CustomSorter _sorter;
    private SimpleAction _sorterAction;
    private SortListModel _sortedListModel;
    private AlbumSorting _sorting = AlbumSorting.Title;    
    
    // Selection
    private SingleSelection _singleSelection;
    
    // Presentation
    private SignalListItemFactory _itemFactory;
    
    private AlbumsAlbumModel? _contextMenuItem;
    
    partial void Initialize()
    {
        InitializeActions();
        InitializeGridView();
    }

    public void ShowAlbums(IReadOnlyList<AlbumsAlbumModel> models)
    {
        Clear();
        foreach (var model in models) _listModel.Append(model);
    }
    
    private void InitializeGridView()
    {
        // Raw Data
        _listModel = ListStore.New(AlbumsAlbumModel.GetGType());
        
        // Sorting
        CompareDataFuncT<AlbumsAlbumModel> sortAlbum = SortAlbum;
        _sorter = CustomSorter.New(sortAlbum);
        _sortedListModel = SortListModel.New(_listModel, _sorter);
        _sorterAction.OnChangeState += SorterActionOnOnChangeState;
        
        // Selection
        _singleSelection = SingleSelection.New(_sortedListModel);
        
        // Presentation
        _itemFactory =SignalListItemFactory.NewWithProperties([]);
        _itemFactory.OnSetup += OnItemFactoryOnOnSetup;
        _itemFactory.OnBind += OnItemFactoryOnOnBind;
        
        _gridView.SetFactory(_itemFactory);
        _gridView.SetModel(_singleSelection);
        
        _gridView.SingleClickActivate = true; // TODO: Move to .UI
        _gridView.OnActivate += GridViewOnOnActivate;
        _gestureClick.OnPressed += GestureClickOnOnPressed;
        _gestureLongPress.OnPressed += GestureLongPressOnOnPressed;
    }

    private void SorterActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        var value = args.Value?.GetString(out _);
        var sorting = Enum.TryParse<AlbumSorting>(value, out var parsed)
            ? parsed
            : AlbumSorting.Title;
        SetActiveSorting(sorting);
    }

    private int SortAlbum(AlbumsAlbumModel a, AlbumsAlbumModel b)
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

    public void SetActiveSorting(AlbumSorting filter)
    {
        _sorting = filter;
        
        _sorterAction.SetState(Variant.NewString(filter.ToString()));
        
        // Tell the actual sorter our preference has changed
        _sorter.Changed(SorterChange.Different);
    }
    
    private void Clear()
    {
        foreach (var dragSource in _albumDragSources)
        {
            dragSource.OnDragBegin -= AlbumOnOnDragBegin;
            dragSource.OnPrepare -= AlbumOnPrepare;            
        }
        _albumDragSources.Clear();
        
        // Dispose models (and their owned textures) before removing them from the store
        var n = (int)_listModel.GetNItems();
        for (var i = n - 1; i >= 0; i--)
        {
            var obj = _listModel.GetObject((uint)i);
            if (obj is not AlbumsAlbumModel model) continue;

            model.CoverTexture = null;
            model.Dispose();
        }        
        _listModel.RemoveAll();        
    }
    
    private void GridViewOnOnActivate(GridView sender, GridView.ActivateSignalArgs args)
    {
        if (_singleSelection.SelectedItem is not AlbumsAlbumModel selectedModel) return;

        var parameter = selectedModel.Album.Id.ToVariant(); 
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", parameter);        
    }
    
    private static void OnItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (AlbumsAlbumModel)listItem.GetItem()!;
        var widget = (AlbumsAlbumListItem)listItem.GetChild()!;
        widget.Bind(modelItem);
    }
}