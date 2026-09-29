using FluentAssertions;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Xunit;

namespace Khyout.Domain.Tests;

public class OutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private static OutboxMessage NewMessage() =>
        OutboxMessage.Create(OutboxMessageType.RfqCreated, "{\"rfqId\":\"...\"}", "user-1", Now);

    [Fact]
    public void Mark_sent_stamps_the_delivery_time()
    {
        var message = NewMessage();

        message.MarkSent(Now.AddMinutes(1));

        message.Status.Should().Be(OutboxMessageStatus.Sent);
        message.SentAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void Failures_increment_attempts_until_dead_letter()
    {
        var message = NewMessage();

        for (var i = 1; i <= OutboxMessage.MaxAttempts; i++)
        {
            message.RegisterFailure("telegram unreachable", Now.AddMinutes(i));
        }

        message.Attempts.Should().Be(OutboxMessage.MaxAttempts);
        message.Status.Should().Be(OutboxMessageStatus.Failed);
        message.LastError.Should().Be("telegram unreachable");
    }
}
