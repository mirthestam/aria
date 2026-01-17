using Aria.Core;
using Aria.Core.Player;

namespace Aria.Infrastructure;

public abstract class BasePlayer : IPlayer
{
    public virtual Id Id { get; protected set;  } = Id.Empty;
    
    public virtual int? Volume { get; protected set;  }
    
    public virtual bool SupportsVolume => false;
    
    public virtual PlaybackState State  { get; protected set;  } = PlaybackState.Stopped;
    
    public virtual int? XFade => null;
    
    public virtual bool CanXFade => false;
    
    public virtual PlaybackProgress Progress => PlaybackProgress.Default;

    public virtual Task PlayAsync() => Task.CompletedTask;

    public virtual Task PauseAsync() => Task.CompletedTask;

    public virtual Task NextAsync() => Task.CompletedTask;

    public virtual Task PreviousAsync() => Task.CompletedTask;

    public virtual Task StopAsync() => Task.CompletedTask;
}