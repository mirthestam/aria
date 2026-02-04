using Aria.App.Infrastructure;
using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.UI;
using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Features.Browser;
using Aria.Features.Browser.Album;
using Aria.Features.Browser.Albums;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Search;
using Aria.Features.Player;
using Aria.Features.Player.Queue;
using Aria.Features.PlayerBar;
using Aria.Features.Shared;
using Aria.Features.Shell;
using Aria.Features.Shell.Welcome;
using Aria.Hosting;
using Aria.Hosting.Extensions;
using Aria.Infrastructure;
using Aria.Infrastructure.Extraction;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MainWindow = Aria.Features.Shell.MainWindow;
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
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.SingleLine = true;
                    options.IncludeScopes = false;                    
                });
            })
            .ConfigureServices(x =>
            {
                // Messaging
                x.AddSingleton<IMessenger>(_ => WeakReferenceMessenger.Default);

                // Infrastructure
                x.AddSingleton<DiskConnectionProfileSource>();
                x.AddSingleton<AriaEngine>();
                x.AddSingleton<IAriaControl>(sp => sp.GetRequiredService<AriaEngine>());
                x.AddSingleton<IAria>(sp => sp.GetRequiredService<AriaEngine>());
                x.AddSingleton<ILibrary>(sp => sp.GetRequiredService<IAria>().Library);
                
                x.AddSingleton<IConnectionProfileProvider, ConnectionProfileProvider>();
                x.AddSingleton<ResourceTextureLoader>();
                x.AddTransient<ITagParser, PicardTagParser>();
                
                // Main
                x.AddSingleton<MainWindow>();
                x.AddSingleton<MainWindowPresenter>();
                x.AddSingleton<MainPagePresenter>();
                x.AddSingleton<WelcomePagePresenter>();

                // Features - Browser
                x.AddSingleton<IAlbumPagePresenterFactory, PresenterFactory>();
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
                x.AddSingleton<QueuePresenter>();

                // Feature - PlayerBar
                x.AddSingleton<PlayerBarPresenter>();
                
                // MPD
                x.AddSingleton<IBackendConnectionFactory, Backends.MPD.Connection.BackendConnectionFactory>();
                x.AddSingleton<IConnectionProfileFactory, ConnectionProfileFactory>();
                x.AddSingleton<IConnectDialogPresenter, Backends.MPD.UI.Connect.ConnectDialogPresenter>();                
                x.AddTransient<Backends.MPD.Connection.BackendConnection>();
                x.AddScoped<Backends.MPD.Queue>();
                x.AddScoped<Backends.MPD.Library>();
                x.AddScoped<Backends.MPD.Connection.Client>();
                x.AddScoped<Aria.Backends.MPD.Player>();
                x.AddScoped<IIdProvider, Backends.MPD.Extraction.IdProvider>();
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
                a.WithSharedGTypes();
                a.WithMPDGTypes();
            });
    }
}