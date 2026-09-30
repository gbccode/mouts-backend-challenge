using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Ambev.DeveloperEvaluation.ORM.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

public class SaleRepositoryTests
{
    private static DefaultContext CreateContext() => new(new DbContextOptionsBuilder<DefaultContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Sale CreateSale(string customer, decimal price)
    {
        var sale = Sale.Create(new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(), customer, Guid.NewGuid(), "Main branch");
        sale.AddItem(Guid.NewGuid(), "Product", 1, price);
        return sale;
    }

    [Fact]
    public void Maps_one_items_navigation_and_creates_both_sales_tables()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Sale))!;
        var navigation = Assert.Single(entity.GetNavigations());
        Assert.Equal(nameof(Sale.Items), navigation.Name);
        Assert.Equal("_items", navigation.FieldInfo!.Name);
        var tables = new CreateSalesTables().UpOperations.OfType<CreateTableOperation>().ToArray();
        Assert.Equal(new[] { "Sales", "SaleItems" }, tables.Select(table => table.Name));
        Assert.Single(tables[1].ForeignKeys);
    }

    [Fact]
    public async Task Filters_before_counting_and_paging_and_includes_items()
    {
        await using var context = CreateContext();
        var low = CreateSale("Acme", 10m);
        var high = CreateSale("ACME", 50m);
        var cancelled = CreateSale("Acme", 100m);
        cancelled.Cancel();
        context.Sales.AddRange(low, high, cancelled, CreateSale("Other", 200m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new SaleRepository(context).GetPageAsync(1, 1, " acme ", "totalAmount desc", false);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(high.Id, Assert.Single(result.Sales).Id);
        Assert.Single(Assert.Single(result.Sales).Items);
    }

    [Fact]
    public async Task Uses_a_unique_tie_breaker_and_clamps_pages_after_deletion()
    {
        await using var context = CreateContext();
        var sales = Enumerable.Range(0, 3).Select(_ => CreateSale("Same", 100m)).ToArray();
        context.Sales.AddRange(sales);
        await context.SaveChangesAsync();
        var repository = new SaleRepository(context);

        var first = await repository.GetPageAsync(1, 2);
        var second = await repository.GetPageAsync(2, 2);
        Assert.Equal(sales.Select(s => s.Id).OrderBy(id => id), first.Sales.Concat(second.Sales).Select(s => s.Id));

        await repository.DeleteAsync(Assert.Single(second.Sales).Id);
        var recovered = await repository.GetPageAsync(2, 2);
        Assert.Equal(1, recovered.CurrentPage);
        Assert.Equal(1, recovered.TotalPages);
        Assert.Equal(2, recovered.TotalItems);
    }

    [Fact]
    public async Task Returns_an_empty_first_page_when_filters_have_no_matches()
    {
        await using var context = CreateContext();
        var result = await new SaleRepository(context).GetPageAsync(int.MaxValue, 10, "missing");
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(0, result.TotalPages);
        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Sales);
    }
}
