using CommunityToolkit.Mvvm.Messaging.Messages;
using JetBrains.Annotations;

namespace Aria.Core.Queue;

[UsedImplicitly]
public sealed class QueueStateChangedMessage(QueueStateChangedFlags flags) : ValueChangedMessage<QueueStateChangedFlags>(flags);