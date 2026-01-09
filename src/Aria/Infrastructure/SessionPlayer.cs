using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Player;

namespace Aria.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class SessionPlayer : IPlayer
{
    //  Simple decorator because underlying integrations can change
    private IPlayer? _active;

    public Task PlayAsync()
    {
        return _active?.PlayAsync() ?? Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        return _active?.PauseAsync() ?? Task.CompletedTask;
    }

    public Task NextAsync()
    {
        return _active?.NextAsync() ?? Task.CompletedTask;
    }

    public Task PreviousAsync()
    {
        return _active?.PreviousAsync() ?? Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return _active?.StopAsync() ?? Task.CompletedTask;
    }

    public Id Id => _active?.Id ?? null!;

    public bool SupportsVolume => _active?.SupportsVolume ?? false;
    public PlaybackState State => _active?.State ?? PlaybackState.Unknown;
    public int? XFade => _active?.XFade;
    public bool CanXFade => _active?.CanXFade ?? false;
    public int? Volume => _active?.Volume;
    public PlaybackProgress Progress => _active?.Progress ?? new PlaybackProgress();
    public SongInfo? CurrentSong => _active?.CurrentSong;

    internal void Attach(IPlayer player)
    {
        if (_active != null) Detach();
        _active = player;
    }

    internal void Detach()
    {
        _active = null;
    }
}