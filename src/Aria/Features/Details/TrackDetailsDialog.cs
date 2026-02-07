using Adw;
using Aria.Core.Library;
using Aria.Features.Browser.Search;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Details;

[Subclass<PreferencesDialog>]
[Template<AssemblyResource>("Aria.Features.Details.TrackDetailsDialog.ui")]
public partial class TrackDetailsDialog
{
    [Connect("artists-group")] private PreferencesGroup _artistsPreferencesGroup;
    [Connect("albumartists-group")] private PreferencesGroup _albumArtistsPreferencesGroup;
    
    [Connect("album-date-row")] private ActionRow _albumDateRow;
    [Connect("album-name-row")] private ActionRow _albumNameRow;
    
    [Connect("track-name-row")] private ActionRow _trackNameRow;
    [Connect("track-duration-row")] private ActionRow _trackDurationRow;
    [Connect("track-volume-row")] private ActionRow _trackVolumeRow;
    [Connect("track-number-row")] private ActionRow _trackNumberRow;
    
    [Connect("work-group")] private PreferencesGroup _workPreferencesGroup;    
    [Connect("work-name-row")] private ActionRow _workNameRow;
    [Connect("work-movement-row")] private ActionRow _workMovementRow;
    [Connect("work-movement-number-row")] private ActionRow _workMovementNumberRow;
    
    [Connect("details-group")] private PreferencesGroup _detailsPreferencesGroup;    
    [Connect("file-row")] private ActionRow _fileRow;    
    
    partial void Initialize()
    {
        InitializeActions();
    }

    public void LoadTrack(AlbumTrackInfo track, AlbumInfo albumInfo)
    {
        Track = track;
        
        var dateText = albumInfo.ReleaseDate.HasValue
            ? albumInfo.ReleaseDate.Value.ToShortDateString()
            : "Unknown";
        _albumDateRow.SetSubtitle(dateText);
        _albumNameRow.SetSubtitle(albumInfo.Title);
        
        _albumNameRow.SetActionName($"details.show-album");
        _albumNameRow.SetActionTargetValue(Variant.NewString(albumInfo.Id.ToString()));
        
        _trackNameRow.SetSubtitle(track.Track.Title);
        _trackDurationRow.SetSubtitle(track.Track.Duration.ToString(@"mm\:ss"));
        _trackVolumeRow.SetSubtitle(track.VolumeName ?? "");
        _trackNumberRow.SetSubtitle(track.TrackNumber != null ? track.TrackNumber.Value.ToString() : "");
        
        _workPreferencesGroup.Visible = track.Track.Work != null;
        _workNameRow.SetSubtitle(track.Track.Work?.Work ?? "");
        _workMovementRow.SetSubtitle(track.Track.Work?.MovementName ?? "");
        _workMovementNumberRow.SetSubtitle(track.Track.Work?.MovementNumber ?? "");
        
        _artistsPreferencesGroup.Visible = track.Track.CreditsInfo.Artists.Count > 0;
        
        
        foreach (var artist in track.Track.CreditsInfo.Artists.OrderBy(a => GetRolePriority(a.Roles)).ThenBy(a => a.Artist.Name))
        {
            var artistRow = ActionRow.New();
            artistRow.Subtitle = artist.Artist.Name;
            artistRow.Activatable = true;
            artistRow.AddCssClass("property");
            
            var image = Image.NewFromIconName("go-next-symbolic");
            artistRow.AddSuffix(image);

            artistRow.Title = RolesFormatting.Format(artist.Roles);
            if (artist.AdditionalInformation != null)
            {
                artistRow.Title += $" ({artist.AdditionalInformation})";
            }
            
            artistRow.SetActionName("details.show-artist");
            artistRow.SetActionTargetValue(Variant.NewString(artist.Artist.Id.ToString()));

            _artistsPreferencesGroup.Add(artistRow);
        }
        
        var albumArtists = albumInfo.CreditsInfo.AlbumArtists
            .Where(aa => track.Track.CreditsInfo.Artists.All(a => aa.Id != a.Artist.Id)).ToList();
        _albumArtistsPreferencesGroup.Visible = albumArtists.Any();
        
        foreach (var albumArtist in albumArtists)
        {
            // Skip artists already shown above
            if (track.Track.CreditsInfo.Artists.Any(a => a.Artist.Id == albumArtist.Id)) continue;
            
            var artistRow = ActionRow.New();
            artistRow.AddCssClass("property");
            artistRow.Subtitle = albumArtist.Name;
            artistRow.Title = RolesFormatting.Format(albumArtist.Roles);
            artistRow.Activatable = true;
            artistRow.SetActionName("details.show-artist");
            artistRow.SetActionTargetValue(Variant.NewString(albumArtist.Id.ToString()));
            
            var image = Image.NewFromIconName("go-next-symbolic");
            artistRow.AddSuffix(image);            

            _albumArtistsPreferencesGroup.Add(artistRow);            
        }
        
        _detailsPreferencesGroup.Visible = track.Track.FileName != null;        
        _fileRow.SetSubtitle(track.Track.FileName ?? "");
    }
    
    public AlbumTrackInfo? Track { get; private set; }

    private void Dismiss()
    {
        Close();
    }

    private static int GetRolePriority(ArtistRoles roles)
    {
        if (roles.HasFlag(ArtistRoles.Composer)) return 0;
        if (roles.HasFlag(ArtistRoles.Conductor)) return 1;
        return roles.HasFlag(ArtistRoles.Ensemble) ? 2 : 3;
    }
}