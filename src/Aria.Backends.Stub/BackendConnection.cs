using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Playlist;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Backends.Stub;

public class BackendConnection(Player player, Queue queue, IMessenger messenger, Library library) : BaseBackendConnection(player, queue, messenger, library)
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Messenger.Send(new LibraryUpdatedMessage());
        Messenger.Send(new PlayerStateChangedMessage(PlayerStateChangedFlags.All));
        Messenger.Send(new QueueChangedMessage(QueueStateChangedFlags.All));
    }
}