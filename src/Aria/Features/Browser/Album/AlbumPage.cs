using Adw;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Gdk;
using Gio;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Album;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Album.AlbumPage.ui")]
public partial class AlbumPage
{
    private AlbumInfo _album;

    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("play-button")] private Button _playButton;
    [Connect("tracks-box")] private Box _tracksBox;
    
    [Connect("albumartists-box")] private WrapBox _albumArtistsBox;
    [Connect("title-label")] private Label _titleLabel;
    [Connect("partial-banner")] private Banner _partialBanner;

    public SimpleAction PlayAlbumAction { get; private set; }
    public SimpleAction EnqueueAlbumAction { get; private set; }
    public SimpleAction ShowFullAlbumAction { get; private set; }
    public SimpleAction EnqueueTrack { get; private set; }
    
    private IReadOnlyList<AlbumTrackInfo> _filteredTracks;
    private readonly List<TrackGroup> _trackGroups = [];

    partial void Initialize()
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(PlayAlbumAction = SimpleAction.New("play", null));
        actionGroup.AddAction(EnqueueAlbumAction = SimpleAction.New("enqueue", null));
        actionGroup.AddAction(ShowFullAlbumAction = SimpleAction.New("full", null));
        actionGroup.AddAction(EnqueueTrack = SimpleAction.New("enqueue-track-default", VariantType.String));
        InsertActionGroup("album", actionGroup);
    }

    public void LoadAlbum(AlbumInfo album, ArtistInfo? filteredArtist = null)
    {
        if (filteredArtist != null)
        {
            _filteredTracks = album.Tracks
                .Where(t => t.Track.CreditsInfo.Artists.Any(a => a.Artist.Id == filteredArtist.Id)).ToList();
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
        UpdateTracks();
    }

    private void UpdateTracks()
    {
        foreach (var group in _trackGroups)
        {
            // TODO: invoke their destructor
            _tracksBox.Remove(group);            
        }

        _trackGroups.Clear();

        if (_filteredTracks.Count == 0)
            return;

        List<AlbumTrackInfo> currentGroupTracks = [];
        string? currentGroupKey = null;

        foreach (var track in _filteredTracks)
        {
            var trackGroupKey = track.Group?.Key;

            // If the group key changes, create a TrackGroup for the previous group
            if (currentGroupKey != trackGroupKey && currentGroupTracks.Count > 0)
            {
                var headerText = currentGroupKey;
                var trackGroup = new TrackGroup();
                trackGroup.LoadTracks(currentGroupTracks, headerText, _album);
                _tracksBox.Append(trackGroup);
                currentGroupTracks = [];
                _trackGroups.Add(trackGroup);
            }

            currentGroupKey = trackGroupKey;
            currentGroupTracks.Add(track);
        }

        // Add the final group
        if (currentGroupTracks.Count > 0)
        {
            var headerText = currentGroupKey;
            var trackGroup = new TrackGroup();
            trackGroup.LoadTracks(currentGroupTracks, headerText, _album);
            _tracksBox.Append(trackGroup);
            _trackGroups.Add(trackGroup);
        }
    }

    public void SetCover(Texture texture)
    {
        _coverPicture.SetPaintable(texture);
    }

    private void UpdateHeader()
    {
        // if (_album.ReleaseDate.HasValue)
        // {
        //     var date = _album.ReleaseDate.Value;
        //     // Check if it's the first day of the year (01-01)
        //     var yearLine = date is { Month: 1, Day: 1 }
        //         ? $"{date.Year}"
        //         : $"{date:d}";
        //     _subtitleLabel.SetLabel(yearLine);
        // }

        // var duration = TimeSpan.FromTicks(_album.Tracks.Sum(t => t.Track.Duration.Ticks));
        // var durationText = duration.TotalHours >= 1
        //     ? duration.ToString(@"h\:mm\:ss")
        //     : duration.ToString(@"mm\:ss");
        //
        // _durationLabel.SetLabel(durationText);

        _albumArtistsBox.RemoveAll();
        
        var label = Label.New("By ");
        label.AddCssClass("dimmed");
        _albumArtistsBox.Append(label);
        
        foreach (var artist in _album.CreditsInfo.AlbumArtists)
        {
            // TODO: Comma separator
            
            // Format the button
            var button = Button.NewWithLabel(artist.Name);
            button.AddCssClass("link");
            button.AddCssClass("artist-link");

            // Configure the action
            button.SetActionName("browser.show-artist");
            var value = Variant.NewString(artist.Id?.ToString() ?? Id.Unknown.ToString());
            button.SetActionTargetValue(value);

            _albumArtistsBox.Append(button);
        }


        _titleLabel.SetLabel(_album.Title);
    }
}