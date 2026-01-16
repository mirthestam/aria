using CommunityToolkit.Mvvm.Messaging.Messages;
using JetBrains.Annotations;

namespace Aria.Core.Playlist;

[UsedImplicitly]
public sealed class QueueChangedMessage(QueueStateChangedFlags flags) : ValueChangedMessage<QueueStateChangedFlags>(flags);