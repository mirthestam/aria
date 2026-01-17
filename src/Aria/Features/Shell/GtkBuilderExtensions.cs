using Aria.Features.Shell.Welcome;
using Aria.Hosting;

namespace Aria.Features.Shell;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithMainGTypes()
        {
            builder.WithGType<ConnectingPage>();
            builder.WithGType<MainPage>();
            builder.WithGType<WelcomePage>();
            builder.WithGType<ConnectionListItem>();
        }
    }
}