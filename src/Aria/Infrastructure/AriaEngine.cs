using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Infrastructure.Connection;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Infrastructure;

public class AriaEngine(
    IEnumerable<IBackendConnectionFactory> integrationProviders,
    IMessenger messenger,
    IConnectionProfileProvider connectionProfileProvider) : IAriaControl, IAria
{
    // The proxy implementations are wrappers around the backend implementations, so we can easily exchange them at runtime, without having the UI to rebind to events.
    private readonly LibraryProxy _libraryProxy = new();
    private readonly PlayerProxy _playerProxy = new();
    private readonly QueueProxy _queueProxy = new();

    private ScopedBackendConnection? _backendScope;

    public IPlayer Player => _playerProxy;
    public IQueue Queue => _queueProxy;
    public ILibrary Library => _libraryProxy;
    
    public async Task InitializeAsync()
    {
        // Forward events from the proxies over the messenger for the UI
        _playerProxy.StateChanged += flags => messenger.Send(new PlayerStateChangedMessage(flags));  
        _libraryProxy.Updated += () => messenger.Send(new LibraryUpdatedMessage());
        _queueProxy.StateChanged += flags => messenger.Send(new QueueStateChangedMessage(flags));
        
        var profile = await connectionProfileProvider.GetDefaultProfileAsync().ConfigureAwait(false);
        if (profile != null) await ConnectAsync(profile).ConfigureAwait(false);
    }
    
    public async Task DisconnectAsync()
    {
        await InternalDisconnectAsync();        
    }

    public Id Parse(string id)
    {
        // We need the ID from the connection to parse it here.
        // This method exists to avoid exposing the entire provider.
        return _backendScope?.Connection.IdProvider.Parse(id) ?? Id.Empty; 
    }

    public async Task ConnectAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
            var profiles = await connectionProfileProvider.GetAllProfilesAsync().ConfigureAwait(false);
            var profile = profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null) throw new InvalidOperationException("No profile found with the given ID");
            await ConnectAsync(profile, cancellationToken).ConfigureAwait(false);
    }
    
    public async Task ConnectAsync(IConnectionProfile connectionProfile, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the backend that is capable of handling this connection profile.
            var provider = integrationProviders.FirstOrDefault(p => p.CanHandle(connectionProfile));
            if (provider == null) throw new NotSupportedException("No provider found for connection profile");

            await InternalDisconnectAsync().ConfigureAwait(false);
            
            // Instantiate the backend and connect our session wrappers to the actual backend implementation  
            _backendScope = await provider.CreateAsync(connectionProfile).ConfigureAwait(false);
            
            var backend = _backendScope.Connection;
            backend.ConnectionStateChanged += BackendOnConnectionStateChanged;

            _playerProxy.Attach(backend.Player);
            _queueProxy.Attach(backend.Queue);
            _libraryProxy.Attach(backend.Library);
            
            //  Initialize the backend. This is where it will connect.
            await backend.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await InternalDisconnectAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task InternalDisconnectAsync()
    {
        if (_backendScope == null) return;

        _playerProxy.Detach();
        _queueProxy.Detach();
        _libraryProxy.Detach();

        var connection = _backendScope.Connection;
        connection.ConnectionStateChanged -= BackendOnConnectionStateChanged;
            
        await connection.DisconnectAsync().ConfigureAwait(false);
        
        _backendScope.Dispose();
        _backendScope = null;
    }
    
    private void BackendOnConnectionStateChanged(ConnectionState state)
    {
        messenger.Send(new ConnectionStateChangedMessage(state));
    }
}