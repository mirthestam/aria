namespace Aria.Main;

public record  ShowToastMessage(string Message)
{
    private readonly string _message = Message;

    public string Message
    {
        get => _message;
        init => _message = value;
    }
}