namespace Aria.Integrations;

public interface IIntegration
{
    event EventHandler<SongChangedEventArgs> SongChanged;
    
    event EventHandler ConnectionChanged;
    
    event EventHandler StatusChanged;
    
    event EventHandler PlaylistsChanged;    
    
    event EventHandler QueueChanged;

    bool IsConnected { get; }

    Task PlayAsync();

    Task PauseAsync();

    Task NextAsync();

    Task PreviousAsync();

    Task StopAsync();

    Status Status { get; }
}