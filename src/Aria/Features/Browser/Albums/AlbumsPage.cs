using Adw;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using GLib;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Albums;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Albums.AlbumsPage.ui")]
public partial class AlbumsPage
{
    [Connect("albums-grid-view")] private GridView _gridView;
    [Connect("artist-stack")] private Stack _artistStack;

    [Connect("gesture-click")] private GestureClick _gestureClick;    
    [Connect("album-popover-menu")] private PopoverMenu _albumPopoverMenu;
    
    private ListStore _listStore;
    private SingleSelection _singleSelection;
    private SignalListItemFactory _itemFactory;
    
    private AlbumsAlbumModel? _contextMenuItem;
    
    partial void Initialize()
    {
        InitializeGridView();
        InitializeActions();
    }

    public void ShowAlbums(IReadOnlyList<AlbumsAlbumModel> models)
    {
        Clear();
        foreach (var model in models) _listStore.Append(model);
    }
    
    private void InitializeGridView()
    {
        _itemFactory =SignalListItemFactory.NewWithProperties([]);
        _itemFactory.OnSetup += OnItemFactoryOnOnSetup;
        _itemFactory.OnBind += OnItemFactoryOnOnBind;

        _listStore = ListStore.New(AlbumsAlbumModel.GetGType());
        _singleSelection = SingleSelection.New(_listStore);
        _gridView.SetFactory(_itemFactory);
        _gridView.SetModel(_singleSelection);
        
        _gridView.SingleClickActivate = true; // TODO: Move to .UI
        _gridView.OnActivate += GridViewOnOnActivate;
        _gestureClick.OnPressed += GestureClickOnOnPressed;
    }
    
    private void Clear()
    {
        foreach (var dragSource in _albumDragSources)
        {
            dragSource.OnDragBegin -= AlbumOnOnDragBegin;
            dragSource.OnPrepare -= AlbumOnPrepare;            
        }
        _albumDragSources.Clear();       
        
        _listStore.RemoveAll();        
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
        widget.Initialize(modelItem);
    }
}