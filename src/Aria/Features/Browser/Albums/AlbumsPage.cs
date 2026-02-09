using Adw;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
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
    
    public event Action<AlbumInfo, ArtistInfo>? AlbumSelected;
    
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
    
    private void GestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        // The grid is in single click activate mode.
        // That means that hover changes the selection.
        // The user 'is' able to hover even when the context menu is shown.
        // Therefore, I remember the hovered item at the moment the menu was shown.
        
        // To be honest, this is probably not the 'correct' approach
        // as right-clicking outside an item also invokes this logic.
        
        // But it works and I have been unable to find out the correct way.
        
        var selected = _singleSelection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        _contextMenuItem = (AlbumsAlbumModel) _listStore.GetObject(selected)!;
        
        var rect = new Rectangle
        {
            X = (int)Math.Round(args.X),
            Y = (int)Math.Round(args.Y),
        };

        _albumPopoverMenu.SetPointingTo(rect);

        if (!_albumPopoverMenu.Visible)
            _albumPopoverMenu.Popup();
    }
    
    private void GridViewOnOnActivate(GridView sender, GridView.ActivateSignalArgs args)
    {
        if (_singleSelection.SelectedItem is not AlbumsAlbumModel selectedModel) return;

        // We just use the first album artist as the artist to show in the hierarchy. 
        AlbumSelected?.Invoke(selectedModel.Album, selectedModel.Album.CreditsInfo.AlbumArtists[0]);
    }
    
    private static void OnItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (AlbumsAlbumModel)listItem.GetItem()!;
        var widget = (AlbumsAlbumListItem)listItem.GetChild()!;
        widget.Initialize(modelItem);
    }
}