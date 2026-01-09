using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure.Tagging;
using MpcNET;

namespace Aria.MusicServers.MPD.Commands;

// ReSharper disable once UnusedType.Global
public class GetCurrentSongInfoCommand : IMpcCommand<IEnumerable<KeyValuePair<string,string>>>
{
    public string Serialize()
    {
        return "currentsong";
    }

    public IEnumerable<KeyValuePair<string, string>> Deserialize(SerializedResponse response)
    {
        return response.ResponseValues.Count == 0 ? [] : response.ResponseValues;
    }
}