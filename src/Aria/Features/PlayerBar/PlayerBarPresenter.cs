using Aria.Core;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Aria.Main;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;

namespace Aria.Features.PlayerBar;

public partial class PlayerBarPresenter : IRecipient<PlayerStateChangedMessage>, IRecipient<QueueChangedMessage>
{
    private readonly ILogger<PlayerBarPresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IPlaybackApi _api;
    private PlayerBar? _view;
    
    public PlayerBarPresenter(IPlaybackApi api, IMessenger messenger, ILogger<PlayerBarPresenter> logger)
    {
        _logger = logger;
        _api = api;
        _messenger = messenger;
        messenger.Register<PlayerStateChangedMessage>(this);
        messenger.Register<QueueChangedMessage>(this);
    }

    public void Receive(PlayerStateChangedMessage message)
    {
        _view?.PlayerStateChanged(message.Value, _api);
    }
    
    public void Receive(QueueChangedMessage message)
    {
        _view?.QueueStateChanged(message.Value, _api);
    }    

    public void Attach(PlayerBar bar)
    {
        _view = bar;
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