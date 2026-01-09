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
    // TODO: Implement guards for all these commands to ensure they can be invoked safely.
    public async Task PlayAsync() => await session.SendCommandAsync(new PlayCommand(0));

    public async Task PauseAsync() => await session.SendCommandAsync(new PauseResumeCommand(true));

    public async Task NextAsync() => await session.SendCommandAsync(new NextCommand());

    public async Task PreviousAsync() => await session.SendCommandAsync(new PreviousCommand());

    public async Task StopAsync() => await session.SendCommandAsync(new StopCommand());

    // TODO: A lot of properties are not implemented yet
    public Id Id { get; private set; }

    public int? Volume { get; private set; }

    public bool SupportsVolume { get; private set; }

    public PlaybackState State { get; private set; }

    public int? XFade { get; private set; }

    public bool CanXFade { get; private set; }

    public PlaybackProgress Progress { get; } = new();

    public SongInfo? CurrentSong { get; private set; }

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
            flags |= PlayerStateChangedFlags.State;
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
        
        // TODO: We are now remembering playlist information (context)).
        // Consider refactoring the UI  to use the playlist to fetch the current song.
        if (s.SongId != _currentSongIdx)
        {
            _currentSongIdx  = s.SongId;
            var (isSuccess, keyValuePairs) = await session.SendCommandAsync(new GetCurrentSongInfoCommand());
            if (!isSuccess) throw new InvalidOperationException("Failed to get current song info");
            if (keyValuePairs == null) throw new InvalidOperationException("No current song info found");
            
            var tagPairs = keyValuePairs.ToList();
            
            var tags = tagPairs.Select(kvp => new Tag(kvp.Key, kvp.Value)).ToList();
            if (tagPairs.Count == 0)
            {
                CurrentSong = null;
            }
            else
            {
                var songInfo = parser.ParseSongInformation(tags);
                CurrentSong = songInfo;
            }
            
            flags |= PlayerStateChangedFlags.CurrentSong;
        }
        
        if (flags != PlayerStateChangedFlags.None)
        {
            messenger.Send(new PlayerStateChangedMessage(flags));
        }
    }

    private int _currentSongIdx = -1;

}