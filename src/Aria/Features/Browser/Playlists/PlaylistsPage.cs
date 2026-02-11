using Adw;
using Aria.Core;
using Aria.Infrastructure;
using Gdk;
using GLib;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Playlists;

// TODO: Context-menu for playlist with enqueue, rename and delete options
// TODO: Load album-art in playlist icon
// TODO: Load album-art in drag & drop 
// TODO: Option to save queue as playlist

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Playlists.PlaylistsPage.ui")]
public partial class PlaylistsPage
{
    public enum PlaylistsPages
    {
        Playlists,
        Empty
    }
    
    private const string EmptyPageName = "empty-stack-page";
    private const string PlaylistsPageName = "playlists-stack-page";
    
    [Connect("playlists-column-view")] private ColumnView _columnView;
    
    [Connect("name-column")] ColumnViewColumn _nameColumn;
    [Connect("modified-column")] ColumnViewColumn _modifiedColumn;
    
    private ListStore _listStore;
    private SingleSelection _selection;
    
    private PlaylistModel? _contextMenuItem;    
    
    partial void Initialize()
    {
        InitializeColumnView();
        InitializeActions();
    }

    public void TogglePage(PlaylistsPages page)
    {
        var pageName = page switch
        {
            PlaylistsPages.Playlists => PlaylistsPageName,
            PlaylistsPages.Empty => EmptyPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        //_artistStack.VisibleChildName = pageName;
    }

    public void ShowPlaylists(List<PlaylistModel> models)
    {
        Clear();
        foreach (var model in models)
        {
            _listStore.Append(model);
        }
    }    
    
    private void InitializeColumnView()
    {
        var nameSorter = CustomSorter.New<PlaylistModel>((a, b) => string.Compare(a.Playlist.Name, b.Playlist.Name, StringComparison.OrdinalIgnoreCase));
        var dateSorter = CustomSorter.New<PlaylistModel>((a, b) => a.Playlist.LastModified.CompareTo(b.Playlist.LastModified));
        
        _nameColumn.SetSorter(nameSorter);
        var nameColumnFactory = (SignalListItemFactory) _nameColumn.Factory!;
        nameColumnFactory.OnSetup += NameColumnSetup;
        nameColumnFactory.OnBind += NameColumnBind;
        
        _modifiedColumn.SetSorter(dateSorter);
        var modifiedColumnFactory =(SignalListItemFactory) _modifiedColumn.Factory!;
        modifiedColumnFactory.OnSetup += ModifiedColumnSetup;
        modifiedColumnFactory.OnBind += ModifiedColumnBind;
        
        _listStore = ListStore.New(PlaylistModel.GetGType());

        var model = SortListModel.New(_listStore, _columnView.Sorter);
        
        _selection = SingleSelection.New(model);
        _selection.Autoselect = false;
    
        _columnView.SetModel(_selection);
        _columnView.OnActivate += ColumnViewOnOnActivate;
        
        _gestureClick.OnPressed += GestureClickOnOnPressed;        
    }

    private void ColumnViewOnOnActivate(ColumnView sender, ColumnView.ActivateSignalArgs args)
    {
        var activatedObject = _listStore.GetObject(args.Position);
        if (activatedObject is not PlaylistModel selectedModel) return;
        
        var argument = Variant.NewString(selectedModel.Playlist.Id.ToString());
        var argumentArray = Variant.NewArray(VariantType.String, [argument]);
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}", argumentArray);    
    }
    
    private void ModifiedColumnSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var label = Label.NewWithProperties([]);
        label.SetXalign(1);
        
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Copy;
        dragSource.OnDragBegin += DragSourceOnOnDragBegin;
        dragSource.OnPrepare += DragSourceOnOnPrepare;
        label.AddController(dragSource);
        
        listItem.SetChild(label);
    }
    private static void ModifiedColumnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var model = (PlaylistModel)listItem.GetItem()!;
        var label = (Label)listItem.GetChild()!;
        label.Label_ = model.Playlist.LastModified.ToShortDateString();
    }

    private void NameColumnSetup(SignalListItemFactory sender, SignalListItemFactory.SetupSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        
        var cell = PlaylistNameCell.NewWithProperties([]);
        
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Copy;
        dragSource.OnDragBegin += DragSourceOnOnDragBegin;
        dragSource.OnPrepare += DragSourceOnOnPrepare;
        cell.AddController(dragSource);
        
        listItem.SetChild(cell);
    }
    
    private static void NameColumnBind(SignalListItemFactory sender, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var model = (PlaylistModel)listItem.GetItem()!;
        var cell = (PlaylistNameCell)listItem.GetChild()!;
        cell.Bind(model);
    }

    private void Clear()
    {
        _listStore.RemoveAll();
    }
}