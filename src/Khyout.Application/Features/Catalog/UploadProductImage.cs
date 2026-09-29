using FluentValidation;
using Khyout.Application.Abstractions;
using Khyout.Application.Common;
using Khyout.Application.Common.Cqrs;
using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Khyout.Application.Features.Catalog;

public sealed record UploadProductImageCommand(Guid ProductId, byte[] Content, string ContentType, string FileName)
    : ICommand<ProductImageDto>;

public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    private const int MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };

    public UploadProductImageCommandValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
        RuleFor(c => c.Content).NotEmpty().WithMessage("The uploaded file is empty.");
        RuleFor(c => c.Content).Must(content => content.Length <= MaxBytes)
            .WithMessage("Images must be 5 MB or smaller.");
        RuleFor(c => c.ContentType).Must(type => AllowedTypes.Contains(type.ToLowerInvariant()))
            .WithMessage("Only JPEG, PNG or WebP images are allowed.");
    }
}

public sealed class UploadProductImageCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IImageProcessor imageProcessor,
    IFileStorage fileStorage)
    : ICommandHandler<UploadProductImageCommand, ProductImageDto>
{
    public async Task<ProductImageDto> Handle(UploadProductImageCommand command, CancellationToken cancellationToken)
    {
        var companyId = currentUser.CompanyId
            ?? throw new ForbiddenException("No company is associated with this account.");

        var product = await db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        if (product.SupplierCompanyId != companyId)
        {
            throw new ForbiddenException("Only the product owner can upload images.");
        }

        ProcessedImage processed;
        await using (var stream = new MemoryStream(command.Content, writable: false))
        {
            processed = await imageProcessor.ProcessToWebPAsync(stream, cancellationToken);
        }

        var existing = product.Images.FirstOrDefault(i => i.ContentHash == processed.ContentHash);
        if (existing is not null)
        {
            return ProductImageDto.From(existing);
        }

        var fileName = $"{processed.ContentHash}.webp";
        var storagePath = await fileStorage.SaveAsync(fileName, processed.Bytes, cancellationToken);

        var nextSortOrder = product.Images.Count == 0
            ? (short)0
            : (short)(product.Images.Max(i => i.SortOrder) + 1);

        var image = ProductImage.Create(
            product.Id,
            storagePath,
            processed.ContentHash,
            processed.WidthPx,
            processed.HeightPx,
            processed.Bytes.Length,
            nextSortOrder);

        db.ProductImages.Add(image);
        await db.SaveChangesAsync(cancellationToken);

        return ProductImageDto.From(image);
    }
}
