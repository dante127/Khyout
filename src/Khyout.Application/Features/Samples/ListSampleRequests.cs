using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Common;
using Khyout.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Samples;

public sealed record ListSampleRequestsQuery(int PageNumber = 1, int PageSize = 20, string? Status = null)
    : IQuery<PagedResult<SampleDto>>;

public sealed class ListSampleRequestsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<ListSampleRequestsQuery, PagedResult<SampleDto>>
{
    public async Task<PagedResult<SampleDto>> Handle(
        ListSampleRequestsQuery query,
        CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var isAdmin = currentUser.Role == UserRole.Admin;

        var samples = db.SampleRequests
            .AsNoTracking()
            .Include(s => s.BuyerCompany)
            .Include(s => s.SupplierCompany)
            .Include(s => s.Product)
            .AsQueryable();

        if (!isAdmin)
        {
            samples = currentUser.Role == UserRole.Supplier
                ? samples.Where(s => s.SupplierCompanyId == companyId)
                : samples.Where(s => s.BuyerCompanyId == companyId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<SampleStatus>(query.Status, ignoreCase: true, out var status))
            {
                throw new DomainRuleException("sample_status_invalid", $"'{query.Status}' is not a valid sample status.");
            }

            samples = samples.Where(s => s.Status == status);
        }

        var (page, size) = Pagination.Normalize(query.PageNumber, query.PageSize);
        var total = await samples.LongCountAsync(cancellationToken);

        var entities = await samples
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return PagedResult<SampleDto>.Create(entities.Select(SampleDto.From).ToList(), page, size, total);
    }
}
