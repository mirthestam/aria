using Adw;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using Gio;
using GObject;
using Gtk;

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

    public SimpleAction PlayAlbumAction { get; private set; }
    public SimpleAction EnqueueAlbumAction { get; private set; }

    partial void Initialize()
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(PlayAlbumAction = SimpleAction.New("play", null));
        actionGroup.AddAction(EnqueueAlbumAction = SimpleAction.New("enqueue", null));
        InsertActionGroup("album", actionGroup);
    }

    public void LoadAlbum(AlbumInfo album)
    {
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
        
        var arrangers = sharedArtists.Where(a => a.Roles.HasFlag(ArtistRoles.Arranger)).ToList();
        if (arrangers.Count > 0)
        {
            _arrangedBox.Append(CreatePrefixLabel("Arranged by"));
            foreach (var artistLabel in arrangers.Select(arranger => Label.New(arranger.Artist.Name)))
            {
                _arrangedBox.Append(artistLabel);
            }
        }

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

    private void UpdateTracksList()
    {
        if (_album.Tracks.Count == 0)
        {
            _tracksListBox.SetVisible(false);
            return;
        }

        foreach (var albumTrack in _album.Tracks)
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

            var row = ActionRow.New();

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
            var subTitleLine = string.Join(", ", guestArtists.Select(a => a.Artist.Name));

            row.SetSubtitle(subTitleLine);

            _tracksListBox.Append(row);
        }
    }
}