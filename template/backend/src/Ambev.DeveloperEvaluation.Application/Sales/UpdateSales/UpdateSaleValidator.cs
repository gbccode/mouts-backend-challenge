using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleValidator : AbstractValidator<UpdateSaleCommand>
{
    public UpdateSaleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SaleDate).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.BranchName).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.ProductId).NotEmpty();
            items.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(200);
            items.RuleFor(i => i.Quantity).InclusiveBetween(1, 20);
            items.RuleFor(i => i.UnitPrice).GreaterThan(0).ScalePrecision(2, 18);
        });

        RuleFor(x => x.Items)
            .Must(list => list.Select(i => i.ProductId).Distinct().Count() == list.Count)
            .WithMessage("Duplicate productId in items is not allowed.");

        RuleFor(x => x.Items)
            .Must(list => list.Select(i => i.Id).Where(id => id != null).Select(id => id!.Value).Distinct().Count()
                == list.Where(i => i.Id != null).Count())
            .WithMessage("Duplicate item id in request.");
    }
}