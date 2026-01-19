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
}