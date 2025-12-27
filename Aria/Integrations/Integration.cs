using System.Diagnostics.CodeAnalysis;

namespace Aria.Integrations;

[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")] // This is a library to write integrations, so it makes  sense not all virtuals  are overridden  in this assembly
public abstract class Integration : IIntegration
{
    public event EventHandler<SongChangedEventArgs>? SongChanged;

    public event EventHandler? PlaylistsChanged;

    public event EventHandler? QueueChanged;

    public event EventHandler? ConnectionChanged;

    public event EventHandler? StatusChanged;
    
    public virtual bool IsConnected => false;
    
    public virtual Task PlayAsync()
    {
        throw new NotSupportedException();
    }

    public virtual Task PauseAsync()
    {
        throw new NotSupportedException();
    }

    public virtual Task NextAsync()
    {
        throw new NotSupportedException();
    }

    public virtual Task PreviousAsync()
    {
        throw new NotSupportedException();
    }

    public virtual Task StopAsync()
    {
        throw new NotSupportedException();
    }

    public Status Status
    {
        get;
        internal set;
    } = Status.Empty;
    
    protected virtual void OnSongChanged(SongChangedEventArgs e)
    {
        SongChanged?.Invoke(this, e);
    }

    protected virtual void OnPlaylistsChanged(EventArgs e)
    {
        PlaylistsChanged?.Invoke(this, e);
    }

    protected virtual void OnQueueChanged(EventArgs e)
    {
        QueueChanged?.Invoke(this, e);
    }

    protected virtual void OnConnectionChanged(EventArgs e)
    {
        ConnectionChanged?.Invoke(this, e);
    }

    protected virtual void OnStatusChanged(EventArgs e)
    {
        StatusChanged?.Invoke(this, e);
    }
}