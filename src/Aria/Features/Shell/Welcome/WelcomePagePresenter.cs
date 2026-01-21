using Aria.Core;
using Aria.Core.Connection;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Shell.Welcome;

public partial class WelcomePagePresenter(
    IConnectionProfileProvider connectionProfileProvider,
    IAriaControl ariaControl,
    IMessenger messenger,
    ILogger<WelcomePagePresenter> logger)
{
    private WelcomePage? _view;
    
    public void Attach(WelcomePage view)
    {
        connectionProfileProvider.DiscoveryCompleted += ConnectionProfileProviderOnDiscoveryCompleted;
        
        _view = view;
        
        _view.ConnectAction.OnActivate += ConnectActionOnOnActivate;
        _view.NewAction.OnActivate += NewActionOnOnActivate;
        _view.ConfigureAction.OnActivate += ConfigureActionOnOnActivate;
        
        _ = RefreshConnectionsAsync();
    }

    private void ConfigureActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // TODO: new edit connection
    }

    private async void ConnectActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (args.Parameter == null) throw new InvalidOperationException("Connection ID parameter missing");
            
            // Parse connection identification
            var guidString = args.Parameter.Print(false);
            guidString = guidString[1..^1]; // Remove quotation
            var connectionId = Guid.Parse(guidString);
            
            // Start connection
            await ariaControl.ConnectAsync(connectionId);
            
            // TODO: we should handle password requests, and intercept in the connection chain
            
            // We are connected. Remember this profile by making it persistent
            await connectionProfileProvider.PersistProfileAsync(connectionId);
        }
        catch (Exception e)
        {
            messenger.Send(new ShowToastMessage("Failed to connect"));
            LogFailedToConnectToMPDServer(e);
        }        
    }

    private void NewActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        // TODO: new connection
    }

    private void ConnectionProfileProviderOnDiscoveryCompleted(object? sender, EventArgs e)
    {
        _ = RefreshConnectionsAsync();
    }

    private async Task RefreshConnectionsAsync()
    {
        try
        {
            var connections = await connectionProfileProvider.GetAllProfilesAsync();

            var connectionModels = connections
                .Select(ConnectionModel.FromConnectionProfile);

            GLib.Functions.IdleAdd(0, () =>
            {
                _view?.RefreshConnections(connectionModels);
                return false;
            });            
        }
        catch (Exception e)
        {
            LogFailedToRefreshConnections(e);
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to refresh connections")]
    partial void LogFailedToRefreshConnections(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to connect to MPD server")]
    partial void LogFailedToConnectToMPDServer(Exception e);
}