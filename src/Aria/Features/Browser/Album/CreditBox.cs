using Adw;
using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Album;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Album.CreditBox.ui")]
public partial class CreditBox
{
    [Connect("main-box")] private WrapBox _mainBox;
    [Connect("composers-box")] private WrapBox _composersBox;
    [Connect("arranged-box")] private WrapBox _arrangedBox;
    [Connect("conductors-box")] private WrapBox _conductorsBox;
    [Connect("performers-box")] private WrapBox _performersBox;
    [Connect("solists-box")] private WrapBox _solistsBox;
    
    [Connect("main-label")] private Label _mainLabel;    
    [Connect("composers-label")] private Label _composersLabel;
    [Connect("arranged-label")] private Label _arrangedLabel;
    [Connect("conductors-label")] private Label _conductorsLabel;
    [Connect("performers-label")] private Label _performersLabel;
    [Connect("solists-label")] private Label _solistsLabel;

    public void UpdateAlbumCredits(IList<ArtistInfo> artists)
    {
        // Wrap them in Track artists with the 'main' flag
        var trackArtists = artists.Select(a => new TrackArtistInfo
        {
            Artist = a,
            Roles = ArtistRoles.Main
        }).ToList();
        
        FillArtistBox(_mainBox, _mainLabel, trackArtists, ArtistRoles.Main);        
    }
    
    public void UpdateTracksCredits(IList<TrackArtistInfo> artists)
    {
        // Do not remove Album Artists. In some tagging schemes, all artists are listed there.
        // Therefore, this is not a reliable source to determine whether they have already been shown.
        // A better approach would be to check whether the artists are shared across all tracks on the album.
        FillArtistBox(_composersBox, _composersLabel, artists, ArtistRoles.Composer);
        FillArtistBox(_conductorsBox, _conductorsLabel,  artists, ArtistRoles.Conductor);
        FillArtistBox(_arrangedBox, _arrangedLabel, artists, ArtistRoles.Arranger);
        FillArtistBox(_solistsBox, _solistsLabel, artists, ArtistRoles.Soloist);        
        FillArtistBox(_performersBox, _performersLabel, artists, ArtistRoles.Ensemble | ArtistRoles.Performer);
    }
    
    private void FillArtistBox(WrapBox box, Label label, IList<TrackArtistInfo> artists, ArtistRoles roleFilter)
    {
        box.RemoveAll();
        var filteredArtists = artists
            .OrderBy(a => a.Artist.Name)
            .ThenBy(a => (a.Roles & ArtistRoles.Composer) == 0) // In combined lists, prio composer over others
            .ThenBy(a => (a.Roles & ArtistRoles.Ensemble) == 0) // And then ensembles
            .Where(a => (a.Roles & roleFilter) != 0).ToList();
        
        label.Visible = filteredArtists.Count > 0;
        box.Visible = filteredArtists.Count > 0;

        if (filteredArtists.Count <= 0) return;
        
        for (var i = 0; i < filteredArtists.Count; i++)
        {
            var artist = filteredArtists[i];
            var isLastArtist = i == filteredArtists.Count - 1;
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
    
    private Button CreateArtistButton(ArtistInfo artist)
    {
        // Format the button
        var displayText = artist.Name;
        var button = Button.NewWithLabel(displayText);
        button.AddCssClass("link");
        button.AddCssClass("artist-link");

        // Configure the action
        button.SetActionName($"{AppActions.Browser.Key}.{AppActions.Browser.ShowArtist.Action}");
        var value = Variant.NewString(artist.Id?.ToString() ?? Id.Undetermined.ToString());
        button.SetActionTargetValue(value);

        return button;
    }    
}