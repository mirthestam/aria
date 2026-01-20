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

    public override async Task<IEnumerable<TrackInfo>> GetTracksAsync()
    {
        await Task.Delay(BackendConnection.Delay).ConfigureAwait(false);
        var tracks = new List<TrackInfo>
        {
            Library.DebuggingTrack,
            Library.IlTalkTrack
        };

        return tracks;
    }

    public override TrackInfo? CurrentTrack => Library.DebuggingTrack;
}