using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Infrastructure;

namespace Aria.Backends.Stub;

public class Queue : BaseQueue
{
    public override PlaybackOrder Order => new PlaybackOrder
    {
        CurrentIndex = 0,
        HasNext = true
    };

    public override int Length => 2;

    public override async Task<IEnumerable<SongInfo>> GetSongsAsync()
    {
        await Task.Delay(BackendConnection.Delay).ConfigureAwait(false);
        var songs = new List<SongInfo>
        {
            Library.DebuggingSong,
            Library.ILTalkSong
        };

        return songs;
    }

    public override SongInfo? CurrentSong => Library.DebuggingSong;
}