using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Aria.Features.Player.Playlist;
using Aria.Infrastructure;
using Aria.Main;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Player;

public partial class PlayerPresenter : IRecipient<PlayerStateChangedMessage>,
    IRecipient<QueueChangedMessage>
{
    private readonly ILogger<PlayerPresenter> _logger;
    private readonly PlaylistPresenter _playlistPresenter;
    private readonly IMessenger _messenger;
    private readonly IPlaybackApi _api;
    private readonly ResourceTextureLoader _resourceTextureLoader;
    private Player? _view;
    

    public PlayerPresenter(ILogger<PlayerPresenter> logger, IMessenger messenger, IPlaybackApi api,
        ResourceTextureLoader resourceTextureLoader, PlaylistPresenter playlistPresenter)
    {
        _logger = logger;
        _resourceTextureLoader = resourceTextureLoader;
        _playlistPresenter = playlistPresenter;
        _api = api;
        _messenger = messenger;
        messenger.Register<PlayerStateChangedMessage>(this);
        messenger.Register<QueueChangedMessage>(this);       
    }

    public void Receive(PlayerStateChangedMessage message)
    {
        _view?.PlayerStateChanged(message.Value, _api);
    }

    private async System.Threading.Tasks.Task LoadCover()
    {
        try
        {
            var song = _api.Queue.CurrentSong;
            if (song == null) return;

            var coverInfo = song.Assets.FrontCover;
            var texture = await _resourceTextureLoader.LoadFromAlbumResourceAsync(coverInfo?.Id ?? Id.Empty);
            if (texture == null) return;
            _view?.LoadCover(texture);
        }
        catch (Exception e)
        {
            LogFailedToLoadAlbumCover(e);
        }
    }
    
    public void Receive(QueueChangedMessage message)
    {
        _view?.QueueStateChanged(message.Value, _api);
        if (!message.Value.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;
        
        _view?.PrevAction.SetEnabled(_api.Queue.Order.CurrentIndex > 0);
        _view?.NextAction.SetEnabled(_api.Queue.Order.HasNext);
        _ = LoadCover();
    }    

    public void Attach(Player player)
    {
        _view = player;
        
        _playlistPresenter.Attach(_view.Playlist);        
        
        _view.NextAction.OnActivate += NextActionOnOnActivate;
        _view.PrevAction.OnActivate += PrevActionOnOnActivate;
    }

    private async void PrevActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _api.Player.PreviousAsync();
        }
        catch(Exception e)
        {
            PlayerActionFailed(e, sender.Name);            
            _messenger.Send(new ShowToastMessage("Failed to go to previous song"));
        }
    }

    private async void NextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _api.Player.NextAsync();
        }
        catch(Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to next song"));
        }
    }
    
    [LoggerMessage(LogLevel.Error, "Player action failed: {action}")]
    partial void PlayerActionFailed(Exception e, string? action);

    [LoggerMessage(LogLevel.Error, "Failed to load album cover")]
    partial void LogFailedToLoadAlbumCover(Exception e);
}