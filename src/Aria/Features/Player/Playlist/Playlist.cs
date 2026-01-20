using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Player.Playlist;

[Subclass<Stack>]
[Template<AssemblyResource>("Aria.Features.Player.Playlist.Playlist.ui")]
public partial class Playlist
{
    public enum PlaylistPages
    {
        Tracks,
        Empty
    }

    private const string EmptyPageName = "empty-playlist-page";
    private const string TracksPageName = "playlist-page";

    private bool _initialized;
    private SignalListItemFactory _signalListItemFactory;

    private ListStore _tracksListStore;

    [Connect("tracks-list-view")] private ListView _tracksListView;
    private SingleSelection _tracksSelection;
    private bool _suppressSelectionEvent;
    public event EventHandler<uint>? TrackSelectionChanged;

    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        TogglePage(PlaylistPages.Empty);

        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            var item = (ListItem)args.Object;
            item.SetChild(new TrackListItem());
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            var modelItem = (TrackModel)listItem.GetItem()!;
            var widget = (TrackListItem)listItem.GetChild()!;
            widget.Update(modelItem);
        };

        _tracksListStore = ListStore.New(TrackModel.GetGType());
        _tracksSelection = SingleSelection.New(_tracksListStore);
        _tracksListView.SetFactory(_signalListItemFactory);
        _tracksListView.SetModel(_tracksSelection);

        _tracksSelection.OnSelectionChanged += (_, _) =>
        {
            if (!_suppressSelectionEvent) TrackSelectionChanged?.Invoke(this, _tracksSelection.GetSelected());
        };

        // TODO: Add context menus to tracks.
        // Should provide access to  playlist functions as well as quick navigation to the artists.
    }

    public void TogglePage(PlaylistPages page)
    {
        var pageName = page switch
        {
            PlaylistPages.Tracks => TracksPageName,
            PlaylistPages.Empty => EmptyPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        SetVisibleChildName(pageName);
    }

    public void SelectTrackIndex(int? index)
    {
        _suppressSelectionEvent = true;
        try
        {
            if (index == null) _tracksSelection.UnselectAll();
            else
            {
                _tracksSelection.SelectItem((uint)index, true);
                _tracksListView.ScrollTo((uint)index, ListScrollFlags.Focus, null);
            }
        }
        finally
        {
            _suppressSelectionEvent = false;
        }
    }

    public void RefreshTracks(IEnumerable<TrackInfo> tracks)
    {
        _tracksListStore.RemoveAll();

        foreach (var track in tracks)
        {
            var titleText = track?.Title ?? "Unnamed track";
            if (track?.Work?.ShowMovement ?? false)
                // For  these kind of works, we ignore the
                titleText = $"{track.Work.MovementName} ({track.Work.MovementNumber} {track.Title} ({track.Work.Work})";


            var credits = track?.CreditsInfo;
            var subTitleText = "";
            var composers = "";

            if (credits != null)
            {
                var artists = string.Join(", ", credits.OtherArtists.Select(x => x.Artist.Name));

                var details = new List<string>();
                var conductors = string.Join(", ", credits.Conductors.Select(x => x.Artist.Name));
                if (!string.IsNullOrEmpty(conductors))
                    details.Add($"{conductors}");

                composers = string.Join(", ", credits.Composers.Select(x => x.Artist.Name));

                subTitleText = artists;
                if (details.Count > 0) subTitleText += $" ({string.Join(", ", details)})";
            }

            var item = new TrackModel(track?.Id ?? Id.Empty, titleText, subTitleText, composers,
                track?.Duration ?? TimeSpan.Zero);
            _tracksListStore.Append(item);
        }
    }
}