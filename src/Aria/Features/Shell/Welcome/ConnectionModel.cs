using Aria.Core.Connection;
using GObject;
using Object = GObject.Object;

namespace Aria.Features.Shell.Welcome;

[Subclass<Object>]
public partial class ConnectionModel
{
    private ConnectionModel(Guid id, string displayName, string connectionText, bool isDiscovered) : this()
    {
        Id = id;
        DisplayName = displayName;
        ConnectionText = connectionText;
        IsDiscovered = isDiscovered;
    }

    public static ConnectionModel FromConnectionProfile(IConnectionProfile profile)
    {
        return new ConnectionModel(profile.Id, profile.Name, profile.ConnectionDisplayString, profile.Flags.HasFlag(ConnectionFlags.Discovered));
    }
    
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public string ConnectionText { get; set; }
    public bool IsDiscovered { get; set; }
}