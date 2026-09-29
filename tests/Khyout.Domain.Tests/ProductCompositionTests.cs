using FluentAssertions;
using Khyout.Domain.Common;
using Khyout.Domain.Entities;
using Khyout.Domain.Enums;
using Xunit;

namespace Khyout.Domain.Tests;

public class ProductCompositionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private static Product NewProduct() =>
        Product.Create(Guid.NewGuid(), Guid.NewGuid(), "Cotton/Lycra jersey", null, 50, UnitOfMeasure.Kg, Now);

    [Fact]
    public void Composition_summing_to_100_is_valid()
    {
        var product = NewProduct();
        product.AddComposition(FiberType.Cotton, 95);
        product.AddComposition(FiberType.Lycra, 5);

        var act = product.EnsureCompositionIsValid;

        act.Should().NotThrow();
        product.HasValidComposition().Should().BeTrue();
    }

    [Fact]
    public void Composition_off_by_more_than_tolerance_is_invalid()
    {
        var product = NewProduct();
        product.AddComposition(FiberType.Cotton, 90);
        product.AddComposition(FiberType.Lycra, 5);

        var act = product.EnsureCompositionIsValid;

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("composition_sum_invalid");
        product.HasValidComposition().Should().BeFalse();
    }

    [Fact]
    public void Composition_within_half_point_tolerance_is_accepted()
    {
        var product = NewProduct();
        product.AddComposition(FiberType.Cotton, 94.7m);
        product.AddComposition(FiberType.Lycra, 5.2m);

        var act = product.EnsureCompositionIsValid;

        act.Should().NotThrow();
    }

    [Fact]
    public void Composition_is_required_before_publication_checks()
    {
        var product = NewProduct();

        var act = product.EnsureCompositionIsValid;

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("composition_required");
    }

    [Fact]
    public void Duplicate_fibers_are_rejected()
    {
        var product = NewProduct();
        product.AddComposition(FiberType.Cotton, 95);

        var act = () => product.AddComposition(FiberType.Cotton, 5);

        act.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("composition_duplicate_fiber");
    }

    [Fact]
    public void Percentage_must_be_between_0_and_100()
    {
        var actZero = () => FabricComposition.Create(Guid.NewGuid(), FiberType.Cotton, 0);
        var actOver = () => FabricComposition.Create(Guid.NewGuid(), FiberType.Cotton, 101);

        actZero.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("composition_percentage_invalid");
        actOver.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("composition_percentage_invalid");
    }

    [Fact]
    public void Technical_attributes_require_positive_gsm_and_width()
    {
        var productId = Guid.NewGuid();

        var actGsm = () => FabricAttributes.Create(productId, 0, 5, WeaveStructure.Plain, 150);
        var actWidth = () => FabricAttributes.Create(productId, 180, 5, WeaveStructure.Plain, 0);

        actGsm.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("fabric_gsm_invalid");
        actWidth.Should().Throw<DomainRuleException>()
            .Which.Code.Should().Be("fabric_width_invalid");
    }
}
