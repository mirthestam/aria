using Aria.Core;
using Aria.Core.Connection;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Shell.Welcome;

public sealed record ConnectDialogResult(ConnectDialogOutcome Outcome, IConnectionProfile? Profile);


public partial class WelcomePagePresenter(
    IConnectionProfileProvider connectionProfileProvider,
    IConnectDialogPresenter connectDialogPresenter,
    IConnectionProfileFactory connectionProfileFactory,
    IAriaControl ariaControl,
    IMessenger messenger,
    ILogger<WelcomePagePresenter> logger)
{
    private CancellationTokenSource? _refreshCancellationTokenSource;    
    private WelcomePage? _view;
    
    public void Attach(WelcomePage view)
    {
        connectionProfileProvider.DiscoveryCompleted += ConnectionProfileProviderOnDiscoveryCompleted;
        
        _view = view;
        _view.Discovering = true;
        
        _view.ConnectAction.OnActivate += ConnectActionOnOnActivate;
        _view.NewAction.OnActivate += NewActionOnOnActivate;
        _view.ConfigureAction.OnActivate += ConfigureActionOnOnActivate;
        
    }
    
    private async void ConfigureActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (_view == null) return;

            var connectionId = ParseConnectionId(args);

            var profile = await connectionProfileProvider.GetProfileAsync(connectionId);
            if (profile == null) return;

            var result = await connectDialogPresenter.ShowAsync(_view, profile);

            switch (result.Outcome)
            {
                case ConnectDialogOutcome.Cancelled:
                    return;

                case ConnectDialogOutcome.Forgotten:
                    await connectionProfileProvider.DeleteProfileAsync(profile.Id);
                    await RefreshConnectionsAsync();
                    await DiscoverAsync();
                    return;

                case ConnectDialogOutcome.Confirmed:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            profile = result.Profile ?? throw new InvalidOperationException("Profile should not be null");

            await SaveRefreshConnectPersistAsync(profile);
            await RefreshConnectionsAsync();            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Start disk refresh
        await RefreshConnectionsAsync(cancellationToken);
        
        // Start discovery
        await DiscoverAsync(cancellationToken);        
    }

    private async void ConnectActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var connectionId = ParseConnectionId(args);
            await ConnectAndPersistAsync(connectionId);
        }
        catch (Exception e)
        {
            messenger.Send(new ShowToastMessage("Failed to connect"));
            LogFailedToConnectToMPDServer(e);
        }
    }

    private async void NewActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (_view == null) return;

            var profile = connectionProfileFactory.CreateProfile();

            var result = await connectDialogPresenter.ShowAsync(_view, profile);

            switch (result.Outcome)
            {
                case ConnectDialogOutcome.Cancelled:
                case ConnectDialogOutcome.Forgotten:
                    return;

                case ConnectDialogOutcome.Confirmed:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            profile = result.Profile ?? throw new InvalidOperationException("Profile should not be null");

            await SaveRefreshConnectPersistAsync(profile);
            await RefreshConnectionsAsync();            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void ConnectionProfileProviderOnDiscoveryCompleted(object? sender, EventArgs e)
    {
        GLib.Functions.IdleAdd(0, () =>
        {
            _view?.Discovering = false;
            return false;
        });        
        
        _ = RefreshConnectionsAsync();
    }

    private async Task SaveRefreshConnectPersistAsync(IConnectionProfile profile)
    {
        await connectionProfileProvider.SaveProfileAsync(profile);
        await ConnectAndPersistAsync(profile.Id);
    }

    private async Task ConnectAndPersistAsync(Guid profileId)
    {
        // We can stop refreshing, we are connecting
        AbortRefresh();
        
        await ariaControl.ConnectAsync(profileId);
        await connectionProfileProvider.PersistProfileAsync(profileId);
    }
    
    private void AbortRefresh()
    {
        _refreshCancellationTokenSource?.Cancel();
        _refreshCancellationTokenSource?.Dispose();
        _refreshCancellationTokenSource = null;
    }

    private async Task DiscoverAsync(CancellationToken cancellationToken = default)
    {
        _view?.Discovering = true;
        await connectionProfileProvider.DiscoverAsync(cancellationToken);
        _view?.Discovering = false;        
    }
    
    private async Task RefreshConnectionsAsync(CancellationToken externalCancellationToken = default)
    {
        AbortRefresh();
        
        try
        {
            _refreshCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var cancellationToken = _refreshCancellationTokenSource.Token;
            
            var connections = await connectionProfileProvider.GetAllProfilesAsync(cancellationToken);

            var connectionModels = connections
                .Select(ConnectionModel.FromConnectionProfile);

            GLib.Functions.IdleAdd(0, () =>
            {
                if (cancellationToken.IsCancellationRequested) return false;                
                _view?.RefreshConnections(connectionModels);
                return false;
            });
        }
        catch (Exception e)
        {
            LogFailedToRefreshConnections(e);
        }
    }

    private static Guid ParseConnectionId(SimpleAction.ActivateSignalArgs args)
    {
        if (args.Parameter == null)
            throw new InvalidOperationException("Connection ID parameter missing");

        var guidString = args.Parameter.Print(false);
        guidString = guidString[1..^1]; // Remove quotation
        return Guid.Parse(guidString);
    }
    
    [LoggerMessage(LogLevel.Error, "Failed to refresh connections")]
    partial void LogFailedToRefreshConnections(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to connect to MPD server")]
    partial void LogFailedToConnectToMPDServer(Exception e);
}