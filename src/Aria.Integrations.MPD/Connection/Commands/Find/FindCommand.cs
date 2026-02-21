using MpcNET.Tags;
using MpcNET.Types;

namespace Aria.Backends.MPD.Connection.Commands.Find;

/// <summary>
///     Finds tracks in the database that is exactly "searchText".
///     Since MPD 0.21, search syntax is now (TAG == 'VALUE').
///     https://mpd.readthedocs.io/en/stable/protocol.html#filters
/// </summary>
public class FindCommand : BaseFilterCommand
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FindCommand" /> class.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <param name="searchText">The search text.</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public FindCommand(ITag tag, string searchText, int windowStart = -1, int windowEnd = -1) : base(tag, searchText,
        windowStart, windowEnd)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FindCommand" /> class.
    /// </summary>
    /// <param name="filters">List of key/value filters</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public FindCommand(List<KeyValuePair<ITag, string>> filters, int windowStart = -1, int windowEnd = -1) : base(
        filters, windowStart, windowEnd)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FindCommand" /> class.
    /// </summary>
    /// <param name="filter">Filter</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public FindCommand(IFilter filter, int windowStart = -1, int windowEnd = -1) : base(filter, windowStart, windowEnd)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FindCommand" /> class.
    /// </summary>
    /// <param name="filters">List of filters</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public FindCommand(List<IFilter> filters, int windowStart = -1, int windowEnd = -1) : base(filters, windowStart,
        windowEnd)
    {
    }

    /// <summary>
    /// </summary>
    public override string CommandName => "find";
}