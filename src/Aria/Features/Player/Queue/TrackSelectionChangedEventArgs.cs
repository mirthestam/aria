namespace Aria.Features.Player.Queue;

public class TrackSelectionChangedEventArgs(uint selectedIndex) : EventArgs
{
    public uint SelectedIndex { get; } = selectedIndex;
}