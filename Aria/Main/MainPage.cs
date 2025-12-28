using Adw;
using Aria.Features.Browser;
using Aria.Features.Player;
using Aria.Features.PlayerBar;
using GObject;
using Gtk;

namespace Aria.Main;

#pragma warning disable CS0649
[Subclass<Box>]
[Template<AssemblyResource>("Aria.Main.MainPage.ui")]
public partial class MainPage
{
    [Connect("player-bar")] private PlayerBar _playerBar;
    [Connect("browser")] private Browser _browser;
    [Connect("player")] private Player _player;
    [Connect("multi-layout-view")] private MultiLayoutView _multiLayoutView;

    public const string BottomSheetLayoutName = "bottom-sheet-layout";
    
    public PlayerBar PlayerBar => _playerBar;
    
    public Browser Browser => _browser;
    
    public Player Player => _player;
    
    public MultiLayoutView MultiLayoutView => _multiLayoutView;

    partial void Initialize()
    {
        Console.WriteLine("MainPage initialized");
    }
}