using GymFlow.Application.DTOs.Metrics;
using GymFlow.Application.UseCases.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GymFlow.WebAPI.Extensions;

namespace GymFlow.WebAPI.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Admin,Owner,Receptionist")]
public class PaymentsController : ControllerBase
{
    private readonly RegisterPaymentUseCase _registerPayment;

    public PaymentsController(RegisterPaymentUseCase registerPayment)
    {
        _registerPayment = registerPayment;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterPayment(
        [FromBody] RegisterPaymentRequest request,
        CancellationToken ct)
    {
        var actingUserId = User.GetUserId(); // Extension method for JWT extraction
        var result = await _registerPayment.ExecuteAsync(request, actingUserId, ct);

        return result.IsSuccess
            ? result.Value.Id == default
                ? Ok(result.Value)
                : Created($"api/payments/{result.Value.Id}", result.Value)
            : StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error });
    }
}