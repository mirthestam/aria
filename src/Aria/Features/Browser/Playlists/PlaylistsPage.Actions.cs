using Aria.Core.Extraction;
using Aria.Infrastructure;
using Gdk;
using Gio;
using Gtk;
using AlertDialog = Adw.AlertDialog;

namespace Aria.Features.Browser.Playlists;

public partial class PlaylistsPage
{
    [Connect("gesture-click")] private GestureClick _gestureClick;
    [Connect("gesture-long-press")] private GestureLongPress _gestureLongPress;
    [Connect("playlist-popover-menu")] private PopoverMenu _playlistPopoverMenu;
    [Connect("confirm-playlist-delete")] private AlertDialog _confirmPlaylistDeleteDialog;
    
    // Actions
    private SimpleAction _showAction;
    private SimpleAction _enqueueDefaultAction;    
    private SimpleAction _enqueueReplaceAction;
    private SimpleAction _enqueueNextAction;
    private SimpleAction _enqueueEndAction;

    private SimpleAction _deleteAction;

    private const string Group = "playlist";
    private const string ActionShowItem = "show";
    private const string ActionDeleteItem = "delete";
    private const string ActionEnqueueDefault = "enqueue-default";        
    private const string ActionEnqueueReplace = "enqueue-replace";
    private const string ActionEnqueueNext = "enqueue-next";
    private const string ActionEnqueueEnd = "enqueue-end";
    
    public event EventHandler<Id>? DeleteRequested;    
    
    private void InitializeActions()
    {
        var itemActionGroup = SimpleActionGroup.New();
        itemActionGroup.AddAction(_showAction = SimpleAction.New(ActionShowItem, null));
        itemActionGroup.AddAction(_enqueueDefaultAction = SimpleAction.New(ActionEnqueueDefault, null));        
        itemActionGroup.AddAction(_enqueueReplaceAction = SimpleAction.New(ActionEnqueueReplace, null));
        itemActionGroup.AddAction(_enqueueNextAction = SimpleAction.New(ActionEnqueueNext, null));
        itemActionGroup.AddAction(_enqueueEndAction = SimpleAction.New(ActionEnqueueEnd, null));
        itemActionGroup.AddAction(_deleteAction = SimpleAction.New(ActionDeleteItem, null));
        
        _showAction.OnActivate += ShowActionOnOnActivate;
        _enqueueDefaultAction.OnActivate += EnqueueDefaultActionOnOnActivate;
        _enqueueReplaceAction.OnActivate += EnqueueReplaceActionOnOnActivate;
        _enqueueNextAction.OnActivate += EnqueueNextActionOnOnActivate;
        _enqueueEndAction.OnActivate += EnqueueEndActionOnOnActivate;
        _deleteAction.OnActivate += DeleteActionOnOnActivate;
        
        _confirmPlaylistDeleteDialog.OnResponse += ConfirmPlaylistDeleteDialogOnOnResponse;        
        
        InsertActionGroup(Group, itemActionGroup);
        
        ConfigureShortcuts();
        CreatePlaylistContextMenu();
    }

    private void DeleteActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        _confirmPlaylistDeleteDialog.Present(this);
    }

    private void ConfirmPlaylistDeleteDialogOnOnResponse(AlertDialog sender, AlertDialog.ResponseSignalArgs args)
    {
        var response = args.Response;
        switch (response)
        {
            case "cancel":
                return;
            
            case "delete":
                break;
        }

        DeleteRequested?.Invoke(this, _contextMenuItem!.Playlist.Id);
    }

    private void EnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = _contextMenuItem!.Playlist.Id.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", argumentArray);        
    }

    private void EnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = _contextMenuItem!.Playlist.Id.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", argumentArray);
    }

    private void EnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = _contextMenuItem!.Playlist.Id.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", argumentArray);
    }

    private void EnqueueDefaultActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = _contextMenuItem!.Playlist.Id.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}", argumentArray);
    }

    private void ShowActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var variant = _contextMenuItem!.Playlist.Id.ToVariant();
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowPlaylist.Action}", variant);
    }

    private void CreatePlaylistContextMenu()
    {
        var menu = Menu.NewWithProperties([]);
        menu.AppendItem(MenuItem.New("Show Playlist", $"{Group}.{ActionShowItem}"));
        
        var enqueueMenu = Menu.NewWithProperties([]);
        var replaceQueueItem = MenuItem.New("Play now (Replace queue)", $"{Group}.{ActionEnqueueReplace}");
        enqueueMenu.AppendItem(replaceQueueItem);
        
        var playNextItem = MenuItem.New("Play after current track", $"{Group}.{ActionEnqueueNext}");
        enqueueMenu.AppendItem(playNextItem);        
        
        var playLastItem = MenuItem.New("Add to queue", $"{Group}.{ActionEnqueueEnd}");
        enqueueMenu.AppendItem(playLastItem);
        
        menu.AppendSection(null, enqueueMenu);        

        var suffixMenu = Menu.NewWithProperties([]);
        
        var deleteItem = MenuItem.New("Delete", $"{Group}.{ActionDeleteItem}");
        suffixMenu.AppendItem(deleteItem);        
        
        menu.AppendSection(null, suffixMenu);
        
        _playlistPopoverMenu.SetMenuModel(menu);
    }

    private void ConfigureShortcuts()
    {
        var controller = ShortcutController.NewWithProperties([]);
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Return"), NamedAction.New($"{Group}.{ActionShowItem}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"), NamedAction.New($"{Group}.{ActionEnqueueDefault}")));
        AddController(controller);
    }

    private void ShowContextMenu(double x, double y)
    {
        // The grid is in single click activate mode.
        // That means that hover changes the selection.
        // The user 'is' able to hover even when the context menu is shown.
        // Therefore, I remember the hovered item at the moment the menu was shown.
        
        // To be honest, this is probably not the 'correct' approach
        // as right-clicking outside an item also invokes this logic.
        
        // But it works, and I have been unable to find out the correct way.
        
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        _contextMenuItem = (PlaylistModel) _listStore.GetObject(selected)!;
        
        var rect = new Rectangle
        {
            X = (int)Math.Round(x),
            Y = (int)Math.Round(y),
        };

        _playlistPopoverMenu.SetPointingTo(rect);

        if (!_playlistPopoverMenu.Visible)
            _playlistPopoverMenu.Popup();        
    }
    
    private void GestureLongPressOnOnPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        ShowContextMenu(args.X, args.Y);
    }
    
    private void GestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        ShowContextMenu(args.X, args.Y);        
    }    
}