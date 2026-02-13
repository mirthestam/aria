using Aria.Infrastructure;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Player.Queue;

[Subclass<Stack>]
[Template<AssemblyResource>("Aria.Features.Player.Queue.Queue.ui")]
public partial class Queue
{
    public enum QueuePages
    {
        Tracks,
        Empty
    }

    private const string EmptyPageName = "empty-playlist-page";
    private const string TracksPageName = "playlist-page";

    private bool _initialized;

    [Connect("tracks-list-view")] private ListView _tracksListView;

    private SignalListItemFactory _itemFactory;
    private ListStore _listStore;
    private SingleSelection _selection;

    [Connect("gesture-click")] private GestureClick _gestureClick;
    [Connect("gesture-long-press")] private GestureLongPress _gestureLongPress;
    [Connect("track-popover-menu")] private PopoverMenu _trackPopoverMenu;

    public event EventHandler<EnqueueRequestedEventArgs> EnqueueRequested;
    public event EventHandler<MoveRequestedEventArgs> MoveRequested;
    public event EventHandler<TrackActivatedEventArgs>? TrackActivated;

    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        TogglePage(QueuePages.Empty);

        InitializeListView();
        InitializeQueueActionGroup();
    }

    private void InitializeListView()
    {
        _itemFactory = SignalListItemFactory.NewWithProperties([]);
        _itemFactory.OnSetup += OnItemFactoryOnOnSetup;
        _itemFactory.OnBind += OnItemFactoryOnOnBind;

        _listStore = ListStore.New(QueueTrackModel.GetGType());
        _selection = SingleSelection.New(_listStore);
        _selection.CanUnselect = true;
        _selection.Autoselect = false;
        _tracksListView.SetFactory(_itemFactory);
        _tracksListView.SetModel(_selection);

        _tracksListView.SingleClickActivate = true; // TODO: Move to .UI
        _tracksListView.OnActivate += TracksListViewOnOnActivate;
        _gestureClick.OnPressed += GestureClickOnOnPressed;
        _gestureLongPress.OnPressed += GestureLongPressOnOnPressed;
    }

    public void TogglePage(QueuePages page)
    {
        var pageName = page switch
        {
            QueuePages.Tracks => TracksPageName,
            QueuePages.Empty => EmptyPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        SetVisibleChildName(pageName);
    }

    public void SelectTrackIndex(int? index)
    {
        if (index == null) _selection.UnselectAll();
        else
        {
            var currentSelected = _selection.GetSelected();
            
            if (currentSelected == (uint)index) return;

            _selection.SelectItem((uint)index, true);

            if (currentSelected != GtkConstants.GtkInvalidListPosition)
            {
                _tracksListView.ScrollTo((uint)index, ListScrollFlags.Focus, null);
            }
        }
    }

    public void RefreshTracks(IEnumerable<QueueTrackModel> tracks)
    {
        // We assume the caller (presenter) reuses QueueTrackModel instances where possible.
        UpdateTracks(tracks as IReadOnlyList<QueueTrackModel> ?? tracks.ToList());
    }

    private void UpdateTracks(IReadOnlyList<QueueTrackModel> desired)
    {
        var desiredCount = desired.Count;

        for (var i = 0; i < desiredCount; i++)
        {
            var desiredItem = desired[i];

            var currentCount = (int)_listStore.GetNItems();
            if (i < currentCount)
            {
                var currentItemObj = _listStore.GetItem((uint)i);
                if (ReferenceEquals(currentItemObj, desiredItem))
                    continue;

                // Look for the desired item later in the store (move case)
                var foundIndex = -1;
                for (var j = i + 1; j < currentCount; j++)
                {
                    var obj = _listStore.GetItem((uint)j);
                    if (ReferenceEquals(obj, desiredItem))
                    {
                        foundIndex = j;
                        break;
                    }
                }

                if (foundIndex >= 0)
                {
                    // Move: remove at foundIndex, insert at i
                    _listStore.Remove((uint)foundIndex);
                    _listStore.Insert((uint)i, desiredItem);
                }
                else
                {
                    // Insert: new item at i
                    _listStore.Insert((uint)i, desiredItem);
                }
            }
            else
            {
                // Append remaining new items
                _listStore.Append(desiredItem);
            }
        }

        // Remove any extra items at the end
        for (var i = (int)_listStore.GetNItems() - 1; i >= desiredCount; i--)
        {
            _listStore.Remove((uint)i);
        }
    }

    private void OnItemFactoryOnOnSetup(SignalListItemFactory _, SignalListItemFactory.SetupSignalArgs args)
    {
        var item = (ListItem)args.Object;
        var child = TrackListItem.NewWithProperties([]);

        // Drag source
        SetupDragDropForItem(child);

        item.SetChild(child);
    }

    private static void OnItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (QueueTrackModel)listItem.GetItem()!;
        var widget = (TrackListItem)listItem.GetChild()!;
        widget.Bind(modelItem);
    }
}