using Aria.Core;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Aria.Features.Player.Playlist;
using Aria.Main;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Player;

public partial class PlayerPresenter : IRecipient<PlayerStateChangedMessage>,
    IRecipient<PlaylistChangedMessage>
{
    private readonly ILogger<PlayerPresenter> _logger;
    private readonly PlaylistPresenter _playlistPresenter;
    private readonly IMessenger _messenger;
    private readonly IPlaybackApi _api;
    private Player? _view;

    private SimpleAction _nextAction;
    private SimpleAction _prevAction;

    public PlayerPresenter(IPlaybackApi api, IMessenger messenger, PlaylistPresenter playlistPresenter, ILogger<PlayerPresenter> logger)
    {
        _logger = logger;
        _playlistPresenter = playlistPresenter;
        _api = api;
        _messenger = messenger;
        messenger.Register<PlayerStateChangedMessage>(this);
        messenger.Register<PlaylistChangedMessage>(this);       
    }

    public void Receive(PlayerStateChangedMessage message)
    {
        _view?.PlayerStateChanged(message.Value, _api);
    }
    
    public void Receive(PlaylistChangedMessage message)
    {
        if (message.Value.HasFlag(PlaylistStateChangedFlags.PlaybackOrder))
        {
            _prevAction.SetEnabled(_api.Playlist.Order.CurrentIndex > 0);
            _nextAction.SetEnabled(_api.Playlist.Order.NextSongId != null);            
        }
    }    

    public void Attach(Player player)
    {
        _view = player;
        
        _playlistPresenter.Attach(_view.Playlist);        

        var actionGroup = SimpleActionGroup.New();

        _nextAction = SimpleAction.New("next", null);
        _nextAction.OnActivate += NextActionOnOnActivate;
        actionGroup.AddAction(_nextAction);

        _prevAction = SimpleAction.New("prev", null);
        _prevAction.OnActivate += PrevActionOnOnActivate;
        actionGroup.AddAction(_prevAction);

        _view.InsertActionGroup("player", actionGroup);
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
}