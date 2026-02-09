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

    private SignalListItemFactory _signalListItemFactory;
    private ListStore _tracksListStore;
    private SingleSelection _tracksSelection;
    private bool _suppressSelectionEvent;
    
    public event EventHandler<EnqueueRequestedEventArgs> EnqueueRequested;
    public event EventHandler<MoveRequestedEventArgs> MoveRequested;
    public event EventHandler<TrackSelectionChangedEventArgs>? TrackSelectionChanged;

    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        TogglePage(QueuePages.Empty);

        _signalListItemFactory = SignalListItemFactory.NewWithProperties([]);
        _signalListItemFactory.OnSetup += OnSignalListItemFactoryOnOnSetup;
        _signalListItemFactory.OnBind += OnSignalListItemFactoryOnOnBind;

        _tracksListStore = ListStore.New(QueueTrackModel.GetGType());
        _tracksSelection = SingleSelection.New(_tracksListStore);
        _tracksSelection.CanUnselect = true;
        _tracksSelection.Autoselect = false;
        _tracksListView.SetFactory(_signalListItemFactory);
        _tracksListView.SetModel(_tracksSelection);

        _tracksSelection.OnSelectionChanged += (_, _) =>
        {
            if (!_suppressSelectionEvent) TrackSelectionChanged?.Invoke(this, new TrackSelectionChangedEventArgs(_tracksSelection.GetSelected()));
        };
        
        InitializeQueueActionGroup();
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
        _suppressSelectionEvent = true;
        try
        {
            if (index == null) _tracksSelection.UnselectAll();
            else
            {
                var currentSelected = _tracksSelection.GetSelected();

                
                
                if (currentSelected == (uint)index) return;
                
                _tracksSelection.SelectItem((uint)index, true);
                
                if (currentSelected != GtkConstants.GtkInvalidListPosition)
                {
                    _tracksListView.ScrollTo((uint)index, ListScrollFlags.Focus, null);
                }                
            }
        }
        finally
        {
            _suppressSelectionEvent = false;
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

            var currentCount = (int)_tracksListStore.GetNItems();
            if (i < currentCount)
            {
                var currentItemObj = _tracksListStore.GetItem((uint)i);
                if (ReferenceEquals(currentItemObj, desiredItem))
                    continue;

                // Look for the desired item later in the store (move case)
                var foundIndex = -1;
                for (var j = i + 1; j < currentCount; j++)
                {
                    var obj = _tracksListStore.GetItem((uint)j);
                    if (ReferenceEquals(obj, desiredItem))
                    {
                        foundIndex = j;
                        break;
                    }
                }

                if (foundIndex >= 0)
                {
                    // Move: remove at foundIndex, insert at i
                    _tracksListStore.Remove((uint)foundIndex);
                    _tracksListStore.Insert((uint)i, desiredItem);
                }
                else
                {
                    // Insert: new item at i
                    _tracksListStore.Insert((uint)i, desiredItem);
                }
            }
            else
            {
                // Append remaining new items
                _tracksListStore.Append(desiredItem);
            }
        }

        // Remove any extra items at the end
        for (var i = (int)_tracksListStore.GetNItems() - 1; i >= desiredCount; i--)
        {
            _tracksListStore.Remove((uint)i);
        }
    }
    
    private void OnSignalListItemFactoryOnOnSetup(SignalListItemFactory _, SignalListItemFactory.SetupSignalArgs args)
    {
        var item = (ListItem)args.Object;
        var child = TrackListItem.NewWithProperties([]);

        // Drag source
        SetupDragDropForItem(child);

        item.SetChild(child);
    }    
    
    private static void OnSignalListItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (QueueTrackModel)listItem.GetItem()!;
        var widget = (TrackListItem)listItem.GetChild()!;
        widget.Initialize(modelItem);
    }
}