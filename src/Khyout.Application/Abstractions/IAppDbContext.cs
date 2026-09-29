using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Abstractions;

/// <summary>Persistence abstraction used by the application layer.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Company> Companies { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<FabricAttributes> FabricAttributes { get; }
    DbSet<FabricComposition> FabricCompositions { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<RfqRequest> RfqRequests { get; }
    DbSet<RfqQuotation> RfqQuotations { get; }
    DbSet<SampleRequest> SampleRequests { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
