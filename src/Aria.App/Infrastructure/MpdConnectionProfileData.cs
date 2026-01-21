namespace Aria.App.Infrastructure;

public class MpdConnectionProfileData : ConnectionProfileData
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 6600;
    public string Password { get; set; } = string.Empty;
}