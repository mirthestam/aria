using MpcNET;

namespace Aria.Backends.MPD.Connection.Commands.Playlist;

public class PlaylistInfoCommand : IMpcCommand<IEnumerable<KeyValuePair<string,string>>>
{
    public string Serialize() => "playlistinfo";

    public IEnumerable<KeyValuePair<string,string>> Deserialize(SerializedResponse response)
    {
        return response.ResponseValues;
    }
}