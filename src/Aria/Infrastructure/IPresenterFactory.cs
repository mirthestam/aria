using Aria.Features.Shell;

namespace Aria.Infrastructure;

public interface IPresenterFactory
{
    TPresenter Create<TPresenter>() where TPresenter : IPresenter;
} 