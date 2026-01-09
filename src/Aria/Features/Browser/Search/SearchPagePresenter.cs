using Aria.Core;
using Aria.Core.Library;

namespace Aria.Features.Browser.Search;

public class SearchPagePresenter(IPlaybackApi playbackApi)
{
    private SearchPage View { get; set; } = null!;
    
    public void Attach(SearchPage view)
    {
        View = view;
        view.SearchChanged += ViewOnSearchChanged;
    }

    private void ViewOnSearchChanged(object? sender, string e)
    {
        // use the library to search
        
        // give the view new search results
    }

    public void Clear()
    {
        View.Clear();
    }
}