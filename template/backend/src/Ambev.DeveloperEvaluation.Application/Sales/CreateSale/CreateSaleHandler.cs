using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, Common.SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateSaleCommand> _validator;

    public CreateSaleHandler(ISaleRepository saleRepository, IMapper mapper, IValidator<CreateSaleCommand> validator)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<Common.SaleResult> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new FluentValidation.ValidationException(validation.Errors);

        // Create domain sale
        var sale = Sale.Create(request.SaleDate.ToUniversalTime(), request.CustomerId, request.CustomerName, request.BranchId, request.BranchName);

        // Add items via domain method (enforces pricing and rules)
        foreach (var it in request.Items)
        {
            sale.AddItem(it.ProductId, it.ProductName, it.Quantity, it.UnitPrice);
        }

        // Persist
        var created = await _saleRepository.AddAsync(sale, cancellationToken);

        // Map result
        var result = _mapper.Map<Ambev.DeveloperEvaluation.Application.Sales.Common.SaleResult>(created);
        return result;
    }
}