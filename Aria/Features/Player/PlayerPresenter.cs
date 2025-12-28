using Aria.Core;
using Aria.Core.Player;
using CommunityToolkit.Mvvm.Messaging;
using Gio;

namespace Aria.Features.Player;

public class PlayerPresenter : IRecipient<PlayerStateChangedMessage>
{
    private readonly IPlaybackApi _api;
    private Player? _view;

    public PlayerPresenter(IPlaybackApi api, IMessenger messenger)
    {
        _api = api;
        messenger.Register(this);
    }

    public void Receive(PlayerStateChangedMessage message)
    {
        _view?.PlayerStateChanged(message.Value, _api);
    }

    public void Attach(Player player)
    {
        _view = player;

        var actionGroup = SimpleActionGroup.New();

        var nextAction = SimpleAction.New("next", null);
        nextAction.OnActivate += NextActionOnOnActivate;
        actionGroup.AddAction(nextAction);

        var prevAction = SimpleAction.New("prev", null);
        prevAction.OnActivate += PrevActionOnOnActivate;
        actionGroup.AddAction(prevAction);

        _view.InsertActionGroup("player", actionGroup);
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