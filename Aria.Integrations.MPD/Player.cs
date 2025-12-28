using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Player;
using CommunityToolkit.Mvvm.Messaging;
using MpcNET;
using MpcNET.Commands.Playback;

namespace Aria.MusicServers.MPD;

public class Player(Session session, IMessenger messenger) : IPlayer
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
        
        await Task.CompletedTask;
        
        // TODO: Implement status updates here.  
        // The current issue is that we receive only playlist song and playing song info, not the full metadata of the actual song.
        // To get the actual metadata, we need to reference the playlist for this index.
        // var newSongId = new SongId(s…);

        // var currentSongId = CurrentSong?.Id as SongId;
        // if (currentSongId?.Value != newSongId.Value)
        // {
        //     var parser  = new MPDTagParser();
        //     var response = await session.SendCommandAsync(new GetCurrentSongInfoCommand(parser));
        //
        //     CurrentSong = response;
        //     flags |= PlayerStateChangedFlags.CurrentSong;
        //
        //     // if (response != null)
        //     // {
        //     //     CurrentSong = new SongInfo
        //     //     {
        //     //         Id = newSongId,
        //     //         AlbumArtist = response.AlbumArtist,
        //     //         Artist = response.Artist,
        //     //         Composer = response.Composer,
        //     //         Duration = TimeSpan.FromSeconds(response.Time),
        //     //         Title = response.Title
        //     //     };
        //     //     flags |= PlayerStateChangedFlags.CurrentSong;                
        //     // }
        // }
        //
        // if (flags != PlayerStateChangedFlags.None)
        // {
        //     messenger.Send(new PlayerStateChangedMessage(flags));
        // }
    }
}