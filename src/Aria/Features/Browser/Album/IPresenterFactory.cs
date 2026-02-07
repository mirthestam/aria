using Aria.Features.Shell;

namespace Aria.Features.Browser.Album;

public interface IPresenterFactory
{
    TPresenter Create<TPresenter>() where TPresenter : IPresenter;
} 