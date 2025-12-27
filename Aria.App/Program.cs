using System.Net;
using Aria;
using Aria.App;
using Aria.Integrations;
using Aria.Integrations.MPD;


var application = Adw.Application.New("org.gir.core", Gio.ApplicationFlags.FlagsNone);
application.OnActivate += (sender, args) =>
{
    //var window = MainWindow.New(application);
    var window = new MainWindow();
    
    // The code tutorials do MainWindow.New(application).
    // That would automatically set the application.
    // However, that creates an empty window, whereas my class implements builder.
    // So, my own workaround was to let the MainWindow load itself then set the applcation as below.
    window.SetApplication(application);
    window.Title = "Aria";
    
    window.Show();
};

// Ik denkj dat ik wil dat de WINDOW het optuigen van de integratie doet.
// Immers, die window moet eerst staan, en  ook nog mogelijkheid geven welke integratie moet worden geladen.

// Wire up a quick MPD instance
IIntegration integration = new MPDIntegration();
var mpdIntegration = (MPDIntegration) integration;

integration.SongChanged += (sender, args) =>
{
    Console.WriteLine($"Song changed: {args.NewSongId}");
};

integration.ConnectionChanged += (sender, args) =>
{
    Console.WriteLine($"Connection changed: {integration.IsConnected}");
};

integration.StatusChanged += (sender, args) =>
{
    Console.WriteLine($"{integration.Status.Song.Id} - {integration.Status.Song.Elapsed}  / {integration.Status.Song.Duration} - {integration.Status.Playlist.CurrentSongIndex + 1} / {integration.Status.Playlist.Length} ({integration.Status.Playlist.Id})");
    Console.WriteLine($"{integration.Status.Song.AudioBits}-bit {integration.Status.Song.AudioChannels} ch {integration.Status.Song.AudioSampleRate / 1000.0:F1} kHz {integration.Status.Song.Bitrate}");
    Console.WriteLine($"{integration.Status.Player.Id} - {integration.Status.Player.State} - {integration.Status.Player.Volume}%");
    Console.WriteLine();
};

integration.PlaylistsChanged += (sender, args) =>
{
    Console.WriteLine($"Playlists changed");
};

integration.QueueChanged += (sender, args) =>
{
    Console.WriteLine($"Queue changed");
};

mpdIntegration.SetCredentials(new MPDCredentials("192.168.10.10", 6600, ""));
await mpdIntegration.InitializeAsync();

// TODO: Set up MPRIS integration

return application.RunWithSynchronizationContext(null);