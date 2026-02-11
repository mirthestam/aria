using Aria.Core;
using Aria.Core.Connection;
using Aria.Features.Shell.Welcome;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Application = Adw.Application;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Shell;

public partial class MainWindowPresenter : IRecipient<ShowToastMessage>
{
    private readonly Application _application;
    private readonly ILogger<MainWindowPresenter> _logger;
    private readonly IAriaControl _ariaControl;
    private readonly MainPagePresenter _mainPagePresenter;
    private readonly WelcomePagePresenter _welcomePagePresenter;

    public MainWindow View { get; private set; }

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

        messenger.Register(this);

        _ariaControl.StateChanged += AriaControlOnStateChanged;
    }

    private async void AriaControlOnStateChanged(object? sender, EngineStateChangedEventArgs e)
    {
        try
        {
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                switch (e.State)
                {
                    case EngineState.Stopped:
                        View.TogglePage(MainWindow.MainPages.Welcome);
                        _ = _welcomePagePresenter.RefreshAsync();
                        break;
                    case EngineState.Starting:
                        View.TogglePage(MainWindow.MainPages.Connecting);
                        break;
                    case EngineState.Seeding:
                        // Ignore seeding state
                        break;
                    case EngineState.Ready:
                        View.TogglePage(MainWindow.MainPages.Main);
                        break;
                    case EngineState.Stopping:
                        // Ignore stopping state
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }
        catch
        {
            // OK
        }
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

        InitializeActions(context);

        View.TogglePage(MainWindow.MainPages.Welcome);
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

    public async void Receive(ShowToastMessage message)
    {
        try
        {
            await ShowToast(message.Message);
        }
        catch
        {
            // OK
        }
    }

    private async Task ShowToast(string message)
    {
        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View.ShowToast(message);
        }).ConfigureAwait(false);
    }

    [LoggerMessage(LogLevel.Critical, "Failed to disconnect.")]
    partial void LogFailedToDisconnect(Exception e);
}