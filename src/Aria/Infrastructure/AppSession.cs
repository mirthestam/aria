using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;

namespace Aria.Infrastructure;

public class AppSession(
    IEnumerable<IBackendConnectionFactory> integrationProviders,
    IConnectionProfileProvider connectionProfileProvider) : IAppSession
{
    // The Session implementations are wrappers around the backend implementations, so we can easily exchange them at runtime, without having the UI to rebind to events.
    private readonly SessionLibrary _library = new();
    private readonly SessionPlayer _player = new();
    private readonly SessionPlaylist _playlist = new();

    private IBackendConnection? _backend;

    public IPlayer Player => _player;
    public IPlaylist Playlist => _playlist;
    public ILibrary Library => _library;

    public bool IsBackendLoaded => _backend != null;

    public async Task InitializeAsync()
    {
        var profile = await connectionProfileProvider.GetDefaultProfileAsync();
        if (profile != null) await ConnectAsync(profile);
    }

    public async Task ConnectAsync(Guid profileId)
    {
        if (IsBackendLoaded)
        {
            throw new InvalidOperationException("Backend already loaded. Disconnect first.");
        }

        var profiles = await connectionProfileProvider.GetAllProfilesAsync();
        var profile = profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) throw new InvalidOperationException("No profile found with the given ID");
        await ConnectAsync(profile);
    }

    public async Task DisconnectAsync()
    {
        await _backend?.DisconnectAsync();
        _player.Detach();
        _playlist.Detach();
        _library.Detach();
        _backend?.Dispose();
        _backend = null;        
    }

    public async Task ConnectAsync(IConnectionProfile connectionProfile)
    {
        try
        {
            // Find the backend that is capable of handling this connection profile.
            var provider = integrationProviders.FirstOrDefault(p => p.CanHandle(connectionProfile));

            if (provider == null) throw new NotSupportedException("No provider found for connection profile");

            // Unload the active integration
            if (_backend != null)
            {
                _player.Detach();
                _playlist.Detach();
                _library.Detach();

                _backend.Dispose();
                _backend = null;
            }

            // Instantiate the backend and connect our session wrappers to the actual backend implementation  
            _backend = await provider.CreateAsync(connectionProfile);

            _player.Attach(_backend.Player);
            _playlist.Attach(_backend.Playlist);
            _library.Attach(_backend.Library);

            //  Initialize the backend. This is where it will connect.
            await _backend.InitializeAsync();
        }
        catch
        {
            _player.Detach();
            _playlist.Detach();
            _library.Detach();
            _backend?.Dispose();
            _backend = null;
            throw;
        }
    }
}