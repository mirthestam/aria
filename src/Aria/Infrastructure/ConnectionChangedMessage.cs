using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Aria.Infrastructure;

// TODO: Reconsider using infrastructure messages. Prefer having the root presenter bind directly to session events, using messaging only within the UI layer.
public class ConnectionChangedMessage(ConnectionState value) : ValueChangedMessage<ConnectionState>(value);