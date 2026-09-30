using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, Common.SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CancelSaleItemCommand> _validator;

    public CancelSaleItemHandler(ISaleRepository saleRepository, IMapper mapper, IValidator<CancelSaleItemCommand> validator)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<Common.SaleResult> Handle(CancelSaleItemCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var sale = await _saleRepository.GetByIdAsync(request.SaleId, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException("Sale not found.");

        var item = sale.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item == null)
            throw new KeyNotFoundException("Item not found in sale.");

        var changed = sale.CancelItem(request.ItemId);
        if (changed)
            await _saleRepository.SaveAsync(sale, cancellationToken);

        var refreshed = await _saleRepository.GetByIdAsync(sale.Id, cancellationToken);
        return _mapper.Map<Common.SaleResult>(refreshed!);
    }
}