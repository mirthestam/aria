using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Main;

public class MainWindowPresenter : IRecipient<ConnectionChangedMessage>
{
    private readonly IAppSession _appSession;
    private readonly MainPagePresenter _mainPagePresenter;
    private readonly WelcomePagePresenter _welcomePagePresenter;
    private MainWindow _view;

    public MainWindowPresenter(IMessenger messenger,
        MainPagePresenter mainPagePresenter,
        WelcomePagePresenter welcomePagePresenter,
        IAppSession appSession)
    {
        _mainPagePresenter = mainPagePresenter;
        _welcomePagePresenter = welcomePagePresenter;
        _appSession = appSession;

        messenger.Register(this);
    }

    public void Receive(ConnectionChangedMessage message)
    {
        switch (message.Value)
        {
            case ConnectionState.Disconnected:
                _view.TogglePage(MainWindow.MainPages.Welcome);
                break;
            case ConnectionState.Connecting:
                // Maybe a Connecting page?
                _view.TogglePage(MainWindow.MainPages.Connecting);
                break;
            case ConnectionState.Connected:
                _view.TogglePage(MainWindow.MainPages.Main);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Attach(MainWindow view)
    {
        _view = view;
        _welcomePagePresenter.Attach(_view.WelcomePage);
        _mainPagePresenter.Attach(_view.MainPage);

        _view.TogglePage(MainWindow.MainPages.Welcome);
    }

    public async Task StartupAsync()
    {
        await _appSession.InitializeAsync();
        _view.Show();
    }
}