using Aria.Core;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;

namespace Aria.Infrastructure;

public interface IBackendConnection : IDisposable
{
    IPlayer Player { get; }

    IPlaylist Playlist { get; }

    ILibrary Library { get; }

    Task InitializeAsync();

    Task DisconnectAsync();
}