using GObject;
using Gtk;

namespace Aria.Features.Browser;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.BrowserEmptyPage.ui")]
public partial class BrowserEmptyPage;