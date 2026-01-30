using Adw;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using Gio;
using GLib;
using GObject;
using Gtk;
using TimeSpan = System.TimeSpan;

namespace Aria.Features.Browser.Album;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Album.AlbumPage.ui")]
public partial class AlbumPage
{
    private AlbumInfo _album;

    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("duration-label")] private Label _durationLabel;
    [Connect("play-button")] private Button _playButton;
    [Connect("tracks-listbox")] private ListBox _tracksListBox;
    [Connect("subtitle-label")] private Label _subtitleLabel;

    [Connect("albumartists-box")] private WrapBox _albumArtistsBox;
    [Connect("composers-box")] private WrapBox _composersBox;
    [Connect("arranged-box")] private WrapBox _arrangedBox;
    [Connect("conductors-box")] private WrapBox _conductorsBox;
    [Connect("performers-box")] private WrapBox _performersBox;
    [Connect("title-label")] private Label _titleLabel;
    [Connect("partial-banner")] private Banner _partialBanner;

    public SimpleAction PlayAlbumAction { get; private set; }
    public SimpleAction EnqueueAlbumAction { get; private set; }
    public SimpleAction ShowFullAlbumAction { get; private set; }
    public SimpleAction EnqueueTrack { get; private set; }

    private ArtistInfo? _filteredArtist;
    private IReadOnlyList<AlbumTrackInfo> _filteredTracks;
    
    private readonly List<DragSource> _trackDragSources = [];
    
    partial void Initialize()
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(PlayAlbumAction = SimpleAction.New("play", null));
        actionGroup.AddAction(EnqueueAlbumAction = SimpleAction.New("enqueue", null));
        actionGroup.AddAction(ShowFullAlbumAction = SimpleAction.New("full", null));
        actionGroup.AddAction(EnqueueTrack = SimpleAction.New("enqueue-track-default", VariantType.String));
        InsertActionGroup("album", actionGroup);
        
        OnUnmap += OnUnmapHandler;
    }
    
    public void LoadAlbum(AlbumInfo album, ArtistInfo? filteredArtist = null)
    {
        _filteredArtist = filteredArtist;
        if (filteredArtist != null)
        {
            _filteredTracks = album.Tracks.Where(t => t.Track.CreditsInfo.Artists.Any(a => a.Artist.Id == filteredArtist.Id)).ToList();
            if (_filteredTracks.Count != album.Tracks.Count)
            {
                _partialBanner.Title = $"Tracks featuring {filteredArtist.Name}";
                _partialBanner.Visible = true;                
            }
        }
        else
        {
            _filteredTracks = album.Tracks;            
            _partialBanner.Visible = false;
        }
        _album = album;
        
        SetTitle(album.Title);
        UpdateHeader();
        UpdateTracksList();
    }

    public void SetCover(Texture texture)
    {
        _coverPicture.SetPaintable(texture);
    }

    private void UpdateHeader()
    {
        if (_album.ReleaseDate.HasValue)
        {
            var date = _album.ReleaseDate.Value;
            // Check if it's the first day of the year (01-01)
            var yearLine = date is { Month: 1, Day: 1 }
                ? $"{date.Year}"
                : $"{date:d}";
            _subtitleLabel.SetLabel(yearLine);            
        }

        var duration = TimeSpan.FromTicks(_album.Tracks.Sum(t => t.Track.Duration.Ticks));
        var durationText = duration.TotalHours >= 1
            ? duration.ToString(@"h\:mm\:ss")
            : duration.ToString(@"mm\:ss");

        _durationLabel.SetLabel(durationText);

        _albumArtistsBox.RemoveAll();
        foreach (var artist in _album.CreditsInfo.AlbumArtists)
        {
            // Format the button
            var button = Button.NewWithLabel(artist.Name);
            button.AddCssClass("flat");
            
            // Configure the action
            button.SetActionName("browser.show-artist");
            var value = GLib.Variant.NewString(artist.Id?.ToString() ?? Id.Unknown.ToString()); 
            button.SetActionTargetValue(value);
            
            _albumArtistsBox.Append(button);
        }
        
        var sharedArtists = _album.GetSharedArtists().ToList();
        
        // If the sharedArtists list does not contain the filtered artist; add them.
        // This happens when we show a "partial" album view for an artist that only appears on some tracks.
        if (_filteredArtist is not null &&
            sharedArtists.All(a => a.Artist.Id != _filteredArtist.Id))
        {
            // If the filtered artist has the same role(s) on all visible tracks, carry those over (per role flag).
            // We compute the intersection of roles across the tracks where the artist appears.
            
            // TODO: this approach is nice, but as a result of filtering, this can now also apply for all other
            // shown artists.
            var rolesIntersection = _filteredTracks.Select(albumTrack => albumTrack.Track.CreditsInfo.Artists.Where(a => a.Artist.Id == _filteredArtist.Id)
                    .Aggregate(ArtistRoles.None, (acc, a) => acc | a.Roles))
                .Where(rolesOnThisTrack => rolesOnThisTrack != ArtistRoles.None)
                .Aggregate<ArtistRoles, ArtistRoles?>(null, (current, rolesOnThisTrack) => current is null
                    ? rolesOnThisTrack
                    : current.Value & rolesOnThisTrack);

            sharedArtists.Add(new TrackArtistInfo
            {
                Artist = _filteredArtist,
                Roles = rolesIntersection ?? ArtistRoles.None
            });
        }
        
        _composersBox.RemoveAll();
        var composers = sharedArtists.Where(a => a.Roles.HasFlag(ArtistRoles.Composer)).ToList();
        if (composers.Count > 0)
        {
            _composersBox.Append(CreatePrefixLabel("Composed by"));

            for (var i = 0; i < composers.Count; i++)
            {
                var composer = composers[i];
                var isLast = i == composers.Count - 1;
                var artistLabel = CreateArtistButton(composer.Artist, isLast);
                _composersBox.Append(artistLabel);
            }
        }

        _conductorsBox.RemoveAll();        
        var conductors = sharedArtists.Where(a => a.Roles.HasFlag(ArtistRoles.Conductor)).ToList();
        if (conductors.Count > 0)
        {
            _conductorsBox.Append(CreatePrefixLabel("Conducted by"));

            for (var i = 0; i < conductors.Count; i++)
            {
                var conductor = conductors[i];
                var isLast = i == conductors.Count - 1;
                var artistLabel = CreateArtistButton(conductor.Artist, isLast);
                _conductorsBox.Append(artistLabel);
            }
        }
        
        _arrangedBox.RemoveAll();
        var arrangers = sharedArtists.Where(a => a.Roles.HasFlag(ArtistRoles.Arranger)).ToList();
        if (arrangers.Count > 0)
        {
            _arrangedBox.Append(CreatePrefixLabel("Arranged by"));
            foreach (var artistLabel in arrangers.Select(arranger => Label.New(arranger.Artist.Name)))
            {
                _arrangedBox.Append(artistLabel);
            }
        }

        _performersBox.RemoveAll();
        var performers = sharedArtists
            .Where(a => !a.Roles.HasFlag(ArtistRoles.Conductor) && !a.Roles.HasFlag(ArtistRoles.Arranger) &&
                        !a.Roles.HasFlag(ArtistRoles.Composer))
            .ToList();
        if (performers.Count > 0)
        {
            _performersBox.Append(CreatePrefixLabel("Performed by"));

            for (var i = 0; i < performers.Count; i++)
            {
                var performer = performers[i];
                var isLast = i == performers.Count - 1;

                var artistLabel = CreateArtistButton(performer.Artist, isLast);                
                artistLabel.TooltipText = performer.Roles switch
                {
                    ArtistRoles.Ensemble => "Ensemble",
                    ArtistRoles.Soloist => "Soloist",
                    _ => "Unknown role"
                };
                _performersBox.Append(artistLabel);
            }
        }
        
        _titleLabel.SetLabel(_album.Title);
        return;

        Button CreateArtistButton(ArtistInfo artist, bool isLast)
        {
            // Format the button
            var displayText = isLast ? artist.Name : $"{artist.Name},";
            var button = Button.NewWithLabel(displayText);
            button.AddCssClass("link");
            button.AddCssClass("artist-link");
            
            // Configure the action
            button.SetActionName("browser.show-artist");
            var value = GLib.Variant.NewString(artist.Id?.ToString() ?? Id.Unknown.ToString()); 
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
        
        if (_filteredTracks.Count == 0)
        {
            _tracksListBox.SetVisible(false);
            return;
        }

        foreach (var albumTrack in _filteredTracks)
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

            var guestArtists = _album.GetUniqueSongArtists(track);
            
            if (_filteredArtist != null)
            {
                guestArtists = guestArtists.Where(a => a.Artist.Id != _filteredArtist.Id).ToList();
            }
            
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
    
    private void OnUnmapHandler(Widget sender, EventArgs args)
    {
        // Remove the tracks. 
        // This unbinds the drag handlers, effectively 'releasing' them.
        RemoveTracks();
    }
}