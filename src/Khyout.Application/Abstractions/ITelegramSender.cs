namespace Khyout.Application.Abstractions;

public interface ITelegramSender
{
    /// <summary>Returns true when the message was delivered.</summary>
    Task<bool> SendAsync(string targetRef, string text, CancellationToken cancellationToken = default);
}
