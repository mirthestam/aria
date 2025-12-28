using Aria.Core;
using Aria.Core.Library;
using Aria.Infrastructure.Tagging;
using MpcNET;

namespace Aria.MusicServers.MPD.Commands;

// ReSharper disable once UnusedType.Global
public class GetCurrentSongInfoCommand(ITagParser parser) : IMpcCommand<SongInfo?>
{
    public string Serialize()
    {
        return "currentsong";
    }

    public SongInfo? Deserialize(SerializedResponse response)
    {
        if (response.ResponseValues.Count == 0) return null;

        // TODO: This currently uses the tag parser. Refactor it to return only the key–value pairs.
        // The calling object should be responsible for applying the tag parser.
        var fileName = response.ResponseValues.First(x => x.Key == "FileName").Value;
        return parser.ParseSongInformation(new SongId(fileName), response.ResponseValues);
    }
}