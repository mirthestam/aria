using Aria.App.Infrastructure;
using Aria.Core;
using Aria.Core.Library;
using Aria.Features.Browser;
using Aria.Features.Browser.Album;
using Aria.Features.Browser.Albums;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Search;
using Aria.Features.Player;
using Aria.Features.Player.Playlist;
using Aria.Features.PlayerBar;
using Aria.Hosting;
using Aria.Hosting.Extensions;
using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using Aria.Main;
using Aria.Main.Welcome;
using Aria.MusicServers.MPD;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BackendConnection = Aria.MusicServers.MPD.BackendConnection;
using Task = System.Threading.Tasks.Task;

namespace Aria.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = CreateHostBuilder(args);
        var host = builder.Build();
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            //.UseWolverine()
            .ConfigureServices(x =>
            {
                // Messaging
                x.AddSingleton<IMessenger>(_ => WeakReferenceMessenger.Default);

                // Infrastructure
                x.AddSingleton<AppSession>();
                x.AddSingleton<IAppSession>(sp => sp.GetRequiredService<AppSession>());
                x.AddSingleton<IPlaybackApi>(sp => sp.GetRequiredService<AppSession>());
                x.AddSingleton<ILibrary>(sp => sp.GetRequiredService<IPlaybackApi>().Library);
                
                x.AddSingleton<IConnectionProfileProvider, ConnectionProfileProvider>();
                x.AddTransient<ITagParser, MPDTagParser>();
                x.AddTransient<IIdFactory, IdFactory>();// TODO: Have the connectionFactory handle this
                x.AddSingleton<ResourceTextureLoader>();

                // Main
                x.AddSingleton<MainWindow>();
                x.AddSingleton<MainWindowPresenter>();
                x.AddSingleton<MainPagePresenter>();
                x.AddSingleton<WelcomePagePresenter>();

                // Features - Browser
                x.AddTransient<IAlbumPagePresenterFactory, PresenterFactory>();
                x.AddSingleton<AlbumPagePresenter>();
                x.AddSingleton<AlbumsPagePresenter>();                
                x.AddSingleton<ArtistPagePresenter>();
                x.AddSingleton<ArtistsPagePresenter>();                
                x.AddSingleton<BrowserHostPresenter>();
                x.AddSingleton<BrowserNavigationState>();                
                x.AddSingleton<BrowserPagePresenter>();
                x.AddSingleton<SearchPagePresenter>();

                // Features - Player
                x.AddSingleton<PlayerPresenter>();
                x.AddSingleton<PlaylistPresenter>();

                // Feature - PlayerBar
                x.AddSingleton<PlayerBarPresenter>();
                
                // MPD
                x.AddTransient<IBackendConnectionFactory, BackendConnectionFactory>();                
                x.AddSingleton<BackendConnection>();
                x.AddSingleton<Queue>();
                x.AddSingleton<Library>();
                x.AddSingleton<Session>();
                x.AddSingleton<MusicServers.MPD.Player>();
                
                // Stub
                x.AddTransient<IBackendConnectionFactory, Aria.Backends.Stub.BackendConnectionFactory>();
                x.AddSingleton<Aria.Backends.Stub.BackendConnection>();
                x.AddSingleton<Aria.Backends.Stub.Library>();
                x.AddSingleton<Aria.Backends.Stub.Player>();
                x.AddSingleton<Aria.Backends.Stub.Queue>();
            })
            .UseGtk(a =>
            {
                a.GtkApplicationType = GtkApplicationType.Adw;
                a.ApplicationId = "nl.mirthestam.aria";
                a.ApplicationFlags = ApplicationFlags.FlagsNone;

                a.UseWindow<MainWindow>();

                a.WithResource("nl.mirthestam.aria.gresource");

                // GObject registrations (GType)
                a.WithMainGTypes();
                a.WithBrowserGTypes();
                a.WithPlayerGTypes();
                a.WithPlayerBarGTypes();
            });
    }
}