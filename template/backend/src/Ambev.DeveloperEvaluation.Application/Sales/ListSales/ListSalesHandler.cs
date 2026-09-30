using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<ListSalesQuery> _validator;

    public ListSalesHandler(ISaleRepository repository, IMapper mapper, IValidator<ListSalesQuery> validator)
    {
        _repository = repository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<ListSalesResult> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var page = await _repository.GetPageAsync(request.Page, request.Size, request.SearchTerm,
            request.Order, request.IsCancelled, cancellationToken);
        return new ListSalesResult(_mapper.Map<List<SaleResult>>(page.Sales),
            page.TotalItems, page.CurrentPage, page.TotalPages);
    }
}
