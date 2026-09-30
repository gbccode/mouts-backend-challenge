using Ambev.DeveloperEvaluation.Domain.Services;
using Xunit;

public class SalePricingTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(3, 300)]
    [InlineData(4, 360)]
    [InlineData(9, 810)]
    [InlineData(10, 800)]
    [InlineData(20, 1600)]
    public void Calculates_total_for_discount_boundaries(int quantity, int expectedTotal)
    {
        var result = SalePricing.Calculate(quantity, 100m);
        Assert.Equal((decimal)expectedTotal, result.Total);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(21)]
    public void Rejects_invalid_quantities(int quantity)
    {
        Assert.Throws<DomainException>(
            () => SalePricing.Calculate(quantity, 100m));
    }
}