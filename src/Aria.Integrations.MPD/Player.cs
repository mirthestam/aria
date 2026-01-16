using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Infrastructure.Tagging;
using Aria.MusicServers.MPD.Commands;
using CommunityToolkit.Mvvm.Messaging;
using MpcNET;
using MpcNET.Commands.Playback;

namespace Aria.MusicServers.MPD;

public class Player(Session session, IMessenger messenger, ITagParser parser) : IPlayer
{
    public async Task PlayAsync() => await session.SendCommandAsync(new PlayCommand(0));
    public async Task PauseAsync() => await session.SendCommandAsync(new PauseResumeCommand(true));
    public async Task NextAsync() => await session.SendCommandAsync(new NextCommand());
    public async Task PreviousAsync() => await session.SendCommandAsync(new PreviousCommand());
    public async Task StopAsync() => await session.SendCommandAsync(new StopCommand());
    
    public Id Id { get; private set; }

    public int? Volume { get; private set; }

    public bool SupportsVolume { get; private set; }

    public PlaybackState State { get; private set; }

    public int? XFade { get; private set; }

    public bool CanXFade { get; private set; }

    public PlaybackProgress Progress { get; } = new();

    public async Task UpdateFromStatusAsync(MpdStatus s)
    {
        var flags = PlayerStateChangedFlags.None;

        var newState = s.State switch
        {
            MpdState.Play => PlaybackState.Playing,
            MpdState.Stop => PlaybackState.Stopped,
            MpdState.Pause => PlaybackState.Paused,
            _ => PlaybackState.Unknown
        };
        if (State != newState)
        {
            State = newState;
            flags |= PlayerStateChangedFlags.PlaybackState;
        }

        // Check Volume
        var newVol = s.Volume == -1 ? null : (int?)s.Volume;
        if (Volume != newVol)
        {
            Volume = newVol;
            flags |= PlayerStateChangedFlags.Volume;
        }

        // Check Progress
        if (Progress.Elapsed != s.Elapsed || Progress.Duration != s.Duration)
        {
            Progress.Elapsed = s.Elapsed;
            Progress.Duration = s.Duration;
            flags |= PlayerStateChangedFlags.Progress;
        }
        
        if (flags != PlayerStateChangedFlags.None)
        {
            messenger.Send(new PlayerStateChangedMessage(flags));
        }
    }
}