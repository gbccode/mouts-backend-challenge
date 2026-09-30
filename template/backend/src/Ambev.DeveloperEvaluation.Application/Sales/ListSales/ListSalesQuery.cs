using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public record ListSalesQuery(int Page = 1, int Size = 10, string? SearchTerm = null,
    string? Order = null, bool? IsCancelled = null) : IRequest<ListSalesResult>;

public record ListSalesResult(IEnumerable<SaleResult> Data, int TotalCount, int CurrentPage, int TotalPages);
