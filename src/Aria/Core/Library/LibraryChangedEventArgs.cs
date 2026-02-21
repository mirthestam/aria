using Aria.Core.Library;

namespace Aria.Core.Queue;

public class LibraryChangedEventArgs(LibraryChangedFlags flags) : EventArgs
{
    public LibraryChangedFlags Flags { get; } = flags;
}