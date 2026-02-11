using Aria.Core;
using Aria.Core.Queue;
using Aria.Infrastructure;
using Gdk;
using Gio;
using GLib;
using Gtk;

namespace Aria.Features.Browser.Albums;

public partial class AlbumsPage
{
    // Actions
    private SimpleAction _albumShowAction;
    private SimpleAction _enqueueReplaceAction;
    private SimpleAction _enqueueNextAction;
    private SimpleAction _enqueueEndAction;
    
    private void InitializeActions()
    {
        const string group = "album";
        const string showAlbum = "show-album";
        const string enqueueReplace = "enqueue-replace";
        const string enqueueNext = "enqueue-next";
        const string enqueueEnd = "enqueue-end";
        
        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_albumShowAction = SimpleAction.New(showAlbum, null));
        queueActionGroup.AddAction(_enqueueReplaceAction = SimpleAction.New(enqueueReplace, null));
        queueActionGroup.AddAction(_enqueueNextAction = SimpleAction.New(enqueueNext, null));
        queueActionGroup.AddAction(_enqueueEndAction = SimpleAction.New(enqueueEnd, null));
        _albumShowAction.OnActivate += AlbumShowActionOnOnActivate;
        _enqueueReplaceAction.OnActivate += EnqueueReplaceActionOnOnActivate;
        _enqueueNextAction.OnActivate += EnqueueNextActionOnOnActivate;
        _enqueueEndAction.OnActivate += EnqueueEndActionOnOnActivate;
        InsertActionGroup(group, queueActionGroup);

        var defaultAction = IQueue.DefaultEnqueueAction switch
        {
            EnqueueAction.Replace => enqueueReplace,
            EnqueueAction.EnqueueNext => enqueueNext,
            EnqueueAction.EnqueueEnd => enqueueEnd,
            _ => throw new ArgumentOutOfRangeException()
        };

        // This is going to be a problem the moment the user is able to change his default,
        // as in that case we to set another item as default.
        var controller = ShortcutController.NewWithProperties([]);
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Return"), NamedAction.New($"{group}.{showAlbum}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"), NamedAction.New($"{group}.{defaultAction}")));
        AddController(controller);
        
        var menu = Menu.NewWithProperties([]);
        menu.AppendItem(MenuItem.New("Show Album", $"{group}.{showAlbum}"));
        
        var enqueueMenu = Menu.NewWithProperties([]);
        
        var replaceQueueItem = MenuItem.New("Play now (Replace queue)", $"{group}.{enqueueReplace}");
        enqueueMenu.AppendItem(replaceQueueItem);
        
        var playNextItem = MenuItem.New("Play after current track", $"{group}.{enqueueNext}");
        enqueueMenu.AppendItem(playNextItem);        
        
        var playLastItem = MenuItem.New("Add to queue", $"{group}.{enqueueEnd}");
        enqueueMenu.AppendItem(playLastItem);        
        
        menu.AppendSection(null, enqueueMenu);
        
        _albumPopoverMenu.SetMenuModel(menu);        
    }    
 
    private void GestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        // The grid is in single click activate mode.
        // That means that hover changes the selection.
        // The user 'is' able to hover even when the context menu is shown.
        // Therefore, I remember the hovered item at the moment the menu was shown.
        
        // To be honest, this is probably not the 'correct' approach
        // as right-clicking outside an item also invokes this logic.
        
        // But it works, and I have been unable to find out the correct way.
        
        var selected = _singleSelection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        _contextMenuItem = (AlbumsAlbumModel) _listStore.GetObject(selected)!;
        
        var rect = new Rectangle
        {
            X = (int)Math.Round(args.X),
            Y = (int)Math.Round(args.Y),
        };

        _albumPopoverMenu.SetPointingTo(rect);

        if (!_albumPopoverMenu.Visible)
            _albumPopoverMenu.Popup();
    }
    
    private void EnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = _contextMenuItem!.Album.Id.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", argumentArray);
    }

    private void EnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = _contextMenuItem!.Album.Id.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", argumentArray);
    }

    private void EnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = _contextMenuItem!.Album.Id.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", argumentArray);
    }

    private void AlbumShowActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", Variant.NewString(_contextMenuItem!.Album.Id.ToString()));        
    }
}