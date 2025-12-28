using MpcNET;
using MpcNET.Tags;
using MpcNET.Types;
using MpcNET.Types.Filters;

namespace Aria.MusicServers.MPD.Commands;

// TODO: This file is adapted from the underlying MPD library to provide direct access to key–value pairs.
// The default library returns its own IMpdFile instead. This results in duplicated code and should be refactored.

/// <summary>
///     Base class for find/search commands.
/// </summary>
public abstract class BaseFilterCommand : IMpcCommand<IEnumerable<KeyValuePair<string, string>>>
{
    private readonly int _end;
    private readonly List<IFilter> _filters;
    private readonly int _start;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BaseFilterCommand" /> class.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <param name="searchText">The search text.</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    /// <param name="operand">Operator of the filter</param>
    public BaseFilterCommand(ITag tag, string searchText, int windowStart = -1, int windowEnd = -1,
        FilterOperator operand = FilterOperator.Equal)
    {
        _filters = new List<IFilter>();
        var Tag = new FilterTag(tag, searchText, operand);
        _filters.Add(Tag);

        _start = windowStart;
        _end = windowEnd;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BaseFilterCommand" /> class.
    /// </summary>
    /// <param name="filters">List of key/value filters</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    /// <param name="operand">Operator of the filter</param>
    public BaseFilterCommand(List<KeyValuePair<ITag, string>> filters, int windowStart = -1, int windowEnd = -1,
        FilterOperator operand = FilterOperator.Equal)
    {
        _filters = new List<IFilter>();
        _filters.AddRange(filters.Select(filter => new FilterTag(filter.Key, filter.Value, operand)).ToList());

        _start = windowStart;
        _end = windowEnd;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BaseFilterCommand" /> class.
    /// </summary>
    /// <param name="filters">Filter</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public BaseFilterCommand(IFilter filters, int windowStart = -1, int windowEnd = -1)
    {
        _filters = new List<IFilter>();
        _filters.Add(filters);

        _start = windowStart;
        _end = windowEnd;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BaseFilterCommand" /> class.
    /// </summary>
    /// <param name="filters">List of filters</param>
    /// <param name="windowStart">Start of the portion of the results desired</param>
    /// <param name="windowEnd">End of the portion of the results desired</param>
    public BaseFilterCommand(List<IFilter> filters, int windowStart = -1, int windowEnd = -1)
    {
        _filters = filters;

        _start = windowStart;
        _end = windowEnd;
    }

    /// <summary>
    ///     Name of the command to use when deserializing
    /// </summary>
    public abstract string CommandName { get; }

    /// <summary>
    ///     Serializes the command.
    /// </summary>
    /// <returns>
    ///     The serialized command.
    /// </returns>
    public string Serialize()
    {
        var cmd = "";

        if (_filters != null)
        {
            var serializedFilters = string.Join(" AND ",
                _filters.Select(x => $"{x.GetFormattedCommand()}")
            );
            cmd = $@"{CommandName} ""({serializedFilters})""";
        }

        if (_start > -1) cmd += $" window {_start}:{_end}";

        return cmd;
    }

    /// <summary>
    ///     Deserializes the specified response text pairs.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>
    ///     The deserialized response.
    /// </returns>
    public IEnumerable<KeyValuePair<string, string>> Deserialize(SerializedResponse response)
    {
        return response.ResponseValues;
    }
}