using Aria.Core;

namespace Aria.Infrastructure;

// TODO The AppSession is currently implementing the PlayBack API. I want to review this decision
public interface IAppSession : IPlaybackApi
{
    public Task InitializeAsync();
}