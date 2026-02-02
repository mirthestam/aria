namespace Aria.Features.Shell;

public interface IPresenter<T>
{
    void Attach(T view, AttachContext context);
    T? View { get; }
}