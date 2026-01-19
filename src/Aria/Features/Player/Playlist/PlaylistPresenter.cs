using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Shell;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Player.Playlist;

public partial class PlaylistPresenter : IRecipient<QueueStateChangedMessage>, IRecipient<PlayerStateChangedMessage>
{
    private readonly ILogger<PlaylistPresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IAria _aria;

    private Playlist? _view;

    public PlaylistPresenter(IMessenger messenger, IAria aria, ILogger<PlaylistPresenter> logger)
    {
        _logger = logger;
        _messenger = messenger;
        _aria = aria;
        _messenger.Register<QueueStateChangedMessage>(this);
        _messenger.Register<PlayerStateChangedMessage>(this);
    }

    public void Attach(Playlist view)
    {
        _view = view;
        _view.SongSelectionChanged += ViewOnSongSelectionChanged;
        _view.TogglePage(Playlist.PlaylistPages.Empty);
    }    

    public async Task RefreshAsync()
    {
        await RefreshSongs();
    }

    public void Reset()
    {
        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.TogglePage(Playlist.PlaylistPages.Empty);
            _view?.RefreshSongs([]);
            return false;
        });        
    }    
    
    public void Receive(PlayerStateChangedMessage message)
    {
        if (!message.Value.HasFlag(PlayerStateChangedFlags.PlaybackState)) return;
        
        if (_aria.Player.State == PlaybackState.Stopped)
        {
            // TODO: Deselect any song,.
            //but this might already be part of currentSong check.
        }
    }

    public void Receive(QueueStateChangedMessage message)
    {
        if (message.Value.HasFlag(QueueStateChangedFlags.Id))
            // The identifier of the playlist has changed.
            // We need to  reload the songs
            _ = RefreshSongs();

        if (message.Value.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SelectSongIndex(_aria.Queue.Order.CurrentIndex);
                return false;
            });        
        }
    }

    private void ViewOnSongSelectionChanged(object? sender, uint e)
    {
        _aria.Queue.PlayAsync((int)e);
    }

    private async Task RefreshSongs()
    {
        try
        {
            LogRefreshingPlaylist();
            var songs = (await _aria.Queue.GetSongsAsync()).ToList();

            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.RefreshSongs(songs);
                _view?.TogglePage(songs.Count != 0 ? Playlist.PlaylistPages.Songs : Playlist.PlaylistPages.Empty);
                return false;
            });            
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

    [LoggerMessage(LogLevel.Information, "Refreshing playlist")]
    partial void LogRefreshingPlaylist();
}