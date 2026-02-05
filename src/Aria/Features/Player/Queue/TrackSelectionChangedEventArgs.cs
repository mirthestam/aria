namespace Aria.Features.Player.Queue;

public class TrackSelectionChangedEventArgs : EventArgs
{
    public uint SelectedIndex { get; }

    public TrackSelectionChangedEventArgs(uint selectedIndex)
    {
        SelectedIndex = selectedIndex;
    }
}