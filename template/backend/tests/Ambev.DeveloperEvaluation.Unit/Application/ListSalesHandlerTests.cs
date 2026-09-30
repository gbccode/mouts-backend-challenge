using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using NSubstitute;
using Xunit;

public class ListSalesHandlerTests
{
    [Fact]
    public async Task Maps_items_totals_and_pagination_from_the_repository()
    {
        var sale = Sale.Create(DateTime.UtcNow, Guid.NewGuid(), "Customer", Guid.NewGuid(), "Branch");
        sale.AddItem(Guid.NewGuid(), "Product", 10, 100m);
        var repository = Substitute.For<ISaleRepository>();
        var query = new ListSalesQuery(2, 10, "Customer", "totalAmount desc", false);
        repository.GetPageAsync(2, 10, "Customer", "totalAmount desc", false, Arg.Any<CancellationToken>())
            .Returns(new SalesPageResult(new[] { sale }, 11, 2, 2));
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<SaleMappingProfile>()).CreateMapper();
        var handler = new ListSalesHandler(repository, mapper, new ListSalesValidator());

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(11, result.TotalCount);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(2, result.TotalPages);
        var returned = Assert.Single(result.Data);
        Assert.Equal(800m, returned.TotalAmount);
        Assert.Equal(0.20m, Assert.Single(returned.Items).DiscountRate);
    }

    [Theory]
    [InlineData(0, 10, "saleDate desc")]
    [InlineData(1, 0, "saleDate desc")]
    [InlineData(1, 101, "saleDate desc")]
    [InlineData(1, 10, "unknown desc")]
    [InlineData(1, 10, "saleDate sideways")]
    [InlineData(1, 10, "saleDate desc; DROP TABLE Sales")]
    public async Task Rejects_invalid_queries_before_reading_the_repository(int page, int size, string order)
    {
        var repository = Substitute.For<ISaleRepository>();
        var handler = new ListSalesHandler(repository, Substitute.For<IMapper>(), new ListSalesValidator());
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ListSalesQuery(page, size, Order: order), CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public void Parses_multiple_allowlisted_sort_fields_and_defaults_missing_directions()
    {
        var sorting = SaleSort.Parse("SaleDate DESC, customerName, totalAmount asc");
        Assert.Equal(new[] { new SaleSort("saledate", true), new SaleSort("customername", false),
            new SaleSort("totalamount", false) }, sorting);
        Assert.Equal(new SaleSort("saledate", true), Assert.Single(SaleSort.Parse(null)));
    }
}
