using Adw;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Gio;
using GLib;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Artists;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Artists.ArtistsPage.ui")]
public partial class ArtistsPage
{
    [Connect("artists-list-view")] private ListView _artistsListView;
    [Connect("artists-menu-button")] private MenuButton _artistsMenuButton;
    [Connect("navigation-menu")] private ListBox _navigationMenu;

    private SingleSelection _singleSelection;
    private ListStore _listStore;
    private Dictionary<Id, ArtistModel> _artistModels = new();
    private SignalListItemFactory _signalListItemFactory;

    public SimpleAction ArtistsFilterAction { get; private set; }

    public event Action<ArtistInfo>? ArtistSelected;

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
        _listStore.RemoveAll();
        _artistModels.Clear();
        foreach (var artist in artists)
        {
            var model = ArtistModel.NewForArtistInfo(artist, nameDisplay);
            _artistModels.Add(artist.Id, model);
            _listStore.Append(model);
        }
    }

    private void InitializeArtistsList()
    {
        _signalListItemFactory = SignalListItemFactory.NewWithProperties([]);
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            ((ListItem)args.Object).SetChild(ArtistListItem.NewWithProperties([]));
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            if (listItem.GetItem() is ArtistModel model && listItem.GetChild() is ArtistListItem widget)
                widget.Update(model);
        };

        _listStore = ListStore.New(ArtistModel.GetGType());

        _singleSelection = SingleSelection.New(_listStore);
        _singleSelection.Autoselect = false;
        _singleSelection.CanUnselect = true;
        _singleSelection.OnSelectionChanged += SingleSelectionOnOnSelectionChanged;

        _artistsListView.SetFactory(_signalListItemFactory);
        _artistsListView.SetModel(_singleSelection);
    }

    private void InitializeArtistsMenu()
    {
        var actionGroup = SimpleActionGroup.New();
        ArtistsFilterAction = SimpleAction.NewStateful("filter", VariantType.String, Variant.NewString("Artists"));
        actionGroup.AddAction(ArtistsFilterAction);
        InsertActionGroup("artists", actionGroup);
        _artistsMenuButton.InsertActionGroup("artists", actionGroup);
    }

    private void SingleSelectionOnOnSelectionChanged(SelectionModel sender,
        SelectionModel.SelectionChangedSignalArgs args)
    {
        if (_singleSelection.SelectedItem is not ArtistModel selectedModel) return;

        _navigationMenu.UnselectAll();
        ArtistSelected?.Invoke(selectedModel.Artist);
    }

    public void Unselect()
    {
        _singleSelection.UnselectAll();
    }

    public void SelectArtist(Id artistId)
    {
        if (!_artistModels.TryGetValue(artistId, out var model))
        {
            _singleSelection.UnselectAll();
            return;
        }

        if (!_listStore.Find(model, out var position))
        {
            // I return now. However, it would be nicer,
            // if we have a sort and filter decorator here.
            // Also, because when I change the filter I don't want to lose the selection
            _singleSelection.UnselectAll();
            return;
        }

        _singleSelection.SelectItem(position, true);
        _artistsListView.ScrollTo(position, ListScrollFlags.None, null);
    }
}