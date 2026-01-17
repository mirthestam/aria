using Aria.Core;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Shell;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.PlayerBar;

public partial class PlayerBarPresenter : IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _api;
    private readonly ILogger<PlayerBarPresenter> _logger;
    private readonly IMessenger _messenger;
    private PlayerBar? _view;

    public PlayerBarPresenter(IAria api, IMessenger messenger, ILogger<PlayerBarPresenter> logger)
    {
        _logger = logger;
        _api = api;
        _messenger = messenger;
        messenger.Register<PlayerStateChangedMessage>(this);
        messenger.Register<QueueStateChangedMessage>(this);
    }
    
    public async Task RefreshAsync()
    {
        // TODO: Implement here logic like on the player
    }

    public void Reset(){}
    
    public void Attach(PlayerBar bar)
    {
        _view = bar;
    }    

    public void Receive(PlayerStateChangedMessage message)
    {
        _view?.PlayerStateChanged(message.Value, _api);
    }

    public void Receive(QueueStateChangedMessage message)
    {
        _view?.QueueStateChanged(message.Value, _api);
    }

    private async void PrevActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _api.PlayerProxy.PreviousAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to previous song"));
        }
    }

    private async void NextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _api.PlayerProxy.NextAsync();
        }
        catch (Exception e)
        {
            PlayerActionFailed(e, sender.Name);
            _messenger.Send(new ShowToastMessage("Failed to go to next song"));
        }
    }

    [LoggerMessage(LogLevel.Error, "Player action failed: {action}")]
    partial void PlayerActionFailed(Exception e, string? action);
}