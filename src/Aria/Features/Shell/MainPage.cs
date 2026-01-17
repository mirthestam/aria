using Adw;
using Aria.Features.Browser;
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

    public const string BottomSheetLayoutName = "bottom-sheet-layout";
    
    public PlayerBar.PlayerBar PlayerBar => _playerBar;
    
    public BrowserHost BrowserHost => _browserHost;
    
    public Player.Player Player => _player;
    
    public MultiLayoutView MultiLayoutView => _multiLayoutView;
}