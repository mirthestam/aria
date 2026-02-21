using Aria.Core.Queue;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Aria.Core.Library;

/// <summary>
/// Indicates that the library has been updated on the backend
/// </summary>
public class LibraryUpdatedMessage(LibraryChangedFlags flags) : ValueChangedMessage<LibraryChangedFlags>(flags);