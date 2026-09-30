using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesValidator : AbstractValidator<ListSalesQuery>
{
    public ListSalesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("_page must be at least 1.");
        RuleFor(x => x.Size).InclusiveBetween(1, 100).WithMessage("_size must be between 1 and 100.");
        RuleFor(x => x.SearchTerm).MaximumLength(200);
        RuleFor(x => x.Order).MaximumLength(200).Must(IsValidOrder)
            .WithMessage("Invalid _order. Use saleNumber, saleDate, customerName, branchName, totalAmount, isCancelled or id with asc/desc.");
    }

    private static bool IsValidOrder(string? value)
    {
        try { _ = SaleSort.Parse(value); return true; }
        catch (DomainException) { return false; }
    }
}
