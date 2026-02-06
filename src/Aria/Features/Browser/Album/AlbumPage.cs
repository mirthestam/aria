using Adw;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Album;

[Subclass<NavigationPage>]
[Template<AssemblyResource>("Aria.Features.Browser.Album.AlbumPage.ui")]
public partial class AlbumPage
{
    private AlbumInfo _album;

    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("tracks-box")] private Box _tracksBox;
    
    [Connect("credit-box")] private CreditBox _creditBox;
    
    [Connect("title-label")] private Label _titleLabel;

    [Connect("message-listbox")] private ListBox _messageListBox;
    [Connect("filter-message-row")]private ActionRow _filterMessageRow;

    [Connect("enqueue-split-button")] private SplitButton _enqueueSplitButton;
    
    private IReadOnlyList<AlbumTrackInfo> _filteredTracks;
    private readonly List<TrackGroup> _trackGroups = [];
    private List<TrackArtistInfo> _sharedArtists = [];

    partial void Initialize()
    {
        InitializeActions();
    }
    
    public void LoadAlbum(AlbumInfo album, ArtistInfo? filteredArtist = null)
    {
        if (filteredArtist != null)
        {
            _filteredTracks = album.Tracks
                .Where(t => t.Track.CreditsInfo.Artists.Any(a => a.Artist.Id == filteredArtist.Id)).ToList();
            if (_filteredTracks.Count != album.Tracks.Count)
            {
                _filterMessageRow.Title =$"Tracks featuring {filteredArtist.Name}";
                _messageListBox.Visible = true; 
            }
        }
        else
        {
            _filteredTracks = album.Tracks;
            _messageListBox.Visible = false;
        }

        _album = album;

        SetTitle(album.Title);
        
        // Always update header first. It needs updated shared artists from the header.
        UpdateHeader();
        UpdateTracks();
    }

    private void UpdateTracks()
    {
        foreach (var group in _trackGroups)
        {
            group.RemoveTracks();
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
                currentGroupTracks = CreateTrackGroup(headerText, _sharedArtists);
            }

            currentGroupKey = trackGroupKey;
            currentGroupTracks.Add(track);
        }

        // Add the final group
        if (currentGroupTracks.Count > 0)
        {
            _ = CreateTrackGroup(currentGroupKey, _sharedArtists);
        }

        if (_trackGroups.Count != 1) return;
        
        // Decide what to do with a single group.
        
        var mainGroup = _trackGroups[0];
        if (string.IsNullOrWhiteSpace(mainGroup.Header))
        {
            // There is just one group. Also, it has no name.
            // We don't need the header here as we can use the global header.
            _trackGroups[0].HeaderVisible = false;                
        }

        return;

        List<AlbumTrackInfo> CreateTrackGroup(string? headerText, IReadOnlyList<TrackArtistInfo> sharedArtists)
        {
            var trackGroup = TrackGroup.NewWithProperties([]);
            trackGroup.LoadTracks(currentGroupTracks, headerText, sharedArtists);
            _tracksBox.Append(trackGroup);
            currentGroupTracks = [];
            _trackGroups.Add(trackGroup);
            return currentGroupTracks;
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
        
        var artists = _album.CreditsInfo.AlbumArtists.ToList();
        _creditBox.UpdateAlbumCredits(artists);
        
        _sharedArtists = SharedArtistHelper.GetSharedArtists(_album.Tracks).ToList();
        
        _creditBox.UpdateTracksCredits(_sharedArtists);
        
        _titleLabel.SetLabel(_album.Title);
    }
}