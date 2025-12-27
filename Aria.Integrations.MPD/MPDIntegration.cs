using Aria.Integrations.MPD.Events;
using MpcNET;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Status;

namespace Aria.Integrations.MPD;

public class SongId(int id) : Id.TypedId<int>(id, "SNG");
public class PlayerId(string name) : Id.TypedId<string>(name, "PLR");
public class PlaylistId(int id) : Id.TypedId<int>(id, "PL");

public sealed class MPDIntegration : Integration
{
    private readonly MPDSession _mpdSession = new();
    
    public override bool IsConnected => _mpdSession.IsConnected;
    
    public override async Task PlayAsync()
    {
        await _mpdSession.SendCommandAsync(new PlayCommand(0));
    }

    public override async Task PauseAsync()
    {
        await _mpdSession.SendCommandAsync(new PauseResumeCommand(true));
    }

    public override async Task NextAsync()
    {
        await _mpdSession.SendCommandAsync(new NextCommand());
    }

    public override async Task PreviousAsync()
    {
        await _mpdSession.SendCommandAsync(new PreviousCommand());
    }

    public override async Task StopAsync()
    {
        await _mpdSession.SendCommandAsync(new StopCommand());
    }
    
    public void SetCredentials(MPDCredentials credentials) => _mpdSession.Credentials = credentials;
    
    public async Task InitializeAsync()
    {
        _mpdSession.ConnectionChanged += (_, _) =>
        {
            OnConnectionChanged(EventArgs.Empty);
        };
        
        _mpdSession.IdleResponseReceived  += MPDSessionOnIdleResponseReceived;
        _mpdSession.StatusChanged +=  MPDSessionOnStatusChanged;
        
        await _mpdSession.InitializeAsync();
    }

    private void MPDSessionOnStatusChanged(object? sender, MPDStatusChangedEventArgs e)
    {
        var oldSongId = Status.Song.Id;

        Status.Song.AudioBits = e.Status.AudioBits; // Current Song
        Status.Song.AudioChannels = e.Status.AudioChannels; // Current Song
        Status.Song.AudioSampleRate = e.Status.AudioSampleRate;
        Status.Song.Bitrate = e.Status.Bitrate;
        Status.Playlist.NextSongId =  new SongId(e.Status.NextSong);

        Status.Playlist.Length = e.Status.PlaylistLength;
        
        Status.Song.Elapsed = e.Status.Elapsed;
        Status.Song.Duration = e.Status.Duration;
        Status.Song.Id = new SongId(e.Status.SongId);

        Status.Playlist.Id = new PlaylistId(e.Status.Playlist);
        
        Status.Playlist.CurrentSongIndex = e.Status.Song;
        
        Status.Playlist.CanConsume = true;
        Status.Playlist.ConsumeEnabled = e.Status.Consume;

        Status.Playlist.CanShuffle = true;
        Status.Playlist.ShuffleEnabled = e.Status.Random;

        Status.Playlist.CanRepeat = true;
        Status.Playlist.RepeatEnabled = e.Status.Repeat;
        
        Status.Playlist.SingleEnabled = e.Status.Single;
        
        Status.Player.Id = new PlayerId(e.Status.Partition);
        
        Status.Player.SupportsVolume = e.Status.Volume != -1;
        Status.Player.Volume = e.Status.Volume != -1 ? null : e.Status.Volume;
        Status.Player.State = e.Status.State switch
        {
            MpdState.Play => PlayerState.Playing,
            MpdState.Stop => PlayerState.Stopped,
            MpdState.Pause => PlayerState.Paused,
            _ => PlayerState.Unknown
        };

        Status.Player.CanXFade = true;
        Status.Player.XFade = e.Status.XFade;
        
        OnStatusChanged(EventArgs.Empty);
        
        if (oldSongId != Status.Song.Id) return;
            OnSongChanged(new SongChangedEventArgs(Status.Song.Id));
    }

    private async void MPDSessionOnIdleResponseReceived(object? sender, MPDIdleResponseEventArgs e)
    {
        try
        {
            var subsystems = e.Message;
            if (subsystems.Contains("playlist"))
            {
                // Queue has changed
                OnQueueChanged(EventArgs.Empty);
            }

            if (subsystems.Contains("stored_playlist"))
            {
                // m3u playlists have changed
                //  await UpdatePlaylistsAsync();
            }

            if (!subsystems.Contains("player") && !subsystems.Contains("mixer") && !subsystems.Contains("output") &&
                !subsystems.Contains("options") && !subsystems.Contains("update")) return;
            
            // Status has changed in a significant way.
            // Force  an update (so, this is in addition of the 1second polling).
            await _mpdSession.UpdateStatusAsync(MPDConnectionType.Idle);
                
            if (subsystems.Contains("player"))
            {
                // Specifically, song has changed
                OnSongChanged(new SongChangedEventArgs(Status.Song.Id));
            }
        }
        catch (Exception ex)
        {
            throw; // TODO handle exception
        }
    }
}
