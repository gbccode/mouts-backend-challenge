using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public class CancelSaleHandler : IRequestHandler<CancelSaleCommand, Common.SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CancelSaleCommand> _validator;

    public CancelSaleHandler(ISaleRepository saleRepository, IMapper mapper, IValidator<CancelSaleCommand> validator)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<Common.SaleResult> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var sale = await _saleRepository.GetByIdAsync(request.SaleId, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException("Sale not found.");

        var changed = sale.Cancel();
        if (changed)
            await _saleRepository.SaveAsync(sale, cancellationToken);

        var refreshed = await _saleRepository.GetByIdAsync(sale.Id, cancellationToken);
        return _mapper.Map<Common.SaleResult>(refreshed!);
    }
}