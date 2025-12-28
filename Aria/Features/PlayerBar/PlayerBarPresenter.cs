using Aria.Core;
using Aria.Core.Player;
using CommunityToolkit.Mvvm.Messaging;
using Gio;

namespace Aria.Features.PlayerBar;

public class PlayerBarPresenter : IRecipient<PlayerStateChangedMessage>
{
    private readonly IPlaybackApi _api;
    private PlayerBar? _view;


    public PlayerBarPresenter(IPlaybackApi api, IMessenger messenger)
    {
        _api = api;
        messenger.Register(this);
    }

    public void Receive(PlayerStateChangedMessage message)
    {
        _view?.PlayerStateChanged(message.Value, _api);
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
        catch
        {
            //  eat
        }
    }

    private async void NextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _api.Player.NextAsync();
        }
        catch
        {
            // eat
        }
    }
}