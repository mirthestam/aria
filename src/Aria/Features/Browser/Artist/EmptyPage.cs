using GObject;
using Gtk;

namespace Aria.Features.Browser.Artist;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.Artist.EmptyPage.ui")]
public partial class EmptyPage;