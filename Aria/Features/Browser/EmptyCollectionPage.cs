using GObject;
using Gtk;

namespace Aria.Features.Browser;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Browser.EmptyCollectionPage.ui")]
public partial class EmptyCollectionPage
{
    partial void Initialize()
    {
        Console.WriteLine("EmptyCollection initialized");
    }
}