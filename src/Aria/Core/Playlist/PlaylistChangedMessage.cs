using CommunityToolkit.Mvvm.Messaging.Messages;
using JetBrains.Annotations;

namespace Aria.Core.Playlist;

[UsedImplicitly]
public sealed class PlaylistChangedMessage(PlaylistStateChangedFlags flags) : ValueChangedMessage<PlaylistStateChangedFlags>(flags);