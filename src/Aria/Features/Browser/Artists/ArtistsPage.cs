using System.Diagnostics;
using Adw;
using Aria.Core;
using Aria.Core.Library;
using Gio;
using GLib;
using GObject;
using Gtk;
using Action = System.Action;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Artists;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Artists.ArtistsPage.ui")]
public partial class ArtistsPage
{
    private ListStore _artistsListStore;
    
    // TODO: Currently, albums open on double-click. Update to open on single-click instead.
    
    [Connect("navigation-menu")] private ListBox _navigationMenu;
    [Connect("artists-list-view")] private ListView _artistsListView;
    
    
    private SingleSelection _artistsSelectionModel;
    private SignalListItemFactory _signalListItemFactory;
    
    private SimpleActionGroup _actionGroup;

    partial void Initialize()
    {
        _actionGroup = SimpleActionGroup.New();
        var primary = SimpleAction.New("primaryartists", VariantType.Checked("b"));
        var composers = SimpleAction.New("composer", VariantType.Checked("b"));
        var conductors = SimpleAction.New("conductor", VariantType.Checked("b"));
        var other = SimpleAction.New("otherartists", VariantType.Checked("b"));
         
        primary.ChangeState(Variant.NewBoolean(true));
        
        _actionGroup.AddAction(primary);
        _actionGroup.AddAction(composers);
        _actionGroup.AddAction(conductors);
        _actionGroup.AddAction(other);
         
        InsertActionGroup("filter", _actionGroup);
        
        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            var item = (ListItem)args.Object;
            item.SetChild(new ArtistListItem());
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            var modelItem = (ArtistModel)listItem.GetItem()!;
            var widget = (ArtistListItem)listItem.GetChild()!;
            widget.Update(modelItem);
        };

        _artistsListStore = ListStore.New(ArtistModel.GetGType());
        _artistsSelectionModel = SingleSelection.New(_artistsListStore);
        _artistsSelectionModel.Autoselect = false;
        _artistsListView.SetFactory(_signalListItemFactory);
        _artistsListView.SetModel(_artistsSelectionModel);

        _artistsSelectionModel.OnSelectionChanged += ArtistsSelectionModelOnOnSelectionModelChanged;
        _navigationMenu.OnRowActivated += NavigationMenuOnOnRowActivated;
    }

    private void NavigationMenuOnOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
        // TODO: maybe better in a handler as this can be triggered externally
        _artistsSelectionModel.UnselectAll();
        AllAlbumsRequested?.Invoke();
    }

    private void ArtistsSelectionModelOnOnSelectionModelChanged(SelectionModel sender,
        SelectionModel.SelectionChangedSignalArgs args)
    {
        if (_artistsSelectionModel.SelectedItem is not ArtistModel selectedModel) return;
        
        _navigationMenu.UnselectAll();
        ArtistSelected?.Invoke(selectedModel.Artist);
    }

    public void RefreshArtists(IEnumerable<ArtistInfo> artists)
    {
        // TODO: I might improve performance to update the list store instead of clearing it
        _artistsListStore.RemoveAll();

        foreach (var artist in artists
                     .OrderBy(a => a.Name))
        {
            var listViewItem = new ArtistModel(artist);
            _artistsListStore.Append(listViewItem);
        }
    }

    public event Action<ArtistInfo>? ArtistSelected;
    public event Action? AllAlbumsRequested;
}