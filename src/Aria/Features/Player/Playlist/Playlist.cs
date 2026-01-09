using System.ComponentModel.Design.Serialization;
using Aria.Core;
using Aria.Core.Library;
using GObject;
using Gtk;
using ListStore = Gio.ListStore;

namespace Aria.Features.Player.Playlist;

[Subclass<Stack>]
[Template<AssemblyResource>("Aria.Features.Player.Playlist.Playlist.ui")]
public partial class Playlist
{
    public enum PlaylistPages
    {
        Songs,
        Empty
    }
    
    private const string EmptyPageName = "empty-playlist-page";
    private const string SongsPageName = "playlist-page";    
    
    private bool _initialized;
    
    [Connect("songs-list-view")] private ListView _songsListView;
    
    private ListStore _songsListStore;
    private SingleSelection _songsSelection;
    private SignalListItemFactory _signalListItemFactory;

    public void TogglePage(PlaylistPages page)
    {
        var pageName = page switch
        {
            PlaylistPages.Songs => SongsPageName,
            PlaylistPages.Empty => EmptyPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        SetVisibleChildName(pageName);
    }
    
    partial void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        TogglePage(PlaylistPages.Empty);
        
        _signalListItemFactory = new SignalListItemFactory();
        _signalListItemFactory.OnSetup += (_, args) =>
        {
            var item = (ListItem)args.Object;
            item.SetChild(new SongListItem());
        };
        _signalListItemFactory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;
            var modelItem = (SongModel)listItem.GetItem()!;
            var widget = (SongListItem)listItem.GetChild()!;
            widget.Update(modelItem);
        };
        
        _songsListStore = ListStore.New(SongModel.GetGType());
        _songsSelection = SingleSelection.New(_songsListStore);
        _songsListView.SetFactory(_signalListItemFactory);
        _songsListView.SetModel(_songsSelection);        
        
        // TODO: Add context menus to songs.
        // Should provide access to  playlist functions as well as quick navigation to the artists.
    }

    public void RefreshSongs(IEnumerable<SongInfo> songs)
    {
        _songsListStore.RemoveAll();
        
        foreach  (var song in songs)
        {
            var titleText = song?.Title ?? "Unnamed song";
            if (song?.Work?.ShowMovement ?? false)
                // For  these kind of works, we ignore the
                titleText = $"{song.Work.MovementName} ({song.Work.MovementNumber} {song.Title} ({song.Work.Work})";


            var credits = song?.CreditsInfo;
            var subTitleText = "";
            var composers = "";

            if (credits != null)
            {
                var artists = string.Join(", ", credits.OtherArtists.Select(x => x.Artist.Name));

                var details = new List<string>();
                var conductors = string.Join(", ", credits.Conductors.Select(x => x.Artist.Name));
                if (!string.IsNullOrEmpty(conductors))
                    details.Add($"{conductors}");

                composers = string.Join(", ", credits.Composers.Select(x => x.Artist.Name));
                
                subTitleText = artists;
                if (details.Count > 0) subTitleText += $" ({string.Join(", ", details)})";
            }
            
            var item = new SongModel(song?.Id ?? Id.Empty, titleText, subTitleText, composers, song?.Duration ?? TimeSpan.Zero);
            _songsListStore.Append(item);
        }
    }
}