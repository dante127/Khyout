using Khyout.Domain.Common;

namespace Khyout.Domain.Entities;

/// <summary>Fabric/yarn taxonomy node (two levels in the MVP).</summary>
public class Category
{
    private Category() { } // EF Core

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string NameEn { get; private set; } = null!;

    /// <summary>Null for top-level categories.</summary>
    public Guid? ParentId { get; private set; }

    public Category? Parent { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<Category> Children { get; private set; } = new List<Category>();

    public static Category Create(string slug, string nameAr, string nameEn, Guid? parentId, int sortOrder, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainRuleException("category_slug_required", "Category slug is required.");
        }

        if (string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(nameEn))
        {
            throw new DomainRuleException("category_names_required", "Arabic and English names are required.");
        }

        return new Category
        {
            Id = id ?? Guid.NewGuid(),
            Slug = slug.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = nameEn.Trim(),
            ParentId = parentId,
            SortOrder = sortOrder,
            IsActive = true
        };
    }
}
