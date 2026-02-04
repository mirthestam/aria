using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Infrastructure;
using Gdk;
using Gio;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Album;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Album.TrackGroup.ui")]
public partial class TrackGroup
{
    [Connect("tracks-listbox")] private ListBox _tracksListBox;
    [Connect("header-label")]  private Label _headerLabel;
    [Connect("credit-box")] private CreditBox _creditBox;
    [Connect("header-box")] private Box _headerBox;
    
    private readonly List<DragSource> _trackDragSources = [];
    
    private IReadOnlyList<TrackArtistInfo> _albumSharedArtists = [];    
    private List<AlbumTrackInfo> _tracks = [];
    
    private SimpleAction _enqueueDefaultAction;
    private SimpleAction _enqueueReplaceAction;
    private SimpleAction _enqueueNextAction;
    private SimpleAction _enqueueEndAction;

    public bool HeaderVisible
    {
        get => _headerBox.Visible;
        set => _headerBox.Visible = value;
    }
    
    partial void Initialize()
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(_enqueueDefaultAction = SimpleAction.New("enqueue-default", null));        
        actionGroup.AddAction(_enqueueReplaceAction = SimpleAction.New("enqueue-replace", null));
        actionGroup.AddAction(_enqueueNextAction = SimpleAction.New("enqueue-next", null));
        actionGroup.AddAction(_enqueueEndAction = SimpleAction.New("enqueue-end", null));
        InsertActionGroup("group", actionGroup);
        
        _enqueueDefaultAction.OnActivate += EnqueueDefaultActionOnOnActivate;
        _enqueueReplaceAction.OnActivate += EnqueueReplaceActionOnOnActivate;
        _enqueueNextAction.OnActivate += EnqueueNextActionOnOnActivate;
        _enqueueEndAction.OnActivate += EnqueueEndActionOnOnActivate;
    }

    private void EnqueueDefaultActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => Enqueue();
    private void EnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => Enqueue(EnqueueAction.EnqueueEnd);
    private void EnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => Enqueue(EnqueueAction.EnqueueNext);
    private void EnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args) => Enqueue(EnqueueAction.Replace);

    private void Enqueue(EnqueueAction? enqueueAction = IQueue.DefaultEnqueueAction)
    {
        var trackList = _tracks.Select(t => t.Track.Id!.ToString()).ToArray();

        switch (enqueueAction)
        {
            case EnqueueAction.Replace:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", Variant.NewStrv(trackList));
                break;
            case EnqueueAction.EnqueueNext:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", Variant.NewStrv(trackList));
                break;
            case EnqueueAction.EnqueueEnd:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", Variant.NewStrv(trackList));
                break;
            case null:
                ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}", Variant.NewStrv(trackList));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(enqueueAction), enqueueAction, null);
        }
    }

    public string? Header
    {
        get => _headerLabel.Label_;
        set
        {
            _headerLabel.Label_ = value;
        }
    }

    public void LoadTracks(List<AlbumTrackInfo> tracks, string? headerText,
        IReadOnlyList<TrackArtistInfo> albumSharedArtists)
    {
        _tracks = tracks;
        _albumSharedArtists = albumSharedArtists;
        Header = headerText;

        if (tracks.Count == 1)
        {
            // Just one track. Header does not make sense
            Header = null;
        }

        UpdateHeader();
        UpdateTracksList();
    }

    public void RemoveTracks()
    {
        // Clean up first
        foreach (var source in _trackDragSources)
        {
            source.OnPrepare -= TrackOnDragPrepare;
            source.OnDragBegin -= TrackOnDragBegin;
        }

        _trackDragSources.Clear();
        _tracksListBox.RemoveAll();
    }

    private void UpdateTracksList()
    {
        RemoveTracks();

        if (_tracks.Count == 0)
        {
            _tracksListBox.SetVisible(false);
            return;
        }
        
        foreach (var albumTrack in _tracks)
        {
            // TODO: We're constructing list items in code here.
            // It would be better to define this via a .UI template.

            // If an album is by "AlbumArtist A", we don't want to repeat "Artist A" next to every track.
            // We only want to show guest artists or different collaborators.

            var track = albumTrack.Track;

            var trackNumberText = albumTrack switch
            {
                { TrackNumber: { } t, VolumeName: { } d and not "" } => $"{d}.{t}",
                { TrackNumber: { } t } => t.ToString(),
                _ => null
            };

            var row = new AlbumTrackRow(track.Id!);

            var prefixLabel = Label.New(trackNumberText);
            prefixLabel.AddCssClass("numeric");
            prefixLabel.AddCssClass("dimmed");
            prefixLabel.SetXalign(1);
            prefixLabel.WidthChars = 4;
            row.AddPrefix(prefixLabel);

            var suffixLabel = Label.New(track.Duration.ToString(@"mm\:ss"));
            suffixLabel.AddCssClass("numeric");
            suffixLabel.AddCssClass("dimmed");

            row.AddSuffix(suffixLabel);
            row.SetUseMarkup(false);
            row.SetTitle(track.Title);

            var guestArtists = SharedArtistHelper.GetUniqueSongArtists(track, _tracks);
            var subTitleLine = string.Join(", ", guestArtists.Select(a => a.Artist.Name));

            row.SetSubtitle(subTitleLine);

            row.SetActivatable(true);
            row.SetActionName("album.enqueue-track-default");

            var value = new Value(new GId(track.Id!));

            row.SetActionTargetValue(Variant.NewString(track.Id?.ToString() ?? string.Empty));

            var dragSource = DragSource.New();
            dragSource.Actions = DragAction.Copy;
            dragSource.OnDragBegin += TrackOnDragBegin;
            dragSource.OnPrepare += TrackOnDragPrepare;
            _trackDragSources.Add(dragSource);
            row.AddController(dragSource);
            
            _tracksListBox.Append(row);
        }
    }

    private void UpdateHeader()
    {
        var sharedArtists = SharedArtistHelper.GetSharedArtists(_tracks).ToList();
        
        // Remove the shared artists from the album's shared artists list.
        // This way we dont duplicate information from the album header.
        sharedArtists = sharedArtists.Except(_albumSharedArtists).ToList();
        
        _creditBox.UpdateTracksCredits(sharedArtists);
        _creditBox.UpdateAlbumCredits([]);
    }

    private ContentProvider? TrackOnDragPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var row = (AlbumTrackRow)sender.GetWidget()!;
        var wrapper = new GId(row.TrackId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }

    private void TrackOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
    }
}