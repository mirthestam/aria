using GObject;
using Gtk;
using Box = Gtk.Box;

namespace Aria.Features.Browser;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.BrowserHost.ui")]
public partial class BrowserHost
{
    public enum BrowserState
    {
        Browser,
        EmptyCollection
    }
    
    [Connect("browser-state-stack")] private Stack _browserStateStack;
    [Connect("browser-page")] private BrowserPage _browserPage;    
    
    private const string EmptyStatePage = "empty-state-page";
    private const string BrowserStatePage = "browser-state-page";    
    
    public BrowserPage BrowserPage => _browserPage;
    
    public void ToggleState(BrowserState state)
    {
        var pageName = state switch
        {
            BrowserState.Browser => BrowserStatePage,
            BrowserState.EmptyCollection => EmptyStatePage,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        _browserStateStack.VisibleChildName = pageName;
    }


}