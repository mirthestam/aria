using Adw;
using Aria.Core;
using Aria.Features.Browser;
using Gio;
using GObject;
using Gtk;

namespace Aria.Features.Shell;

#pragma warning disable CS0649
[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Shell.MainPage.ui")]
public partial class MainPage
{
    [Connect("player-bar")] private PlayerBar.PlayerBar _playerBar;
    [Connect("browser")] private BrowserHost _browserHost;
    [Connect("player")] private Player.Player _player;
    [Connect("multi-layout-view")] private MultiLayoutView _multiLayoutView;
    [Connect(BottomSheetLayoutName)]private Layout _bottomSheetLayout;
    [Connect(SidebarLayoutName)]private Layout _sidebarLayout;
    
    partial void Initialize()
    {
        // HACK: Force MultiLayoutView to switch layouts to ensure actions are properly bound.
        // Without this, actions (like NextAction and PrevAction) may not be correctly bound to widgets
        // in layouts that haven't been activated yet. Temporarily switching to the other layout and back
        // ensures both layouts have their action bindings initialized properly.
        var currentLayout = _multiLayoutView.GetLayout();
        if (currentLayout == null) return;
        var otherLayout = currentLayout == _sidebarLayout ? _bottomSheetLayout : _sidebarLayout;
        _multiLayoutView.SetLayout(otherLayout);
        _multiLayoutView.SetLayout(currentLayout);        
    }

    public const string BottomSheetLayoutName = "bottom-sheet-layout";
    public const string SidebarLayoutName = "sidebar-layout";
    
    public PlayerBar.PlayerBar PlayerBar => _playerBar;
    
    public BrowserHost BrowserHost => _browserHost;
    
    public Player.Player Player => _player;
    
    public MultiLayoutView MultiLayoutView => _multiLayoutView;


}