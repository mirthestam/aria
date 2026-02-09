using GObject;
using Gtk;

namespace Aria.Features.Browser.Playlists;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Playlists.PlaylistsEmptyPage.ui")]
public partial class PlaylistsEmptyPage;