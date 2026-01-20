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
        _view.TrackSelectionChanged += ViewOnTrackSelectionChanged;
        _view.TogglePage(Playlist.PlaylistPages.Empty);
    }    

    public async Task RefreshAsync()
    {
        await RefreshTracks();
    }

    public void Reset()
    {
        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.TogglePage(Playlist.PlaylistPages.Empty);
            _view?.RefreshTracks([]);
            return false;
        });        
    }    
    
    public void Receive(PlayerStateChangedMessage message)
    {
        if (!message.Value.HasFlag(PlayerStateChangedFlags.PlaybackState)) return;
        
        if (_aria.Player.State == PlaybackState.Stopped)
        {
            // TODO: Deselect any track.s
            //but this might already be part of currentTrack check.
        }
    }

    public void Receive(QueueStateChangedMessage message)
    {
        if (message.Value.HasFlag(QueueStateChangedFlags.Id))
            // The identifier of the playlist has changed.
            // We need to  reload the tracks
            _ = RefreshTracks();

        if (message.Value.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SelectTrackIndex(_aria.Queue.Order.CurrentIndex);
                return false;
            });        
        }
    }

    private void ViewOnTrackSelectionChanged(object? sender, uint e)
    {
        _aria.Queue.PlayAsync((int)e);
    }

    private async Task RefreshTracks()
    {
        try
        {
            LogRefreshingPlaylist();
            var tracks = (await _aria.Queue.GetTracksAsync()).ToList();

            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.RefreshTracks(tracks);
                _view?.TogglePage(tracks.Count != 0 ? Playlist.PlaylistPages.Tracks : Playlist.PlaylistPages.Empty);
                return false;
            });            
        }
        catch (Exception e)
        {
            LogCouldNotLoadTracks(e);
            _view?.TogglePage(Playlist.PlaylistPages.Empty);
            _messenger.Send(new ShowToastMessage("Could not load playlist"));
        }
    }

    [LoggerMessage(LogLevel.Error, "Could not load tracks")]
    partial void LogCouldNotLoadTracks(Exception e);

    [LoggerMessage(LogLevel.Information, "Refreshing playlist")]
    partial void LogRefreshingPlaylist();
}