namespace Aria.Core.Queue;

public class QueueStateChangedEventArgs : EventArgs
{
    public QueueStateChangedFlags Flags { get; }

    public QueueStateChangedEventArgs(QueueStateChangedFlags flags)
    {
        Flags = flags;
    }
}