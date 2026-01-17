using GObject;
using Gtk;

namespace Aria.Features.Browser.Search;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Search.SearchPage.ui")]
public partial class SearchPage
{
    private const string ResultsStackpageName = "results-stack-page";
    private const string NoResultsStackPageName = "no-results-stack-page";
    
    [Connect("album-list-box")] private ListBox _albumListBox;

    [Connect("artist-list-box")] private ListBox _artistListBox;
    [Connect(NoResultsStackPageName)] private StackPage _noResultsStackPage;
    [Connect(ResultsStackpageName)] private StackPage _resultsStackPage;
    [Connect("search-entry")] private SearchEntry _searchEntry;

    [Connect("search-stack")] private Stack _searchStack;
    [Connect("song-list-box")] private ListBox _songListBox;
    [Connect("work-list-box")] private ListBox _workListBox;

    public event EventHandler<string>? SearchChanged;
    public event EventHandler? SearchStopped;

    partial void Initialize()
    {
        _searchEntry.OnSearchChanged += SearchEntryOnOnSearchChanged;
        _searchEntry.OnStopSearch += SearchEntryOnOnStopSearch;
    }
    
    public void Clear()
    {
        _searchStack.VisibleChildName = NoResultsStackPageName;
        _searchEntry.SetText("");
        _searchEntry.GrabFocus();

        _artistListBox.RemoveAll();
        _albumListBox.RemoveAll();
        _workListBox.RemoveAll();
        _songListBox.RemoveAll();

        // TODO; use the adjustement to scroll back to 0,0
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