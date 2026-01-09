using CommunityToolkit.Mvvm.Messaging.Messages;
using JetBrains.Annotations;

namespace Aria.Core.Player;

[UsedImplicitly]
public sealed class PlayerStateChangedMessage(PlayerStateChangedFlags flags) : ValueChangedMessage<PlayerStateChangedFlags>(flags);