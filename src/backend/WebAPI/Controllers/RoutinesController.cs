using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Routines;
using GymFlow.Application.UseCases.WorkoutLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.WebAPI.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class RoutinesController(
    CreateRoutineUseCase createRoutine,
    UpdateRoutineUseCase updateRoutine,
    GetRoutinesUseCase getRoutines,
    AssignRoutineUseCase assignRoutine,
    GetMemberRoutinesUseCase getMemberRoutines,
    CreateWorkoutLogUseCase createWorkoutLog,
    GetWorkoutLogsUseCase getWorkoutLogs) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdminOrOwner => User.IsInRole("Admin") || User.IsInRole("Owner");

    [HttpGet("routines")]
    [Authorize(Roles = "Trainer,Admin,Owner")]
    public async Task<IActionResult> GetRoutines([FromQuery] bool? isPublic, [FromQuery] bool mine = false, CancellationToken ct = default)
    {
        var result = await getRoutines.ExecuteAsync(CurrentUserId, isPublic, mine, ct);
        return Ok(result);
    }

    [HttpPost("routines")]
    [Authorize(Roles = "Trainer,Admin,Owner")]
    public async Task<IActionResult> CreateRoutine([FromBody] CreateRoutineRequest request, CancellationToken ct)
    {
        var result = await createRoutine.ExecuteAsync(request, CurrentUserId, ct);
        return CreatedAtAction(nameof(GetRoutines), new { id = result.Id }, result);
    }

    [HttpPut("routines/{id:guid}")]
    [Authorize(Roles = "Trainer,Admin,Owner")]
    public async Task<IActionResult> UpdateRoutine(Guid id, [FromBody] UpdateRoutineRequest request, CancellationToken ct)
    {
        try
        {
            var result = await updateRoutine.ExecuteAsync(id, request, CurrentUserId, IsAdminOrOwner, ct);
            if (result is null) return NotFound();
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost("routine-assignments")]
    [Authorize(Roles = "Trainer,Admin,Owner")]
    public async Task<IActionResult> AssignRoutine([FromBody] AssignRoutineRequest request, CancellationToken ct)
    {
        try
        {
            var result = await assignRoutine.ExecuteAsync(request, CurrentUserId, ct);
            return CreatedAtAction(nameof(GetMemberRoutines), new { memberId = result.MemberId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("members/{memberId:guid}/routines")]
    public async Task<IActionResult> GetMemberRoutines(Guid memberId, CancellationToken ct)
    {
        if (User.IsInRole("Member") && CurrentUserId != memberId)
            return Forbid();

        var result = await getMemberRoutines.ExecuteAsync(memberId, ct);
        return Ok(result);
    }

    [HttpPost("workout-logs")]
    public async Task<IActionResult> CreateWorkoutLog([FromBody] CreateWorkoutLogRequest request, CancellationToken ct)
    {
        try
        {
            var (dto, alreadyProcessed) = await createWorkoutLog.ExecuteAsync(request, CurrentUserId, ct);
            if (alreadyProcessed)
                return Ok(new { alreadyProcessed = true, data = dto });
            return CreatedAtAction(nameof(GetWorkoutLogs), new { memberId = dto.MemberId }, dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("members/{memberId:guid}/workout-logs")]
    public async Task<IActionResult> GetWorkoutLogs(Guid memberId, CancellationToken ct)
    {
        if (User.IsInRole("Member") && CurrentUserId != memberId)
            return Forbid();

        var result = await getWorkoutLogs.ExecuteAsync(memberId, ct);
        return Ok(result);
    }
}