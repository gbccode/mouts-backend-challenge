using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.WebApi.Common;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
        try
        {
            var result = await _mediator.Send(new Application.Sales.GetSale.GetSaleQuery(id), cancellationToken);
            return Ok(new ApiResponseWithData<SaleResult> { Success = true, Data = result });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Sale not found." });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSale.UpdateSaleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<Application.Sales.UpdateSale.UpdateSaleCommand>(request);
            command.Id = id;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponseWithData<SaleResult> { Success = true, Data = result });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new ApiResponse { Success = false, Errors = ex.Errors.Select(e => (DeveloperEvaluation.Common.Validation.ValidationErrorDetail)e) });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Sale not found." });
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> CancelSale(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new Application.Sales.CancelSale.CancelSaleCommand { SaleId = id }, cancellationToken);
            return Ok(new ApiResponseWithData<SaleResult> { Success = true, Data = result });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Sale not found." });
        }
    }

    [HttpPatch("{saleId:guid}/items/{itemId:guid}/cancel")]
    public async Task<IActionResult> CancelItem(Guid saleId, Guid itemId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new Application.Sales.CancelSaleItem.CancelSaleItemCommand { SaleId = saleId, ItemId = itemId }, cancellationToken);
            return Ok(new ApiResponseWithData<SaleResult> { Success = true, Data = result });
        }
        catch (KeyNotFoundException ex)
        {
            var msg = ex.Message.Contains("Item") ? "Item not found." : "Sale not found.";
            return NotFound(new ApiResponse { Success = false, Message = msg });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new Application.Sales.DeleteSale.DeleteSaleCommand { Id = id }, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse { Success = false, Message = "Sale not found." });
        }
    }
}