using Aria.Core.Connection;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Shell.Welcome;

[Subclass<Object>]
public partial class ConnectionModel
{
    private ConnectionModel(Guid id, string displayName, string details, bool isDiscovered) : this()
    {
        Id = id;
        DisplayName = displayName;
        Details = details;
        IsDiscovered = isDiscovered;
    }

    public static ConnectionModel FromConnectionProfile(IConnectionProfile profile)
    {
        
        var details = profile.AutoConnect && profile.Flags.HasFlag(ConnectionFlags.Saved) 
            ? "Auto-Connect" 
            : string.Empty;

        return new ConnectionModel(
            profile.Id,
            profile.Name,
            details,
            profile.Flags.HasFlag(ConnectionFlags.Discovered));
    }

    public Guid Id { get; }
    public string DisplayName { get; }
    public string Details { get; }
    public bool IsDiscovered { get; }
}