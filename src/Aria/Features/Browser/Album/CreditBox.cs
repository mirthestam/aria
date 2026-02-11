using Adw;
using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Album;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Album.CreditBox.ui")]
public partial class CreditBox
{
    [Connect("main-box")] private Box _mainBox;
    [Connect("composers-box")] private Box _composersBox;
    [Connect("arranged-box")] private Box _arrangedBox;
    [Connect("conductors-box")] private Box _conductorsBox;
    [Connect("performers-box")] private Box _performersBox;
    [Connect("solists-box")] private Box _solistsBox;

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
        FillArtistBox(_conductorsBox, _conductorsLabel, artists, ArtistRoles.Conductor);
        FillArtistBox(_arrangedBox, _arrangedLabel, artists, ArtistRoles.Arranger);
        FillArtistBox(_solistsBox, _solistsLabel, artists, ArtistRoles.Soloist);
        FillArtistBox(_performersBox, _performersLabel, artists, ArtistRoles.Ensemble | ArtistRoles.Performer);
    }

    private void FillArtistBox(Box container, Label label, IList<TrackArtistInfo> artists, ArtistRoles roleFilter)
    {
        while (container.GetFirstChild() != null)
        {
            container.Remove(container.GetFirstChild()!);
        }

        var filteredArtists = artists
            .OrderBy(a => a.Artist.Name)
            .ThenBy(a => (a.Roles & ArtistRoles.Composer) == 0) // In combined lists, prio composer over others
            .ThenBy(a => (a.Roles & ArtistRoles.Ensemble) == 0) // And then ensembles
            .Where(a => (a.Roles & roleFilter) != 0).ToList();

        label.Visible = filteredArtists.Count > 0;
        container.Visible = filteredArtists.Count > 0;

        const int moreThreshold = 3;
        const int moreMinimum = 3;

        var added = 0;
        foreach (var artist in filteredArtists)
        {
            added++;

            if (added == moreThreshold + 1 && filteredArtists.Count - moreThreshold > moreMinimum)
            {
                var parentContainer = container;
                var expander = Expander.New("More (" + (filteredArtists.Count - moreThreshold) + ")");
                container = New(Orientation.Vertical, 2);
                expander.SetChild(container);
                parentContainer.Append(expander);
            }

            container.Append(CreateArtistBox(artist));
        }
    }

    private static Box CreateArtistBox(TrackArtistInfo artist)
    {
        var box = New(Orientation.Horizontal, 2);
        box.Append(CreateArtistButton(artist.Artist));
        if (artist.AdditionalInformation == null) return box;

        var additionalInfoLabel = Label.New($"({artist.AdditionalInformation})");
        additionalInfoLabel.AddCssClass("dimmed");
        box.Append(additionalInfoLabel);

        return box;
    }

    private static Button CreateArtistButton(ArtistInfo artist)
    {
        // Format the button
        var displayText = artist.Name;
        var button = Button.NewWithLabel(displayText);
        //button.AddCssClass("link");
        button.AddCssClass("flat");
        button.AddCssClass("artist-link");
        button.AddCssClass("accent");

        // Configure the action
        button.SetActionName($"{AppActions.Browser.Key}.{AppActions.Browser.ShowArtist.Action}");
        var value = Variant.NewString(artist.Id.ToString());
        button.SetActionTargetValue(value);

        return button;
    }
}