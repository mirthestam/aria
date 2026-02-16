using Adw;
using Aria.Features.Browser.Shared;
using Aria.Infrastructure;
using Gdk;
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
    [Connect("album-popover-menu")] private PopoverMenu _albumPopoverMenu;
    
    // Raw Storage
    private ListStore _listModel;
    
    // Sorting
    private CustomSorter _sorter;
    private SimpleAction _sorterAction;
    private SortListModel _sortedListModel;
    private AlbumSorting _sorting = AlbumSorting.Title;    
    
    // Selection
    private SingleSelection _selection;
    
    // Presentation
    private SignalListItemFactory _itemFactory;
    
    partial void Initialize()
    {
        InitializeActions();
        InitializeGridView();
    }

    public void ShowAlbums(IReadOnlyList<AlbumModel> models)
    {
        Clear();
        foreach (var model in models) _listModel.Append(model);
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
        _selection = SingleSelection.New(_sortedListModel);
        
        // Presentation
        _itemFactory =SignalListItemFactory.NewWithProperties([]);
        _itemFactory.OnSetup += OnItemFactoryOnOnSetup;
        _itemFactory.OnTeardown += ItemFactoryOnOnTeardown;
        _itemFactory.OnBind += OnItemFactoryOnOnBind;
        
        _gridView.SetFactory(_itemFactory);
        _gridView.SetModel(_selection);
        
        _gridView.OnActivate += GridViewOnOnActivate;
    }
    
    private void SorterActionOnOnChangeState(SimpleAction sender, SimpleAction.ChangeStateSignalArgs args)
    {
        var value = args.Value?.GetString(out _);
        var sorting = Enum.TryParse<AlbumSorting>(value, out var parsed)
            ? parsed
            : AlbumSorting.Title;
        SetActiveSorting(sorting);
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

    public void SetActiveSorting(AlbumSorting filter)
    {
        _sorting = filter;
        
        _sorterAction.SetState(Variant.NewString(filter.ToString()));
        
        // Tell the actual sorter our preference has changed
        _sorter.Changed(SorterChange.Different);
    }
    
    private void Clear()
    {
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
    
    private void ShowAlbumContextMenu(AlbumListItem listItem, double x, double y)
    {
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        
        var pointInItem = new Graphene.Point { X = (float)x, Y = (float)y };        
        
        if (!listItem.ComputePoint(_gridView, pointInItem, out var pointInListView))
            return;
        
        var rect = new Rectangle();
        rect.X = (int)Math.Round(pointInListView.X);
        rect.Y = (int)Math.Round(pointInListView.Y);
        rect.Width = 1;
        rect.Height = 1;
        
        _albumPopoverMenu.SetPointingTo(rect);

        if (!_albumPopoverMenu.Visible)
            _albumPopoverMenu.Popup();        
    }
    
    private void GestureClickOnOnReleased(GestureClick sender, GestureClick.ReleasedSignalArgs args)
    {
        if (sender.Widget is not AlbumListItem listItem) return;
        _listModel.Find(listItem.Model!, out var position);
        if (!_selection.IsSelected(position)) _selection.SelectItem(position, true);        

        var button = sender.GetCurrentButton();
        switch (button)
        {
            case Gdk.Constants.BUTTON_PRIMARY:
                ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", Variant.NewString(listItem.Model!.Album.Id.ToString()));
                break;
            
            case Gdk.Constants.BUTTON_SECONDARY:
                ShowAlbumContextMenu(listItem, args.X, args.Y);                
                break;
        }
    }
    
    private void GestureLongPressOnOnPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        if (sender.Widget is not AlbumListItem listItem) return;
        _listModel.Find(listItem.Model!, out var position);
        
        if (!_selection.IsSelected(position)) _selection.SelectItem(position, true);
        
        ShowAlbumContextMenu(listItem, args.X, args.Y);
    }
    
    private void OnItemFactoryOnOnSetup(SignalListItemFactory factory, SignalListItemFactory.SetupSignalArgs args)
    {
        var item = (ListItem)args.Object;
        var child = AlbumListItem.NewWithProperties([]);
        
        // Gestures
        child.GestureClick.OnReleased += GestureClickOnOnReleased;
        child.GestureLongPress.OnPressed += GestureLongPressOnOnPressed;
        
        item.SetChild(child);
    }

    private void ItemFactoryOnOnTeardown(SignalListItemFactory sender, SignalListItemFactory.TeardownSignalArgs args)
    {
        var item = (ListItem)args.Object;
        if (item.Child is not AlbumListItem child) return;
        
        // Gestures
        child.GestureClick.OnReleased -= GestureClickOnOnReleased;
        child.GestureLongPress.OnPressed -= GestureLongPressOnOnPressed;
    }
    
    private void GridViewOnOnActivate(GridView sender, GridView.ActivateSignalArgs args)
    {
        if (_selection.SelectedItem is not AlbumModel selectedModel) return;

        var parameter = selectedModel.Album.Id.ToVariant(); 
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", parameter);        
    }
    
    private static void OnItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (AlbumModel)listItem.GetItem()!;
        var widget = (AlbumListItem)listItem.GetChild()!;
        widget.Bind(modelItem);
    }
}