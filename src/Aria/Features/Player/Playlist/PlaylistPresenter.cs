using Aria.Core;
using Aria.Core.Playlist;
using Aria.Main;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Player.Playlist;

public partial class PlaylistPresenter : IRecipient<PlaylistChangedMessage>
{
    private readonly ILogger<PlaylistPresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IPlaybackApi _playbackApi;
    
    public PlaylistPresenter(IMessenger messenger, IPlaybackApi playbackApi, ILogger<PlaylistPresenter> logger)
    {
        _logger = logger;
        _messenger = messenger;
        _playbackApi = playbackApi;
        _messenger.Register(this);
    }

    private Playlist? _view;
    
    public void Attach(Playlist view)
    {
        _view = view;
        _view.TogglePage(Playlist.PlaylistPages.Empty);
    }

    public void Receive(PlaylistChangedMessage message)
    {
        if (message.Value.HasFlag(PlaylistStateChangedFlags.Id))
        {
            // The identifier of the playlist has changed.
            // We need to  reload the songs
            _ = RefreshSongs();
        }
    }

    private async Task RefreshSongs()
    {
        try
        {
            var songs = (await _playbackApi.Playlist.GetSongsAsync()).ToList();
            
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