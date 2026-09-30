using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, Common.SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateSaleCommand> _validator;

    public UpdateSaleHandler(ISaleRepository saleRepository, IMapper mapper, IValidator<UpdateSaleCommand> validator)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<Common.SaleResult> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException("Sale not found.");

        // Map header validation + domain update
        sale.UpdateHeader(request.SaleDate.ToUniversalTime(), request.CustomerId, request.CustomerName, request.BranchId, request.BranchName);

        var desired = request.Items.Select(i => new Sale.ItemUpdate(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();
        sale.UpdateItems(desired);

        await _saleRepository.SaveAsync(sale, cancellationToken);

        // Reload fresh aggregate to return (tracked instance already updated)
        var refreshed = await _saleRepository.GetByIdAsync(sale.Id, cancellationToken);
        return _mapper.Map<Common.SaleResult>(refreshed!);
    }
}