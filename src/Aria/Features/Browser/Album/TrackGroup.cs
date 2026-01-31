using Adw;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
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
    [Connect("composers-box")] private WrapBox _composersBox;
    [Connect("arranged-box")] private WrapBox _arrangedBox;
    [Connect("conductors-box")] private WrapBox _conductorsBox;
    [Connect("performers-box")] private WrapBox _performersBox;
    
    [Connect("composers-label")] private Label _composersLabel;
    [Connect("arranged-label")] private Label _arrangedLabel;
    [Connect("conductors-label")] private Label _conductorsLabel;
    [Connect("performers-label")] private Label _performersLabel;    

    private readonly List<DragSource> _trackDragSources = [];
    private List<AlbumTrackInfo> _tracks = [];
    private AlbumInfo _album;

    public string? Header
    {
        get => _headerLabel.Label_;
        set
        {
            _headerLabel.Label_ = value;
            _headerLabel.Visible = !string.IsNullOrWhiteSpace(value);
        }
    }

    public void LoadTracks(List<AlbumTrackInfo> tracks, string? headerText, AlbumInfo album)
    {
        _tracks = tracks;
        _album = album;
        Header = headerText;

        if (tracks.Count == 1)
        {
            // Just one track. Header does not make sense
            Header = null;
        }

        UpdateHeader();
        UpdateTracksList();
    }

    private void RemoveTracks()
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
        
        FillArtistBox(_composersBox, _composersLabel, sharedArtists, ArtistRoles.Composer);
        FillArtistBox(_conductorsBox, _conductorsLabel,  sharedArtists, ArtistRoles.Conductor);
        FillArtistBox(_arrangedBox, _arrangedLabel, sharedArtists, ArtistRoles.Arranger);
        FillArtistBox(_performersBox, _performersLabel, sharedArtists, ArtistRoles.Performer);
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

    private void FillArtistBox(WrapBox box, Label label, List<TrackArtistInfo> sharedArtists, ArtistRoles role)
    {
        box.RemoveAll();
        var artists = sharedArtists.Where(a => a.Roles.HasFlag(role)).ToList();
        
        label.Visible = artists.Count > 0;
        box.Visible = artists.Count > 0;
        
        if (artists.Count > 0)
        {
            for (var i = 0; i < artists.Count; i++)
            {
                var artist = artists[i];
                var isLastArtist = i == artists.Count - 1;
                var nameButton = CreateArtistButton(artist.Artist);
                box.Append(nameButton);

                if (artist.AdditionalInformation != null)
                {
                    var additionalInfoLabel = Label.New($"({artist.AdditionalInformation})");
                    additionalInfoLabel.AddCssClass("dimmed");
                    box.Append(additionalInfoLabel);
                }

                if (!isLastArtist) box.Append(Label.New(","));
            }
        }

        Button CreateArtistButton(ArtistInfo artist)
        {
            // Format the button
            var displayText = artist.Name;
            var button = Button.NewWithLabel(displayText);
            button.AddCssClass("link");
            button.AddCssClass("artist-link");

            // Configure the action
            button.SetActionName("browser.show-artist");
            var value = Variant.NewString(artist.Id?.ToString() ?? Id.Unknown.ToString());
            button.SetActionTargetValue(value);

            return button;
        }

        Label CreatePrefixLabel(string text)
        {
            var label = Label.New(text);
            label.AddCssClass("dimmed");
            return label;
        }
    }
}