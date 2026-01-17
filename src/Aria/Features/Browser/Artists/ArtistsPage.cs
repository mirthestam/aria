using Adw;
using Aria.Core.Library;
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
    [Connect("artists-list-view")] private ListView _artistsListView;
    
    private SingleSelection _artistsSelectionModel;

    // TODO: Currently, albums open on double-click. Update to open on single-click instead.

    [Connect("navigation-menu")] private ListBox _navigationMenu;
    private SignalListItemFactory _signalListItemFactory;

    public event Action<ArtistInfo>? ArtistSelected;
    public event Action? AllAlbumsRequested;    
    
    partial void Initialize()
    {


        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            ((ListItem)args.Object).SetChild(new ArtistListItem());
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            if (listItem.GetItem() is ArtistModel model && listItem.GetChild() is ArtistListItem widget)
                widget.Update(model);
        };

        _artistsListStore = ListStore.New(ArtistModel.GetGType());

        _artistsSelectionModel = SingleSelection.New(_artistsListStore);
        _artistsSelectionModel.Autoselect = false;
        _artistsSelectionModel.OnSelectionChanged += ArtistsSelectionModelOnOnSelectionModelChanged;
        
        _artistsListView.SetFactory(_signalListItemFactory);
        _artistsListView.SetModel(_artistsSelectionModel);        

        _navigationMenu.OnRowActivated += NavigationMenuOnOnRowActivated;
    }

    private void NavigationMenuOnOnRowActivated(ListBox sender, ListBox.RowActivatedSignalArgs args)
    {
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
        _artistsListStore.RemoveAll();

        foreach (var artist in artists
                     .OrderBy(a => a.Name))
        {
            var listViewItem = new ArtistModel(artist);
            _artistsListStore.Append(listViewItem);
        }
    }
}