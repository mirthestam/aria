namespace Aria.Hosting.Extensions;

public static class IGTkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public IGtkBuilder UseWindow<TWindow>()
        {
            builder.WindowType = typeof(TWindow);
            return builder;
        }
    }
}