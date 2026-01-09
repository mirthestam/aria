using Aria.Features.Browser;
using Aria.Features.Player;
using Aria.Features.PlayerBar;

namespace Aria.Main;

public class MainPagePresenter(
    BrowserHostPresenter browserHostPresenter,
    PlayerPresenter playerPresenter,
    PlayerBarPresenter playerBarPresenter)
{
    public void Attach(MainPage view)
    {
        browserHostPresenter.Attach(view.BrowserHost);
        playerBarPresenter.Attach(view.PlayerBar);
        playerPresenter.Attach(view.Player);
    }
}