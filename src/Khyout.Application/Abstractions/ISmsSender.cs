namespace Khyout.Application.Abstractions;

public interface ISmsSender
{
    /// <summary>Dev note: the Phase 3 implementation logs the message; a real gateway arrives later.</summary>
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
