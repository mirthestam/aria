using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Aria.Core.Connection;

// TODO: Reconsider using infrastructure messages. Prefer having the root presenter bind directly to session events, using messaging only within the UI layer.
public class ConnectionStateChangedMessage(ConnectionState value) : ValueChangedMessage<ConnectionState>(value);