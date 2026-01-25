using Adw;
using Aria.Core.Library;
using Gio;
using GLib;
using GObject;
using Gtk;
using Humanizer;

namespace Aria.Features.Browser.Search;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Search.SearchPage.ui")]
public partial class SearchPage
{
    private const string ResultsStackpageName = "results-stack-page";
    private const string NoResultsStackPageName = "no-results-stack-page";
    
    [Connect(NoResultsStackPageName)] private StackPage _noResultsStackPage;
    [Connect(ResultsStackpageName)] private StackPage _resultsStackPage;
    [Connect("search-entry")] private SearchEntry _searchEntry;
    [Connect("search-stack")] private Stack _searchStack;
    
    [Connect("album-box")] private Box _albumBox;
    [Connect("album-list-box")] private ListBox _albumListBox;
    
    [Connect("artist-box")] private Box _artistBox;    
    [Connect("artist-list-box")] private ListBox _artistListBox;
    
    [Connect("track-box")] private Box _trackBox;
    [Connect("track-list-box")] private ListBox _trackListBox;
    
    [Connect("work-box")] private Box _workBox;
    [Connect("work-list-box")] private ListBox _workListBox;    
    
    public event EventHandler<string>? SearchChanged;
    public event EventHandler? SearchStopped;
    
    public SimpleAction ShowAlbumAction { get; private set; }
    public SimpleAction ShowArtistAction { get; private set; }
    public SimpleAction EnqueueTrackAction { get; private set; }

    partial void Initialize()
    {
        _searchEntry.OnSearchChanged += SearchEntryOnOnSearchChanged;
        _searchEntry.OnStopSearch += SearchEntryOnOnStopSearch;
        
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(ShowAlbumAction = SimpleAction.New("show-album", VariantType.String));
        actionGroup.AddAction(ShowArtistAction = SimpleAction.New("show-artist", VariantType.String));
        actionGroup.AddAction(EnqueueTrackAction = SimpleAction.New("enqueue-track", VariantType.String));
        InsertActionGroup("results", actionGroup);
    }
    
    public void Clear()
    {
        _searchStack.VisibleChildName = NoResultsStackPageName;
        _searchEntry.SetText("");
        _searchEntry.GrabFocus();

        _artistListBox.RemoveAll();
        _albumListBox.RemoveAll();
        _workListBox.RemoveAll();
        _trackListBox.RemoveAll();

        // TODO; use the adjustement to scroll back to 0,0
    }
    
    public void ShowResults(SearchResults results)
    {
        _artistListBox.RemoveAll();
        _albumListBox.RemoveAll();
        _workListBox.RemoveAll();
        _trackListBox.RemoveAll();
        
        var totalCount = results.Artists.Count + results.Albums.Count + results.Tracks.Count;
        _searchStack.VisibleChildName = totalCount == 0 ? NoResultsStackPageName : ResultsStackpageName;
        
        _artistBox.Visible = results.Artists.Count > 0;
        _albumBox.Visible = results.Albums.Count > 0;
        _trackBox.Visible = results.Tracks.Count > 0;
        _workBox.Visible = false;
        
        foreach (var artist in results.Artists)
        {
            var row = ActionRow.New();
            row.Activatable = true;
            row.UseMarkup = false;
            row.Title = artist.Name;
            
            row.ActionName = "results.show-artist";
            row.SetActionTargetValue(Variant.NewString(artist.Id?.ToString() ?? string.Empty));

            var roles = new List<string>();
            if (artist.Roles.HasFlag(ArtistRoles.Composer)) roles.Add("Composer");
            if (artist.Roles.HasFlag(ArtistRoles.Arranger)) roles.Add("Arranger");
            if (artist.Roles.HasFlag(ArtistRoles.Conductor)) roles.Add("Conductor");
            if (artist.Roles.HasFlag(ArtistRoles.Ensemble)) roles.Add("Ensemble");
            if (artist.Roles.HasFlag(ArtistRoles.Performer)) roles.Add("Performer");
            if (artist.Roles.HasFlag(ArtistRoles.Soloist)) roles.Add("Soloist");
            row.Subtitle = roles.Humanize();
            _artistListBox.Append(row);
        }

        foreach (var album in results.Albums)
        {
            var row = ActionRow.New();
            row.Activatable = true;
            row.UseMarkup = false;
            row.Title = album.Title;
            row.Subtitle = album.CreditsInfo.AlbumArtists.Select(a => a.Name).Humanize();
            
            row.ActionName = "results.show-album";
            
            var albumIdString = album.Id!.ToString();
            
            row.SetActionTargetValue(Variant.NewString(albumIdString));
            
            _albumListBox.Append(row);
        }

        foreach (var track in results.Tracks)
        {
            var row = ActionRow.New();
            row.Activatable = true;
            row.UseMarkup = false;
            row.Title = track.Title;
            row.Subtitle = track.CreditsInfo.AlbumArtists.Select(a => a.Name).Humanize();
            
            row.ActionName = "results.enqueue-track";
            row.SetActionTargetValue(Variant.NewString(track.Id?.ToString() ?? string.Empty));            
            
            _trackListBox.Append(row);
        }
    }    
    
    private void SearchEntryOnOnStopSearch(SearchEntry sender, EventArgs args)
    {
        SearchStopped?.Invoke(this, EventArgs.Empty);
    }

    private void SearchEntryOnOnSearchChanged(SearchEntry sender, EventArgs args)
    {
        SearchChanged?.Invoke(this, _searchEntry.GetText());
    }
}