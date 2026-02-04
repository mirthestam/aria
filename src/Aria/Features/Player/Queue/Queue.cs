using Aria.Core.Extraction;
using Gdk;
using GObject;
using Gtk;
using GId = Aria.Infrastructure.GId;
using ListStore = Gio.ListStore;

namespace Aria.Features.Player.Queue;



[Subclass<Stack>]
[Template<AssemblyResource>("Aria.Features.Player.Queue.Queue.ui")]
public partial class Queue
{
    private const uint GTK_INVALID_LIST_POSITION = 4294967295;
    
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

    public event EventHandler<(Id id, int index)> EnqueueRequested;
    public event EventHandler<(Id sourceId, int targetIndex)> MoveRequested;
    public event EventHandler<uint>? TrackSelectionChanged;

    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        TogglePage(QueuePages.Empty);

        _signalListItemFactory = new SignalListItemFactory();
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
            if (!_suppressSelectionEvent) TrackSelectionChanged?.Invoke(this, _tracksSelection.GetSelected());
        };
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
                
                if (currentSelected != GTK_INVALID_LIST_POSITION)
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

    private ContentProvider? TrackOnDragPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (TrackListItem)sender.GetWidget()!;
        var data = new GQueueTrackId(widget.Model!.QueueTrackId);
        var value = new Value(data);
        return ContentProvider.NewForValue(value);
    }

    private void OnSignalListItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (QueueTrackModel)listItem.GetItem()!;
        var widget = (TrackListItem)listItem.GetChild()!;
        widget.Initialize(modelItem);
    }

    private void OnSignalListItemFactoryOnOnSetup(SignalListItemFactory _, SignalListItemFactory.SetupSignalArgs args)
    {
        var item = (ListItem)args.Object;
        var child = new TrackListItem();

        // Drag source
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Move;
        dragSource.OnPrepare += TrackOnDragPrepare;
        child.AddController(dragSource);

        // Drag targets
        var type = GObject.Type.Object;

        var idWrapperDropTarget = DropTarget.New(type, DragAction.Copy);
        idWrapperDropTarget.OnDrop += TrackOnGIdDropped;
        child.AddController(idWrapperDropTarget);

        var playlistPositionDropTarget = DropTarget.New(type, DragAction.Move);
        playlistPositionDropTarget.OnDrop += TrackOnPlaylistPositionDropped;
        child.AddController(playlistPositionDropTarget);

        item.SetChild(child);
    }

    private bool TrackOnGIdDropped(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something on a track in this playlist.
        var value = args.Value.GetObject();

        if (value is not GId gId) return false;

        var widget = (TrackListItem)sender.Widget!;
        EnqueueRequested(this, (gId.Id, widget.Model!.Position));

        return true;
    }

    private bool TrackOnPlaylistPositionDropped(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something on a track in this playlist.
        var value = args.Value.GetObject();

        if (value is not GQueueTrackId queueTrackId) return false;
        var widget = (TrackListItem)sender.Widget!;

        MoveRequested(this, (queueTrackId.Id, widget.Model!.Position));
        return true;
    }
}