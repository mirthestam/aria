using System.ComponentModel;
using Aria.Core;
using Gdk;
using Gio;
using GLib;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Artist;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Artist.AlbumListItem.ui")]
public partial class AlbumListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    
    [Connect("gesture-click")] private GestureClick _gestureClick;
    [Connect("popover-menu")] private PopoverMenu _popoverMenu;    
    
    public AlbumModel? Model { get; private set; }
    
    public void Initialize(AlbumModel model)
    {
        if (Model != null) Model.PropertyChanged -= ModelOnPropertyChanged;
        
        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;

        var artistsLine = string.Join(", ", model.Album.CreditsInfo.AlbumArtists.Select(a => a.Name));

        _titleLabel.SetLabel(model.Album.Title);
        _subTitleLabel.SetLabel(artistsLine);

        _gestureClick.OnPressed += GestureClickOnOnPressed;        
        
        ConfigureContextMenu();        
        SetCoverPicture();
    }
    
    private void ConfigureContextMenu()
    {
        var argument = Variant.NewArray(VariantType.String, [Variant.NewString(Model.Album.Id!.ToString())]);
        
        var menu = new Menu();
        
        var showAlbumForArtistItem = MenuItem.New("Show Album for " + Model.Artist.Name, null);
        var showAlbumForArtistArgument = Variant.NewArray(VariantType.String, [Variant.NewString(Model.Album.Id!.ToString()), Variant.NewString(Model.Artist.Id!.ToString())]);
        showAlbumForArtistItem.SetActionAndTargetValue($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbumForArtist.Action}", showAlbumForArtistArgument);
        menu.AppendItem(showAlbumForArtistItem);
        
        menu.AppendItem(MenuItem.New("Show Album", $"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}::{Model.Album.Id}"));        
        
        var enqueueMenu = new Menu();
        
        var replaceQueueItem = MenuItem.New("Play now (Replace queue)", null);
        replaceQueueItem.SetActionAndTargetValue($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", argument);
        enqueueMenu.AppendItem(replaceQueueItem);
        
        var playNextItem = MenuItem.New("Play after current track", null);
        playNextItem.SetActionAndTargetValue($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", argument);
        enqueueMenu.AppendItem(playNextItem);        
        
        var playLastItem = MenuItem.New("Add to queue", null);
        playLastItem.SetActionAndTargetValue($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", argument);
        enqueueMenu.AppendItem(playLastItem);        
        
        menu.AppendSection(null, enqueueMenu);
        
        _popoverMenu.SetMenuModel(menu);
    }
    
    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AlbumModel.CoverTexture)) return;
        SetCoverPicture();
    }

    private void GestureClickOnOnPressed(GestureClick sender, GestureClick.PressedSignalArgs args)
    {
        var rect = new Rectangle
        {
            X = (int)Math.Round(args.X),
            Y = (int)Math.Round(args.Y),
        };

        _popoverMenu.SetPointingTo(rect);

        if (!_popoverMenu.Visible)
            _popoverMenu.Popup();
    }    
    
    private void SetCoverPicture()
    {
        if (Model?.CoverTexture == null) return;
        _coverPicture.SetPaintable(Model.CoverTexture);
    }
}