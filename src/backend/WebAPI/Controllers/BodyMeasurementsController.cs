using GymFlow.Application.DTOs.BodyMeasurements;
using GymFlow.Application.UseCases.BodyMeasurements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GymFlow.WebAPI.Controllers;

/// <summary>
/// Controller for managing body measurements.
/// </summary>
[ApiController]
[Route("api/members/{memberId:guid}/measurements")]
[Authorize]
public class BodyMeasurementsController : ControllerBase
{
    private readonly AddBodyMeasurementUseCase _addUseCase;
    private readonly GetBodyMeasurementsUseCase _getUseCase;

    public BodyMeasurementsController(
        AddBodyMeasurementUseCase addUseCase,
        GetBodyMeasurementsUseCase getUseCase)
    {
        _addUseCase = addUseCase;
        _getUseCase = getUseCase;
    }

    /// <summary>
    /// Adds a new body measurement for a member.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BodyMeasurementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid memberId, [FromBody] AddBodyMeasurementRequest request, CancellationToken ct)
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();

        if (callerId == null)
            return Unauthorized();

        var result = await _addUseCase.ExecuteAsync(memberId, callerId.Value, callerRole, request, ct);

        return result.Switch(
            created => CreatedAtAction(nameof(Add), new { id = created.Value.Id }, created.Value),
            alreadyProcessed => Ok(alreadyProcessed.Value),
            notFound => NotFound(new { Message = notFound.Error }),
            forbidden => Forbid()
        );
    }

    /// <summary>
    /// Retrieves body measurements for a specific member.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BodyMeasurementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid memberId, CancellationToken ct)
    {
        var callerId = GetCallerId();
        var callerRole = GetCallerRole();

        if (callerId == null)
            return Unauthorized();

        var result = await _getUseCase.ExecuteAsync(memberId, callerId.Value, callerRole, ct);

        return result.Switch(
            ok => Ok(ok.Value),
            notFound => NotFound(new { Message = notFound.Error }),
            forbidden => Forbid()
        );
    }

    private Guid? GetCallerId() =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "", out var id) ? id : null;

    private UserRole GetCallerRole() =>
        Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role) ? role : UserRole.Member;
}