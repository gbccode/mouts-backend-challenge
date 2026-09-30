using MediatR;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using System;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public record GetSaleQuery(Guid Id) : IRequest<SaleResult>;