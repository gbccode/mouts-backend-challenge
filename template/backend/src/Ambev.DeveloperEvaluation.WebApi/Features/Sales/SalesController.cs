using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

[ApiController]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ISaleRepository _saleRepository;

    public SalesController(IMediator mediator, IMapper mapper, ISaleRepository saleRepository)
    {
        _mediator = mediator;
        _mapper = mapper;
        _saleRepository = saleRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSale.CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<Application.Sales.CreateSale.CreateSaleCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        var response = new ApiResponseWithData<SaleResult> { Success = true, Data = result };
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(id, cancellationToken);
        if (sale == null)
            return NotFound(new ApiResponse { Success = false, Message = "Sale not found." });

        var result = _mapper.Map<SaleResult>(sale);
        return Ok(new ApiResponseWithData<SaleResult> { Success = true, Data = result });
    }
}