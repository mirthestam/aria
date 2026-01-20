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

    [Connect("albumartists-label")] private Label _albumArtistsLabel;
    [Connect("artists-label")] private Label _commonArtistsLabel;
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
        var albumArtistsLine = string.Join(", ", _album.CreditsInfo.AlbumArtists.Select(a => a.Name));
        var commonArtistsLine = string.Join(", ", _album.GetSharedGuestArtists().Select(ca => ca.Artist.Name));

        _albumArtistsLabel.SetLabel(albumArtistsLine);
        _commonArtistsLabel.SetLabel(commonArtistsLine);
        _commonArtistsLabel.SetVisible(!string.IsNullOrEmpty(commonArtistsLine));

        // if (releaseDate.HasValue)
        // {
        //     var date = releaseDate.Value;
        //     // Check if it's the first day of the year (01-01)
        //     yearLine = date is { Month: 1, Day: 1 }
        //         ? $"{date.Year}"
        //         : $"{date:d}";
        // }

        _albumArtistsLabel.SetLabel(albumArtistsLine);
        _commonArtistsLabel.SetLabel(commonArtistsLine);
        _commonArtistsLabel.SetVisible(!string.IsNullOrEmpty(commonArtistsLine));

        _titleLabel.SetLabel(_album.Title);
        // _subtitleLabel.SetLabel(yearLine);
        // _durationLabel.SetLabel("Not implemented");
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

            var guestArtists = track.CreditsInfo.Artists.Except(_album.GetSharedGuestArtists());
            var subTitleLine = string.Join(", ", guestArtists.Select(a => a.Artist.Name));

            row.SetSubtitle(subTitleLine);

            _tracksListBox.Append(row);
        }
    }
}