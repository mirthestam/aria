using Aria.App.Infrastructure;
using Aria.Core;
using Aria.Features.Browser;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Player;
using Aria.Features.PlayerBar;
using Aria.Hosting;
using Aria.Hosting.Extensions;
using Aria.Infrastructure;
using Aria.Infrastructure.Tagging;
using Aria.Main;
using Aria.MusicServers.MPD;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                x.AddTransient<IBackendConnectionFactory, BackendConnectionFactory>();
                x.AddTransient<IConnectionProfileProvider, ConnectionProfileProvider>();
                x.AddTransient<ITagParser, MPDTagParser>();

                // Main
                x.AddSingleton<MainWindow>();
                x.AddSingleton<MainWindowPresenter>();
                x.AddSingleton<MainPagePresenter>();
                x.AddSingleton<WelcomePagePresenter>();

                // Features - Browser
                x.AddSingleton<BrowserNavigationState>();
                x.AddSingleton<BrowserPresenter>();
                x.AddSingleton<ArtistPagePresenter>();
                x.AddSingleton<ArtistsPagePresenter>();

                // Features - Player
                x.AddSingleton<PlayerPresenter>();

                // Feature - PlayerBar
                x.AddSingleton<PlayerBarPresenter>();
            })
            .UseGtk(a =>
            {
                a.GtkApplicationType = GtkApplicationType.Adw;
                a.ApplicationId = "nl.nuvina.aria";
                a.ApplicationFlags = ApplicationFlags.FlagsNone;

                a.UseWindow<MainWindow>();

                // GObject registrations (GType)
                a.WithMainGTypes();
                a.WithBrowserGTypes();
                a.WithPlayerGTypes();
                a.WithPlayerBarGTypes();
            });
    }
}