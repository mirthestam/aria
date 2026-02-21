using Aria.Backends.MPD.Connection.Commands.Find;
using MpcNET.Tags;
using MpcNET.Types;

namespace Aria.Backends.MPD.Connection.Commands.Search;

/// <summary>
///     Finds tracks in the database that contain "searchText" and adds them to the queue.
///     Since MPD 0.21, search syntax is now (TAG == 'VALUE').
///     https://mpd.readthedocs.io/en/stable/protocol.html#filters
/// </summary>
public class SearchAddCommand : BaseFilterCommand
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SearchAddCommand" /> class.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <param name="searchText">The search text.</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public SearchAddCommand(ITag tag, string searchText, int windowStart = -1, int windowEnd = -1) : base(tag,
        searchText, windowStart, windowEnd)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SearchAddCommand" /> class.
    /// </summary>
    /// <param name="filters">List of key/value filters</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public SearchAddCommand(List<KeyValuePair<ITag, string>> filters, int windowStart = -1, int windowEnd = -1) : base(
        filters, windowStart, windowEnd)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SearchAddCommand" /> class.
    /// </summary>
    /// <param name="filter">Filter</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public SearchAddCommand(IFilter filter, int windowStart = -1, int windowEnd = -1) : base(filter, windowStart,
        windowEnd)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SearchAddCommand" /> class.
    /// </summary>
    /// <param name="filters">List of filters</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public SearchAddCommand(List<IFilter> filters, int windowStart = -1, int windowEnd = -1) : base(filters,
        windowStart, windowEnd)
    {
    }

    /// <summary>
    /// </summary>
    public override string CommandName => "searchadd";
}