using Adw;
using Aria.Core.Library;
using Gdk;
using GObject;
using Gtk;
using Humanizer;

namespace Aria.Features.Browser.Album;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Album.AlbumPage.ui")]
public partial class AlbumPage
{
    private AlbumInfo _album;
    
    [Connect("suptitle-label")] private Label _suptitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    [Connect("subtitle-label")] private Label _subtitleLabel;
    [Connect("duration-label")] private Label _durationLabel;
    
    [Connect("songs-listbox")] private ListBox _songsListBox;
    
    [Connect("cover-picture")] private Picture _coverPicture;    

    public void SetCover(Texture texture)
    {
        _coverPicture.SetPaintable(texture);
    }
    
    private void UpdateHeader()
    {
        var albumArtistsLine = string.Join(", ", _album.CreditsInfo.AlbumArtists.Select(a => a.Name));
        
        // Year
        var releaseDate = _album.ReleaseDate;
        var yearLine = "";

        if (releaseDate.HasValue)
        {
            var date = releaseDate.Value;
            // Check if it's the first day of the year (01-01)
            yearLine = date is { Month: 1, Day: 1 } 
                ? $"{date.Year}" 
                : $"{date:d}";
        }
        
        _suptitleLabel.SetLabel(albumArtistsLine);
        _titleLabel.SetLabel(_album.Title);
        _subtitleLabel.SetLabel(yearLine);
        _durationLabel.SetLabel("Not implemented");
    }

    private void UpdateSongsList()
    {
        if (_album.Songs.Count == 0)
        {
            _songsListBox.SetVisible(false);
            return;
        }
        
        var albumArtistIds = _album.CreditsInfo.AlbumArtists
            .Select(a => a.Id)
            .ToHashSet();
        
        foreach (var song in _album.Songs)
        {
            // If an album is by "AlbumArtist A", we don't want to repeat "Artist A" next to every song.
            // We only want to show guest artists or different collaborators.
            var guestArtists = song.CreditsInfo.Artists
                .Where(artist => !albumArtistIds.Contains(artist.Artist.Id))
                .ToList();
            
            var subTitleLine = string.Join(", ", guestArtists.Select(a => a.Artist.Name));
            
            var row = ActionRow.New();
            row.SetUseMarkup(false);
            row.SetTitle(song.Title);
            row.SetSubtitle(subTitleLine);
            
            _songsListBox.Append(row);
        }
    }

    public void LoadAlbum(AlbumInfo album)
    {
        _album = album;
        SetTitle(album.Title);
        UpdateHeader();
        UpdateSongsList();
    }
}