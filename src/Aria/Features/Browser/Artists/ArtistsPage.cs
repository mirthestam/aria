using Adw;
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
    [Connect("artists-list-view")] private ListView _artistsListView;
    [Connect("artists-menu-button")] private MenuButton _artistsMenuButton;
    [Connect("navigation-menu")] private ListBox _navigationMenu;
    
    private SingleSelection _artistsSelectionModel;
    private ListStore _artistsListStore;    
    private SignalListItemFactory _signalListItemFactory;
    
    public SimpleAction ArtistsFilterAction { get; private set; }   

    public event Action<ArtistInfo>? ArtistSelected;
    public event Action? AllAlbumsRequested;    
    
    partial void Initialize()
    {
        InitializeArtistsList();
        InitializeArtistsMenu();
    }

    public void SetActiveFilter(ArtistsFilter filter)
    {
        var displayName = filter switch
        {
            ArtistsFilter.Artists => "All Artists",    
            ArtistsFilter.Main => "Artists",
            ArtistsFilter.Composers => "Composers",
            ArtistsFilter.Conductors => "Conductors",
            ArtistsFilter.Ensembles => "Ensembles",
            ArtistsFilter.Performers => "Performers",
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
        
        ArtistsFilterAction.SetState(Variant.NewString(filter.ToString()));
        _artistsMenuButton.SetLabel(displayName);
    }
    
    public void RefreshArtists(IEnumerable<ArtistInfo> artists, ArtistNameDisplay nameDisplay)
    {
        _artistsListStore.RemoveAll();
        foreach (var artist in artists)
        {
            var listViewItem = new ArtistModel(artist, nameDisplay);
            _artistsListStore.Append(listViewItem);
        }
    }    
    
    private void InitializeArtistsList()
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

    private void InitializeArtistsMenu()
    {
        var actionGroup = SimpleActionGroup.New();
        ArtistsFilterAction = SimpleAction.NewStateful("filter", VariantType.String, Variant.NewString("Artists"));
        actionGroup.AddAction(ArtistsFilterAction);
        InsertActionGroup("artists", actionGroup);
        _artistsMenuButton.InsertActionGroup("artists", actionGroup);
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
}