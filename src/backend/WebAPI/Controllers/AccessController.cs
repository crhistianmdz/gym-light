using GymFlow.Application.UseCases.Access;
using GymFlow.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.WebAPI.Controllers;

/// <summary>
/// Endpoint de control de acceso al gimnasio.
/// Requiere rol Receptionist o Admin (PRD §2 — RBAC).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Receptionist,Admin")]
public class AccessController : ControllerBase
{
    private readonly ValidateAccessUseCase _validateAccess;

    public AccessController(ValidateAccessUseCase validateAccess)
    {
        _validateAccess = validateAccess;
    }

    /// <summary>
    /// POST /api/access/checkin
    ///
    /// Registra el intento de acceso de un socio.
    ///
    /// Idempotencia: si el ClientGuid ya fue procesado, retorna 200 OK
    /// con el resultado original sin reejecutar lógica (RFC §4).
    ///
    /// Respuestas:
    ///   200 OK              → acceso permitido (o duplicado detectado)
    ///   403 Forbidden       → membresía vencida o congelada
    ///   404 Not Found       → socio no encontrado
    /// </summary>
    [HttpPost("checkin")]
[ServiceFilter(typeof(IdempotencyFilter))]
    [ProducesResponseType(typeof(AccessValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequestDto request,
        CancellationToken ct)
    {
        var result = await _validateAccess.ExecuteAsync(request, ct);

        return result.StatusCode switch
        {
            200 when result.Value!.Allowed  => Ok(result.Value),
            200 when !result.Value!.Allowed => StatusCode(StatusCodes.Status403Forbidden,
                                                new ProblemDetails
                                                {
                                                    Title = "Acceso denegado.",
                                                    Detail = result.Value.DenialReason,
                                                    Status = 403
                                                }),
            404 => NotFound(new ProblemDetails { Title = result.Error, Status = 404 }),
            _   => StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error })
        };
    }
}
