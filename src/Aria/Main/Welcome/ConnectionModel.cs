using Aria.Core;
using Aria.Infrastructure;
using GObject;
using Object = GObject.Object;

namespace Aria.Main.Welcome;

[Subclass<Object>]
public partial class ConnectionModel
{
    private ConnectionModel(Guid id, string displayName, string connectionText) : this()
    {
        Id = id;
        DisplayName = displayName;
        ConnectionText = connectionText;
    }

    public static ConnectionModel FromConnectionProfile(IConnectionProfile profile)
    {
        return new ConnectionModel(profile.Id, profile.Name, profile.ConnectionDisplayString);
    }
    
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public string ConnectionText { get; set; }
}