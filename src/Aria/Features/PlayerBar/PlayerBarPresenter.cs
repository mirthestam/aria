using Aria.Core;
using Aria.Core.Player;
using Aria.Core.Queue;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.PlayerBar;

public partial class PlayerBarPresenter : IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _aria;
    private readonly ILogger<PlayerBarPresenter> _logger;
    private readonly IMessenger _messenger;
    private PlayerBar? _view;

    public PlayerBarPresenter(IAria aria, IMessenger messenger, ILogger<PlayerBarPresenter> logger)
    {
        _logger = logger;
        _aria = aria;
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
        Refresh(message.Value);
    }

    public void Receive(QueueStateChangedMessage message)
    {
        Refresh(message.Value);
    }

    private void Refresh(PlayerStateChangedFlags flags)
    {
        if (flags.HasFlag(PlayerStateChangedFlags.PlaybackState))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SetPlaybackState(_aria.Player.State);
                return false;
            });            
        }
        if (flags.HasFlag(PlayerStateChangedFlags.Progress))
        {
            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.SetProgress(_aria.Player.Progress.Elapsed, _aria.Player.Progress.Duration);
                return false;
            });            
        }
    }

    private void Refresh(QueueStateChangedFlags flags)
    {
        if (!flags.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;

        var track = _aria.Queue.CurrentTrack;
        
        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.SetCurrentTrack(track);
            return false;
        });                    

        //_ = LoadCover();
    }
}