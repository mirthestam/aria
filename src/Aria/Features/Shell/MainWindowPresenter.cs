using Adw;
using Aria.Core;
using Aria.Core.Connection;
using Aria.Features.Shell.Welcome;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Application = Adw.Application;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Shell;

public partial class MainWindowPresenter : IRecipient<ConnectionStateChangedMessage>
    , IRecipient<ShowToastMessage>
{
    private readonly Application _application;
    private readonly ILogger<MainWindowPresenter> _logger;
    private readonly IAriaControl _ariaControl;
    private readonly MainPagePresenter _mainPagePresenter;
    private readonly WelcomePagePresenter _welcomePagePresenter;

    public MainWindow View { get; private set; }

    private SimpleAction _aboutAction;
    private SimpleAction _disconnectAction;

    public MainWindowPresenter(IMessenger messenger,
        MainPagePresenter mainPagePresenter,
        WelcomePagePresenter welcomePagePresenter,
        ILogger<MainWindowPresenter> logger,
        Application application,
        IAriaControl ariaControl)
    {
        _application = application;
        _mainPagePresenter = mainPagePresenter;
        _welcomePagePresenter = welcomePagePresenter;
        _ariaControl = ariaControl;
        _logger = logger;

        messenger.Register<ConnectionStateChangedMessage>(this);
        messenger.Register<ShowToastMessage>(this);
    }

    public void Receive(ConnectionStateChangedMessage message)
    {
        GLib.Functions.IdleAdd(0, () =>
        {
            switch (message.Value)
            {
                case ConnectionState.Disconnected:
                    View.TogglePage(MainWindow.MainPages.Welcome);
                    _ = _welcomePagePresenter.RefreshAsync();
                    break;
                case ConnectionState.Connecting:
                    View.TogglePage(MainWindow.MainPages.Connecting);
                    break;
                case ConnectionState.Connected:
                    View.TogglePage(MainWindow.MainPages.Main);
                    break;
            }
            return false;
        });        
    }

    public void Attach(MainWindow view)
    {
        View = view;

        var context = new AttachContext
        {
            InsertAppActionGroup = View.InsertActionGroup,
            SetAccelsForAction = _application.SetAccelsForAction
        };
        
        _welcomePagePresenter.Attach(View.WelcomePage, context);
        _mainPagePresenter.Attach(View.MainPage, context);
        
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(_aboutAction = SimpleAction.New(Accelerators.Window.About.Name, null));
        actionGroup.AddAction(_disconnectAction = SimpleAction.New(Accelerators.Window.Disconnect.Name, null));
        
        context.InsertAppActionGroup(Accelerators.Window.Key, actionGroup);        
        context.SetAccelsForAction($"{Accelerators.Window.Key}.{Accelerators.Window.About.Name}", [Accelerators.Window.About.Accels]);
        context.SetAccelsForAction($"{Accelerators.Window.Key}.{Accelerators.Window.Disconnect.Name}", [Accelerators.Window.Disconnect.Accels]);
        
        _aboutAction.OnActivate += AboutActionOnOnActivate;        
        _disconnectAction.OnActivate += DisconnectActionOnActivate;
        
        View.TogglePage(MainWindow.MainPages.Welcome);
    }
    
    private async void DisconnectActionOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            await _ariaControl.DisconnectAsync();
        }
        catch (Exception e)
        {
            ShowToast("Failed to disconnect. Please restart Aria.");
            LogFailedToDisconnect(e);
        }
        finally
        {
            // Whatever happens; always return to the Welcome page
            View.TogglePage(MainWindow.MainPages.Welcome);
        }
    }

    private void AboutActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var dialog = AboutDialog.NewFromAppdata("/nl/mirthestam/aria/nl.mirthestam.aria.metainfo.xml", null);
        dialog.Present(View);
    }

    public async Task StartupAsync()
    {
        await _ariaControl.InitializeAsync();
        View.Show();
        
        var autoConnected = await _welcomePagePresenter.TryStartAutoConnectAsync();
        if (!autoConnected)
        {
            await _welcomePagePresenter.RefreshAsync();            
        }
    }

    public void Receive(ShowToastMessage message) => ShowToast(message.Message);

    private void ShowToast(string message)
    {
        GLib.Functions.IdleAdd(0, () =>
        {
            View.ShowToast(message);            
            return false;
        });
    }

    [LoggerMessage(LogLevel.Critical, "Failed to disconnect.")]
    partial void LogFailedToDisconnect(Exception e);
}