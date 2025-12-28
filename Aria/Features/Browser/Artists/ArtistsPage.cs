using Adw;
using Aria.Core;
using Aria.Core.Library;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Browser.Artists;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Artists.ArtistsPage.ui")]
public partial class ArtistsPage
{
    private ListStore _artistsListStore;

    [Connect("artists-list-view")] private ListView _artistsListView;
    private SingleSelection _artistsSelection;
    private SignalListItemFactory _signalListItemFactory;

    partial void Initialize()
    {
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
        _artistsSelection = SingleSelection.New(_artistsListStore);
        _artistsListView.SetFactory(_signalListItemFactory);
        _artistsListView.SetModel(_artistsSelection);

        _artistsSelection.OnSelectionChanged += ArtistsSelectionOnOnSelectionChanged;
    }

    private void ArtistsSelectionOnOnSelectionChanged(SelectionModel sender,
        SelectionModel.SelectionChangedSignalArgs args)
    {
        var selectedModel = (ArtistModel)_artistsListStore.GetObject(args.Position)!;
        var artistId = selectedModel.Id;
        ArtistSelected?.Invoke(artistId);
    }

    public void RefreshArtists(IEnumerable<ArtistInfo> artists)
    {
        // TODO: I might improve performance to update the list store instead of clearing it
        _artistsListStore.RemoveAll();

        foreach (var artist in artists
                     .OrderBy(a => a.Name))
        {
            var listViewItem = new ArtistModel(artist.Id, artist.Name);
            _artistsListStore.Append(listViewItem);
        }
    }
    
    public event Action<Id>? ArtistSelected;
}