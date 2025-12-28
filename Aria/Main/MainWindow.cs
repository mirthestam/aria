using Adw;
using GObject;
using Gtk;
using ApplicationWindow = Adw.ApplicationWindow;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Aria.Main;

[Subclass<ApplicationWindow>]
[Template<AssemblyResource>("Aria.Main.MainWindow.ui")]
public partial class MainWindow
{
    public enum MainPages
    {
        Main,
        Welcome,
        Connecting
    }

    private const string MainPageName = "main-stack-page";
    private const string WelcomePageName = "welcome-stack-page";
    private const string ConnectingPageName = "connecting-stack-page";

    private bool _initialized;

    [Connect("main-stack")] private Stack _mainStack;
    [Connect("welcome-page")] private WelcomePage _welcomePage;
    [Connect("connecting-page")] private ConnectingPage _connectingPage;
    [Connect("main-page")] private MainPage _mainPage;

    public MainWindow(MainWindowPresenter presenter) : this()
    {
        presenter.Attach(this);

        // TODO: I do  not like this way of bootstrapping the APP here.
        // the presenters initialize the backend logic. :(
        OnRealize += async (s, e) =>
        {
            try
            {
                await presenter.StartupAsync();
            }
            catch
            {
                // handle
                // EAT :(
            }
        };
    }
    
    public WelcomePage WelcomePage => _welcomePage;
    
    public ConnectingPage ConnectingPage => _connectingPage;
    
    public MainPage MainPage => _mainPage;

    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // TODO: I would like to define this breakpoint in the .UI
        var breakpoint =
            Breakpoint.New(BreakpointCondition.NewLength(BreakpointConditionLengthType.MaxWidth, 620, LengthUnit.Sp));

        // TODO: I also don't like i have to know here about my children
        breakpoint.AddSetter(MainPage.MultiLayoutView, "layout-name", new Value(MainPage.BottomSheetLayoutName));
        AddBreakpoint(breakpoint);
    }

    public void TogglePage(MainPages page)
    {
        var pageName = page switch
        {
            MainPages.Welcome => WelcomePageName,
            MainPages.Connecting => ConnectingPageName,
            MainPages.Main => MainPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        _mainStack.VisibleChildName = pageName;
    }
}