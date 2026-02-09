using Aria.Backends.MPD.Connection;
using Aria.Core.Player;
using Aria.Infrastructure;
using MpcNET;
using MpcNET.Commands.Playback;

namespace Aria.Backends.MPD;

public class Player(Client client) : BasePlayer
{
    public override async Task PlayAsync(int index) => await client.SendCommandAsync(new PlayCommand(index)).ConfigureAwait(false);
    public override async Task PauseAsync() => await client.SendCommandAsync(new PauseResumeCommand(true)).ConfigureAwait(false);
    public override async Task ResumeAsync() => await client.SendCommandAsync(new PauseResumeCommand(false)).ConfigureAwait(false);
    public override async Task NextAsync() => await client.SendCommandAsync(new NextCommand()).ConfigureAwait(false);
    public override async Task PreviousAsync() => await client.SendCommandAsync(new PreviousCommand()).ConfigureAwait(false);
    public override async Task StopAsync() => await client.SendCommandAsync(new StopCommand()).ConfigureAwait(false);
    public override async Task SetVolumeAsync(int volume)
    {
        await client.SendCommandAsync(new SetVolumeCommand((byte)volume)).ConfigureAwait(false);
    }

    public override async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        var command = new SeekCurCommand(position.TotalSeconds);
        await client.SendCommandAsync(command, token: cancellationToken).ConfigureAwait(false);
    }

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
            Progress.AudioBits = s.AudioBits;
            Progress.AudioChannels = s.AudioChannels;
            Progress.AudioSampleRate = s.AudioSampleRate;
            Progress.Bitrate = s.Bitrate;
            Progress.Elapsed = s.Elapsed;
            Progress.Duration = s.Duration;
            flags |= PlayerStateChangedFlags.Progress;
        }
        
        if (flags != PlayerStateChangedFlags.None)
        {
            OnStateChanged(flags);
        }
    }
}