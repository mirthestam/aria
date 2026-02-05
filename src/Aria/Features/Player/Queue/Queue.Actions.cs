using Aria.Core;
using Aria.Infrastructure;
using Gio;
using GLib;
using Gtk;

namespace Aria.Features.Player.Queue;

public partial class Queue
{
    // Actions
    private SimpleAction? _queueDeleteSelectionAction;
    private SimpleAction? _queueShowAlbumAction;

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
        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_queueDeleteSelectionAction = SimpleAction.New(deleteSelection, null));
        queueActionGroup.AddAction(_queueShowAlbumAction = SimpleAction.New(showAlbum, null));
        _queueDeleteSelectionAction.OnActivate += QueueDeleteSelectionActionOnOnActivate;
        _queueShowAlbumAction.OnActivate += QueueShowAlbumActionOnOnActivate;
        InsertActionGroup(group, queueActionGroup);
        
        var controller = new ShortcutController();
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Delete"), NamedAction.New($"{group}.{deleteSelection}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"), NamedAction.New($"{group}.{showAlbum}")));
        AddController(controller);
    }    
    
    private void QueueShowAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var selected = _tracksSelection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        var item = (QueueTrackModel) _tracksListStore.GetObject(selected)!;

        // Just invoke the global action as we have one.
        // There is no need for our queue presenter to handle this
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", Variant.NewString(item.AlbumId.ToString()));        
    }

    private void QueueDeleteSelectionActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var selected = _tracksSelection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        var item = (QueueTrackModel) _tracksListStore.GetObject(selected)!;
        
        // Proactively remove this track from the list.
        // After the server has confirmed this, it would update the list.
        // If this failed, it would still be in the list and would re-appear.
        _tracksListStore.Remove(selected);
        
        // Just invoke the global action as we have one.
        // There is no need for our queue presenter to handle this
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.RemoveTrack.Action}", Variant.NewString(item.QueueTrackId.ToString()));
    }
}