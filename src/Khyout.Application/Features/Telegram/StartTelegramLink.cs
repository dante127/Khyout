using System.Security.Cryptography;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Telegram;

public sealed record TelegramLinkDto(string DeepLink, DateTimeOffset ExpiresAt);

public sealed record StartTelegramLinkCommand : ICommand<TelegramLinkDto>;

public sealed class StartTelegramLinkCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ITelegramOptions telegramOptions)
    : ICommandHandler<StartTelegramLinkCommand, TelegramLinkDto>
{
    public async Task<TelegramLinkDto> Handle(StartTelegramLinkCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Authentication required.");

        var botUsername = telegramOptions.BotUsername;
        if (string.IsNullOrWhiteSpace(botUsername))
        {
            throw new DomainRuleException(
                "telegram_not_configured",
                "Telegram account linking is not configured on this server.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        user.BeginTelegramLink(code, clock.UtcNow, TimeSpan.FromMinutes(15));
        await db.SaveChangesAsync(cancellationToken);

        var deepLink = $"https://t.me/{botUsername}?start={code}";
        return new TelegramLinkDto(deepLink, user.TelegramLinkCodeExpiresAt!.Value);
    }
}
