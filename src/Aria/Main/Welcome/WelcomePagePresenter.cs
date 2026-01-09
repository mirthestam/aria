using Adw;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Main.Welcome;

public partial class WelcomePagePresenter(
    IConnectionProfileProvider connectionProfileProvider,
    IAppSession _appSession,
    IMessenger messenger,
    ILogger<WelcomePagePresenter> logger)
{
    private WelcomePage _view;

    public void Attach(WelcomePage view)
    {
        connectionProfileProvider.DiscoveryCompleted += ConnectionProfileProviderOnDiscoveryCompleted;
        
        _view = view;
        _view.ConnectionSelected += ViewOnConnectionSelected;
        _ = RefreshConnectionsAsync();
    }

    private void ConnectionProfileProviderOnDiscoveryCompleted(object? sender, EventArgs e)
    {
        _ = RefreshConnectionsAsync();
    }

    private async void ViewOnConnectionSelected(Guid obj)
    {
        try
        {
            await _appSession.ConnectAsync(obj);
        }
        catch (Exception e)
        {
            messenger.Send(new ShowToastMessage("Failed to connect"));
            LogFailedToConnectToMPDServer(e);
        }
    }

    private async Task RefreshConnectionsAsync()
    {
        try
        {
            var connections = await connectionProfileProvider.GetAllProfilesAsync();

            var connectionModels = connections
                .Select(ConnectionModel.FromConnectionProfile);

            _view.RefreshConnections(connectionModels);
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