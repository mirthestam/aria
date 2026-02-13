using Aria.Core;
using Aria.Infrastructure;
using Gdk;
using Gio;
using GLib;
using Gtk;

namespace Aria.Features.Player.Queue;

public partial class Queue
{
    // Actions
    private SimpleAction? _queueDeleteSelectionAction;
    private SimpleAction? _queueShowAlbumAction;
    private SimpleAction? _queueShowTrackAction;
    
    private QueueTrackModel? _contextMenuItem;    

    private new void InsertActionGroup(string name, ActionGroup? actionGroup)
    {
        base.InsertActionGroup(name, actionGroup);
        _tracksListView.InsertActionGroup(name, actionGroup);
    }    
    
    private void InitializeQueueActionGroup()
    {
        const string group = "queue";
        const string deleteSelection = "delete-selection";
        const string showAlbum = "show-album";
        const string showTrack = "show-track";
        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_queueDeleteSelectionAction = SimpleAction.New(deleteSelection, null));
        queueActionGroup.AddAction(_queueShowAlbumAction = SimpleAction.New(showAlbum, null));
        queueActionGroup.AddAction(_queueShowTrackAction = SimpleAction.New(showTrack, null));       
        _queueDeleteSelectionAction.OnActivate += QueueDeleteSelectionActionOnOnActivate;
        _queueShowAlbumAction.OnActivate += QueueShowAlbumActionOnOnActivate;
        _queueShowTrackAction.OnActivate += QueueShowTrackActionOnOnActivate;
        InsertActionGroup(group, queueActionGroup);

        var controller = ShortcutController.New();
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Delete"), NamedAction.New($"{group}.{deleteSelection}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Alt>Return"), NamedAction.New($"{group}.{showTrack}")));        
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"), NamedAction.New($"{group}.{showAlbum}")));
        AddController(controller);
        
         var menu = Menu.NewWithProperties([]);
         menu.AppendItem(MenuItem.New("Remove", $"{group}.{deleteSelection}"));
         menu.AppendItem(MenuItem.New("Track Details", $"{group}.{showTrack}"));        
         menu.AppendItem(MenuItem.New("Show Album", $"{group}.{showAlbum}"));        
         
         var playlistSection = Menu.NewWithProperties([]);
         playlistSection.AppendItem(MenuItem.New("Clear", $"{AppActions.Queue.Key}.{AppActions.Queue.Clear.Action}"));
         
         menu.AppendSection(null, playlistSection);
         
         _trackPopoverMenu.SetMenuModel(menu);        
    }

    private void ShowContextMenu(double x, double y)
    {
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        _contextMenuItem = (QueueTrackModel) _listStore.GetObject(selected)!;
        
        var rect = new Rectangle
        {
            X = (int)Math.Round(x),
            Y = (int)Math.Round(y)
        };

        _trackPopoverMenu.SetPointingTo(rect);

        if (!_trackPopoverMenu.Visible)
            _trackPopoverMenu.Popup();        
    }    
    private void TracksListViewOnOnActivate(ListView sender, ListView.ActivateSignalArgs args)
    {
         if (_selection.SelectedItem is not QueueTrackModel selectedModel) return;
        TrackActivated?.Invoke(this, new TrackActivatedEventArgs(selectedModel.Position));
    }

    private void GestureLongPressOnOnPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        ShowContextMenu(args.X, args.Y);
    }

    private void GestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        ShowContextMenu(args.X, args.Y);
    }
    
    private void QueueShowTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        var item = (QueueTrackModel) _listStore.GetObject(selected)!;

        // Just invoke the global action as we have one.
        // There is no need for our queue presenter to handle this
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowTrack.Action}", Variant.NewString(item.TrackId.ToString()));
    }

    private void QueueShowAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        var item = (QueueTrackModel) _listStore.GetObject(selected)!;

        // Just invoke the global action as we have one.
        // There is no need for our queue presenter to handle this
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", Variant.NewString(item.AlbumId.ToString()));        
    }

    private void QueueDeleteSelectionActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        var item = (QueueTrackModel) _listStore.GetObject(selected)!;
        
        // Proactively remove this track from the list.
        // After the server has confirmed this, it would update the list.
        // If this failed, it would still be in the list and would re-appear.
        _listStore.Remove(selected);
        
        // Just invoke the global action as we have one.
        // There is no need for our queue presenter to handle this
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.RemoveTrack.Action}", Variant.NewString(item.QueueTrackId.ToString()));
    }
}