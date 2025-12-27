using System.Net;
using Aria.Integrations.MPD.Events;
using CodeProject.ObjectPool;
using MpcNET;
using MpcNET.Commands.Reflection;
using MpcNET.Commands.Status;

namespace Aria.Integrations.MPD;

/// <summary>
/// Manages a MPD server session using multiple connections:
/// 1. Status Connection: Dedicated connection for periodic status polling
/// 2. Idle Connection: Dedicated connection using MPD's IDLE command for event-driven updates
/// 3. Command Pool: Pool of connections available for executing MPD commands from the application
/// This architecture ensures status updates don't block command execution and provides both
/// polling-based and event-driven status updates for optimal responsiveness.
/// </summary>
public sealed class MPDSession
{
    private const int ConnectionPoolSize = 5;
    
    private IPEndPoint? _mpdEndpoint;    
    
    private MpcConnection? _statusConnection;

    private System.Timers.Timer? _statusUpdater;
    public MpcConnection?  StatusConnection => _statusConnection;    
    
    /// <summary>
    /// A dedicated connection for MPD that uses the IDLE mechanism.
    /// We will wait for MPD to return from the IDLE command which in turn
    /// tells us something has changed in the server state (e.g., song changed, playlist modified).
    /// This provides event-driven updates complementing the periodic polling of _statusConnection,
    /// </summary>
    private MpcConnection? _idleConnection;

    private CancellationTokenSource _cancelIdle = new();
    
    private CancellationTokenSource _cancelConnect = new();    
    
    /// <summary>
    /// A pool of connections available for the application to send commands to MPD.
    /// Using a pool prevents blocking on the status and idle connections and allows concurrent command execution.
    /// </summary>
    private ObjectPool<PooledObjectWrapper<MpcConnection>>? _connectionPool;    
    
    public event EventHandler? ConnectionChanged;
    
    public event EventHandler<MPDIdleResponseEventArgs>? IdleResponseReceived;

    public event EventHandler<MPDStatusChangedEventArgs>? StatusChanged;

    public bool IsConnected { get; private set; }
    
    public bool IsConnecting { get; private set; }
    
    public MPDCredentials? Credentials { get; set; }
    
    public async Task InitializeAsync()
    {
        IsConnecting = true;
        await Disconnect();
        
        var cancellationToken = _cancelConnect.Token;

        try
        {
            await Connect(cancellationToken);
        }
        catch (Exception e)
        {
            IsConnecting = false;
            ConnectionChanged?.Invoke(this, EventArgs.Empty);            
        }
        
        IsConnecting = false;
    }

    /// <summary>
    /// Sends a command to MPD using a connection from the command pool.
    /// This method retrieves an available connection from _connectionPool, sends the command,
    /// and automatically returns the connection to the pool when done.
    /// </summary>
    public async Task<T> SendCommandAsync<T>(IMpcCommand<T> command)
    {
        try
        {
            // THIS would get from the local connection POOL
            if (_connectionPool != null)
            {
                using var connectionWrapper = await _connectionPool.GetObjectAsync();
                var response = await connectionWrapper.InternalResource.SendAsync(command);
                return !response.IsResponseValid ? throw
                    // TODO: Error handling
                    new InvalidOperationException("Invalid response") : response.Response.Content;
            }
            else
            {
                throw new InvalidOperationException("Connection pool not initialized");
            }
        }
        catch (Exception e)
        {
            // TODO: Handle Error 
            throw;
        }
    }    
    
    private MpcConnection? GetConnection(MPDConnectionType mpdConnectionType)
    {
        // TODO is this method not overthe top
        var connection = mpdConnectionType switch
        {
            MPDConnectionType.Idle => _idleConnection,
            MPDConnectionType.Status => _statusConnection,
            MPDConnectionType.Pool => throw  new InvalidOperationException("Pool connection not supported"),
            _ => throw new ArgumentOutOfRangeException(nameof(mpdConnectionType), mpdConnectionType, null)
        };

        return connection;
    }
    
    private bool _isUpdatingStatus = false;
    
    public async Task UpdateStatusAsync(MPDConnectionType mpdConnectionType)
    {

        var  connection = GetConnection(mpdConnectionType);
        if (connection == null)
        {
            throw new InvalidOperationException("connection not initialized");
        }

        if (_isUpdatingStatus)
        {
            // Already updating status
            return;
        }

        _isUpdatingStatus = true;
        try
        {
            var response = await connection.SendAsync(new StatusCommand());

            if (response is { IsResponseValid: true })
            {
                StatusChanged?.Invoke(this, new MPDStatusChangedEventArgs(response.Response.Content));
            }
            else
            {
                throw new InvalidOperationException("Invalid response");
            }
        }
        catch (Exception e)
        {
            // Something went wrong. Let's reconnect
            // TODO: Reconnect
        }
        finally
        {
            _isUpdatingStatus = false;
        }
    }    
    
    /// <summary>
    /// Establishes all three types of MPD connections: status, idle, and command pool.
    /// Order of initialization:
    /// 1. Status connection - for periodic status polling
    /// 2. Command pool - creates 5 pooled connections for executing commands
    /// 3. Idle connection - for event-driven updates via MPD IDLE command
    /// 4. Status updater - starts both the timer-based polling and idle event loop
    /// All connections are authenticated using the provided credentials if a password is set.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the connection process</param>
    private async Task Connect(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return;
        
        if (Credentials == null) throw new InvalidOperationException("Credentials not set");
        
        if (!IPAddress.TryParse(Credentials.Host, out var ipAddress))
        {
            // TODO: Try to fetch IP address from DNS
            throw new InvalidOperationException("Invalid host");
        }

        _mpdEndpoint = new IPEndPoint(ipAddress, Credentials.Port);
        
        _statusConnection = await OpenConnectionAsync(cancellationToken);
        
        _connectionPool = new ObjectPool<PooledObjectWrapper<MpcConnection>>(ConnectionPoolSize,
            async (poolToken) =>
            {
                var c = await OpenConnectionAsync(poolToken);
                return new PooledObjectWrapper<MpcConnection>(c)
                {
                    // Check our internal global IsConnected status
                    OnValidateObject = (context) => IsConnected && !cancellationToken.IsCancellationRequested,
                    OnReleaseResources = (conn) => conn?.Dispose()
                };
            }
        );

        _idleConnection = await OpenConnectionAsync(_cancelIdle.Token);
        
        InitializeStatusUpdater(_cancelIdle.Token);       

        IsConnecting = false;
        IsConnected = true;
        
        ConnectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Disconnects and cleans up all three connection types.
    /// Shutdown order:
    /// 1. Cancels the idle connection loop (stops IDLE command processing)
    /// 2. Stops and disposes the status update timer
    /// 3. Disconnects the status connection
    /// 4. Cancels any ongoing connection operations
    /// 5. Clears the command pool (implicitly closes all pooled connections)
    /// 6. Nullifies idle and status connection references
    /// This ensures all MPD connections are properly closed and resources are released.
    /// </summary>
    private async Task Disconnect()
    {
        if (IsConnected)
        {
            IsConnected = false;
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }

        // Stop the IDLE connection
        await _cancelIdle.CancelAsync();
        _cancelIdle = new CancellationTokenSource();

        // Stop the status updater
        _statusUpdater?.Stop();
        _statusUpdater?.Dispose();
        if(_statusConnection != null) await _statusConnection.DisconnectAsync();
        
        await _cancelConnect.CancelAsync();
        _cancelConnect = new CancellationTokenSource();

        // Clear the connection pool.
        // This will implicitly close inner connections
        _connectionPool?.Clear();

        _idleConnection = null;
        _statusConnection = null;
    }

    /// <summary>
    /// Initializes the dual status update mechanism combining timer-based polling and event-driven updates.
    /// Creates two concurrent update sources:
    /// 1. Timer-based: A 1-second interval timer that polls status via _statusConnection
    /// 2. Event-driven: A loop that waits on the IDLE command via _idleConnection for immediate server state changes
    /// When IDLE returns (indicating a change), the loop processes the event and immediately re-enters IDLE.
    /// This combination ensures status is both regularly refreshed and immediately updated on changes.
    /// The loop continues until the cancellation token is triggered during disconnect.
    /// </summary>
    /// <param name="cancellationToken">Token to stop the status update loop</param>
    private void InitializeStatusUpdater(CancellationToken cancellationToken = default)
    {
        async Task? StatusLoop()
        {
            while (true)
            {
                if (_statusUpdater?.Enabled != true && _statusConnection is { IsConnected: true })
                {
                    // Update the status every second.
                    _statusUpdater?.Stop();
                    _statusUpdater = new System.Timers.Timer(1000);     
                    _statusUpdater.Elapsed += async (s, e) => await UpdateStatusAsync(MPDConnectionType.Status);
                    _statusUpdater.Start();                    
                }

                try
                {
                    // Run the idleConnection in a wrapper task since MpcNET isn't fully async and will block here
                    if (_idleConnection is null) throw new InvalidOperationException("Idle connection not initialized");
                    var idleChangesTask = Task.Run(async () => await _idleConnection.SendAsync(new IdleCommand("stored_playlist playlist player mixer output options update")));
                    
                    // Wait for the idle command to finish or for the token to be cancelled
                    await Task.WhenAny(idleChangesTask, Task.Delay(-1, cancellationToken));
                    
                    if (cancellationToken.IsCancellationRequested || _idleConnection is not { IsConnected: true })
                    {
                        // Disconnect the idle connection
                        _idleConnection?.DisconnectAsync();
                        break;
                    }
                    
                    // Process the result of the message  from the IDLE connection
                    var message = idleChangesTask.Result;
                    
                    if (message.IsResponseValid)
                        HandleIdleResponseAsync(message.Response.Content);
                    else
                        throw new Exception(message.Response?.Content);                    
                    
                }
                catch (Exception e)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    // TODO: repair the connection.
                    // original code did an initialize again with retry mechanism
                    throw;
                }
            }
        }
        
        Task.Run(StatusLoop, cancellationToken).ConfigureAwait(false);
    }

    private void HandleIdleResponseAsync(string responseContent)
    {
        IdleResponseReceived?.Invoke(this, new MPDIdleResponseEventArgs(responseContent));
    }


    private async Task<MpcConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MpcConnection(_mpdEndpoint);
        await connection.ConnectAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(Credentials?.Password)) return connection;
        
        var response = await connection.SendAsync(new PasswordCommand(Credentials.Password));
        return !response.IsResponseValid
            ? throw new InvalidOperationException("Invalid password")
            : connection;
    }    
}