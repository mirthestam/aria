using Aria.Core;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Aria.Main;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Player.Playlist;

public partial class PlaylistPresenter : IRecipient<QueueChangedMessage>, IRecipient<PlayerStateChangedMessage>
{
    private readonly ILogger<PlaylistPresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IPlaybackApi _playbackApi;
    
    public PlaylistPresenter(IMessenger messenger, IPlaybackApi playbackApi, ILogger<PlaylistPresenter> logger)
    {
        _logger = logger;
        _messenger = messenger;
        _playbackApi = playbackApi;
        _messenger.Register<QueueChangedMessage>(this);
        _messenger.Register<PlayerStateChangedMessage>(this);
    }

    private Playlist? _view;
    
    public void Attach(Playlist view)
    {
        _view = view;
        _view.SongSelectionChanged += ViewOnSongSelectionChanged;
        _view.TogglePage(Playlist.PlaylistPages.Empty);
    }

    private void ViewOnSongSelectionChanged(object? sender, uint e)
    {
        _playbackApi.Queue.PlayAsync((int)e);
    }

    public void Receive(QueueChangedMessage message)
    {
        if (message.Value.HasFlag(QueueStateChangedFlags.Id))
        {
            // The identifier of the playlist has changed.
            // We need to  reload the songs
            _ = RefreshSongs();
        }

        if (message.Value.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            _view?.SelectSongIndex(_playbackApi.Queue.Order.CurrentIndex);
        }
    }
    
    public void Receive(PlayerStateChangedMessage message)
    {
        if (message.Value.HasFlag(PlayerStateChangedFlags.PlaybackState))
        {
            if (_playbackApi.Player.State == PlaybackState.Stopped)
            {
                // TODO: Deselect any song,.
                //but this might already be part of currentSong check.
            }
        }
    }    

    private async Task RefreshSongs()
    {
        try
        {
            var songs = (await _playbackApi.Queue.GetSongsAsync()).ToList();
            
            _view?.RefreshSongs(songs);
            _view?.TogglePage(songs.Count != 0 ? Playlist.PlaylistPages.Songs : Playlist.PlaylistPages.Empty);
        }
        catch (Exception e)
        {
            LogCouldNotLoadSongs(e);
            _view?.TogglePage(Playlist.PlaylistPages.Empty);
            _messenger.Send(new ShowToastMessage("Could not load playlist"));            
        }
    }
    
    [LoggerMessage(LogLevel.Error, "Could not load songs")]
    partial void LogCouldNotLoadSongs(Exception e);

    
}