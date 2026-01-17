using Aria.Core.Player;
using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using CommunityToolkit.Mvvm.Messaging;
using MpcNET;
using MpcNET.Commands.Playback;

namespace Aria.MusicServers.MPD;

public class Player(Session session, IMessenger messenger, ITagParser parser) : BasePlayer
{
    public override async Task PlayAsync() => await session.SendCommandAsync(new PlayCommand(0));
    public override async Task PauseAsync() => await session.SendCommandAsync(new PauseResumeCommand(true));
    public override async Task NextAsync() => await session.SendCommandAsync(new NextCommand());
    public override async Task PreviousAsync() => await session.SendCommandAsync(new PreviousCommand());
    public override async Task StopAsync() => await session.SendCommandAsync(new StopCommand());
    
    public async Task UpdateFromStatusAsync(MpdStatus s)
    {
        await Task.CompletedTask;
        
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