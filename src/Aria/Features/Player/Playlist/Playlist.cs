using Aria.Core.Extraction;
using Aria.Core.Library;
using Gdk;
using GObject;
using Gtk;
using GId = Aria.Infrastructure.GId;
using ListStore = Gio.ListStore;

namespace Aria.Features.Player.Playlist;

[Subclass<GObject.Object>]
public partial class GQueueTrackId
{
    public GQueueTrackId(Id id) : this()
    {
        Id = id;
    }
    public Id Id { get; set; }
}

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
    
    private readonly List<DragSource> _trackDragSources = [];
    private readonly List<DropTarget> _idTrackDropTargets = [];
    private readonly List<DropTarget> _playlistPositionTrackDropTargets = [];

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

        TogglePage(PlaylistPages.Empty);

        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += OnSignalListItemFactoryOnOnSetup;
        _signalListItemFactory.OnBind += OnSignalListItemFactoryOnOnBind;

        _tracksListStore = ListStore.New(TrackModel.GetGType());
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

    private void Clean()
    {
        foreach (var source in _trackDragSources)
        {
            source.OnPrepare -= TrackOnDragPrepare;
        }

        foreach (var target in _idTrackDropTargets)
        {
            target.OnDrop -= TrackOnGIdDropped;
        }

        foreach (var target in _playlistPositionTrackDropTargets)
        {
            target.OnDrop -= TrackOnPlaylistPositionDropped;
        }
        
        _trackDragSources.Clear();
        _idTrackDropTargets.Clear();
        _playlistPositionTrackDropTargets.Clear();
        _tracksListStore.RemoveAll();        
    }
    
    public void RefreshTracks(IEnumerable<QueueTrackInfo> tracks)
    {
        Clean();
        
        foreach (var queueTrack in tracks)
        {
            var track = queueTrack.Track;
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
            
            var item = new TrackModel(track?.Id ?? Id.Empty, queueTrack.Id ?? Id.Empty, queueTrack.Position, titleText, subTitleText, composers,
                track?.Duration ?? TimeSpan.Zero);
            _tracksListStore.Append(item);
        }
    }

    private ContentProvider? TrackOnDragPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (TrackListItem)sender.GetWidget()!;
        var data = new GQueueTrackId(widget.Model.QueueTrackId);
        var value = new Value(data);
        return ContentProvider.NewForValue(value);       
    }
    
    private void OnSignalListItemFactoryOnOnBind(SignalListItemFactory _, SignalListItemFactory.BindSignalArgs args)
    {
        var listItem = (ListItem)args.Object;
        var modelItem = (TrackModel)listItem.GetItem()!;
        var widget = (TrackListItem)listItem.GetChild()!;
        widget.Update(modelItem);
    }

    private void OnSignalListItemFactoryOnOnSetup(SignalListItemFactory _, SignalListItemFactory.SetupSignalArgs args)
    {
        var item = (ListItem)args.Object;
        var child = new TrackListItem();
        
        // Drag source
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Move;
        dragSource.OnPrepare += TrackOnDragPrepare;
        _trackDragSources.Add(dragSource);           
        child.AddController(dragSource);        

        // Drag target
        var type = GObject.Type.Object;
        var idWrapperDropTarget = DropTarget.New(type, DragAction.Copy);
        idWrapperDropTarget.OnDrop += TrackOnGIdDropped;
        _idTrackDropTargets.Add(idWrapperDropTarget);       
        
        var playlistPositionDropTarget = DropTarget.New(type, DragAction.Move);
        playlistPositionDropTarget.OnDrop += TrackOnPlaylistPositionDropped;
        _playlistPositionTrackDropTargets.Add(playlistPositionDropTarget);       

        child.AddController(idWrapperDropTarget);
        child.AddController(playlistPositionDropTarget);       
        item.SetChild(child);
    }
    
    private bool TrackOnGIdDropped(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something on a track in this playlist.
        var value = args.Value.GetObject();

        if (value is not GId gId) return false;
        
        var widget = (TrackListItem)sender.Widget!;
        EnqueueRequested(this, (gId.Id, widget.Model.Position));
        
        return true;
    }
    
    private bool TrackOnPlaylistPositionDropped(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something on a track in this playlist.
        var value = args.Value.GetObject();

        if (value is not GQueueTrackId queueTrackId) return false;
        var widget = (TrackListItem)sender.Widget!;
        
        MoveRequested(this, (queueTrackId.Id, widget.Model.Position));
        return true;
    }
}