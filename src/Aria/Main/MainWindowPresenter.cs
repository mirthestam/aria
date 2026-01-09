using Adw;
using Aria.Infrastructure;
using Aria.Main.Welcome;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Application = Adw.Application;
using Task = System.Threading.Tasks.Task;

namespace Aria.Main;

public partial class MainWindowPresenter : IRecipient<ConnectionChangedMessage>
    , IRecipient<ShowToastMessage>
{
    private readonly Application _application;
    private readonly ILogger<MainWindowPresenter> _logger;
    private readonly IAppSession _appSession;
    private readonly MainPagePresenter _mainPagePresenter;
    private readonly WelcomePagePresenter _welcomePagePresenter;
    private MainWindow _view;

    private SimpleAction _aboutAction;
    private SimpleAction _disconnectAction;

    public MainWindowPresenter(IMessenger messenger,
        MainPagePresenter mainPagePresenter,
        WelcomePagePresenter welcomePagePresenter,
        ILogger<MainWindowPresenter> logger,
        Application application,
        IAppSession appSession)
    {
        _application = application;
        _mainPagePresenter = mainPagePresenter;
        _welcomePagePresenter = welcomePagePresenter;
        _appSession = appSession;
        _logger = logger;

        messenger.Register<ConnectionChangedMessage>(this);
        messenger.Register<ShowToastMessage>(this);
    }

    public void Receive(ConnectionChangedMessage message)
    {
        switch (message.Value)
        {
            case ConnectionState.Disconnected:
                _view.TogglePage(MainWindow.MainPages.Welcome);
                break;
            case ConnectionState.Connecting:
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
        
         _application.SetAccelsForAction("win.disconnect", ["<Control>d"]);
         
        _view = view;
        _welcomePagePresenter.Attach(_view.WelcomePage);
        _mainPagePresenter.Attach(_view.MainPage);
        
        var actionGroup = SimpleActionGroup.New();
        
        _aboutAction = SimpleAction.New("about", null);
        _aboutAction.OnActivate += AboutActionOnOnActivate;
        actionGroup.AddAction(_aboutAction);
        
        // TODO: This action state should be updated based on whether we are connected or not.
        _disconnectAction = SimpleAction.New("disconnect", null);
        _disconnectAction.OnActivate += DisconnectActionOnActivate;
        actionGroup.AddAction(_disconnectAction);

        _view.InsertActionGroup("win", actionGroup);
        _view.TogglePage(MainWindow.MainPages.Welcome);
    }

    private async void DisconnectActionOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _appSession.DisconnectAsync();
        }
        catch (Exception e)
        {
            ShowToast("Failed to disconnect. Please restart Aria.");
            LogFailedToDisconnect(e);
        }
    }

    private void AboutActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var dialog = AboutDialog.NewFromAppdata("/nl/mirthestam/aria/nl.mirthestam.aria.metainfo.xml", null);
        dialog.Present(_view);
    }

    public async Task StartupAsync()
    {
        await _appSession.InitializeAsync();
        _view.Show();
    }

    public void Receive(ShowToastMessage message) => ShowToast(message.Message);

    private void ShowToast(string message) => _view.ShowToast(message);
    
    [LoggerMessage(LogLevel.Critical, "Failed to disconnect.")]
    partial void LogFailedToDisconnect(Exception e);
}