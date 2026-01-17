using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser.Search;

public partial class SearchPagePresenter(ILogger<SearchPagePresenter> logger)
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

    public void Reset()
    {
        LogResettingSearchPage(logger);
        Clear();
    }

    [LoggerMessage(LogLevel.Debug, "Resetting search page")]
    static partial void LogResettingSearchPage(ILogger<SearchPagePresenter> logger);
}