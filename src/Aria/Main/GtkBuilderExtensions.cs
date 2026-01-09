using Aria.Hosting;

namespace Aria.Main;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithMainGTypes()
        {
            builder.WithGType<ConnectingPage>();
            builder.WithGType<MainPage>();
            builder.WithGType<Welcome.WelcomePage>();
            builder.WithGType<Welcome.ConnectionListItem>();
        }
    }
}