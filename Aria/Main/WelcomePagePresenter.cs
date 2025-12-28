using Adw;

namespace Aria.Main;

public class WelcomePagePresenter
{
    private readonly Application _application;
    private readonly IServiceProvider _serviceProvider;

    private WelcomePage _view;

    public WelcomePagePresenter(Application application, IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
    }

    public void Attach(WelcomePage view)
    {
        _view = view;
    }
}