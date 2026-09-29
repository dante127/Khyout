using Khyout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khyout.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>Starter taxonomy seeded through migrations (static, deterministic ids).</summary>
    private static readonly Category[] SeedCategories =
    {
        Category.Create("cotton-fabrics",    "أقمشة قطنية",              "Cotton Fabrics",        null, 1,  new Guid("a1000000-0000-4000-8000-000000000001")),
        Category.Create("knitted-fabrics",   "أقمشة تريكو",              "Knitted Fabrics",       null, 2,  new Guid("a1000000-0000-4000-8000-000000000002")),
        Category.Create("denim",             "دنيم",                     "Denim",                 null, 3,  new Guid("a1000000-0000-4000-8000-000000000003")),
        Category.Create("wool-fabrics",      "أقمشة صوفية",              "Wool Fabrics",          null, 4,  new Guid("a1000000-0000-4000-8000-000000000004")),
        Category.Create("synthetic-fabrics", "أقمشة صناعية",             "Synthetic Fabrics",     null, 5,  new Guid("a1000000-0000-4000-8000-000000000005")),
        Category.Create("viscose-rayon",     "فيسكوز وحرير صناعي",       "Viscose & Rayon",       null, 6,  new Guid("a1000000-0000-4000-8000-000000000006")),
        Category.Create("blended-fabrics",   "أقمشة مخلوطة",             "Blended Fabrics",       null, 7,  new Guid("a1000000-0000-4000-8000-000000000007")),
        Category.Create("linen",             "كتان",                     "Linen",                 null, 8,  new Guid("a1000000-0000-4000-8000-000000000008")),
        Category.Create("cotton-yarn",       "خيوط قطنية",               "Cotton Yarn",           null, 9,  new Guid("a1000000-0000-4000-8000-000000000009")),
        Category.Create("synthetic-yarn",    "خيوط صناعية",              "Synthetic Yarn",        null, 10, new Guid("a1000000-0000-4000-8000-00000000000a"))
    };

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        builder.Property(c => c.NameAr).HasColumnName("name_ar").HasMaxLength(200).IsRequired();
        builder.Property(c => c.NameEn).HasColumnName("name_en").HasMaxLength(200).IsRequired();
        builder.Property(c => c.ParentId).HasColumnName("parent_id");
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();

        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("ux_categories_slug");
        builder.HasIndex(c => new { c.ParentId, c.SortOrder }).HasDatabaseName("ix_categories_parent_sort");

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedCategories);
    }
}
