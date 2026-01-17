using Aria.Core.Player;
using Aria.Infrastructure;

namespace Aria.Backends.Stub;

public class Player : BasePlayer
{
    public override PlaybackState State => PlaybackState.Playing;
    public override PlaybackProgress Progress => new()
    {
        Id = new StubId(0),
        Elapsed = TimeSpan.FromSeconds(15),
        Duration = TimeSpan.FromSeconds(60),
        AudioBits = 24,
        AudioChannels = 2,
        AudioSampleRate = 96000,
        Bitrate = 320
    };
}