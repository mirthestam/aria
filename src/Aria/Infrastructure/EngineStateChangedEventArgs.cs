using Aria.Core.Connection;

namespace Aria.Infrastructure;

public class EngineStateChangedEventArgs(EngineState state) : EventArgs
{
    public EngineState State { get; } = state;
}