using Aria.Features.Browser;
using Aria.Features.Player;
using Aria.Features.PlayerBar;

namespace Aria.Main;

public class MainPagePresenter(
    BrowserPresenter browserPresenter,
    PlayerPresenter playerPresenter,
    PlayerBarPresenter playerBarPresenter)
{
    public void Attach(MainPage view)
    {
        browserPresenter.Attach(view.Browser);
        playerBarPresenter.Attach(view.PlayerBar);
        playerPresenter.Attach(view.Player);
    }
}