namespace GymFlow.WebAPI.Controllers;

using GymFlow.Application.UseCases;
using GymFlow.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly CreateSaleUseCase _createSaleUseCase;
    private readonly CancelSaleUseCase _cancelSaleUseCase;

    public SalesController(
CreateSaleUseCase createSaleUseCase,
         CancelSaleUseCase cancelSaleUseCase,
         GetSalesUseCase getSalesUseCase)
    {
        _createSaleUseCase = createSaleUseCase;
        _cancelSaleUseCase = cancelSaleUseCase;
        _getSalesUseCase = getSalesUseCase;
    }

    [HttpPost]
    [Authorize(Roles = "Receptionist,Admin,Owner")]
    public async Task<IActionResult> Create(
        [FromBody] CreateSaleRequestDto dto,
        CancellationToken ct)
    {
        var result = await _createSaleUseCase.ExecuteAsync(dto, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return result.StatusCode == 200
            ? Ok(result.Value)
            : CreatedAtAction(nameof(Create), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Owner,Receptionist")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getSalesUseCase.ExecuteAsync(page, pageSize, ct);
        return result.IsSuccess ? Ok (Result!)!!...
/// ful-code.
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _cancelSaleUseCase.ExecuteAsync(id, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}