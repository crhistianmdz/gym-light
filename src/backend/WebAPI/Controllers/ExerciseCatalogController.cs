using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.ExerciseCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymFlow.WebAPI.Controllers;

[ApiController]
[Route("api/exercise-catalog")]
[Authorize]
public class ExerciseCatalogController(
    CreateExerciseUseCase createExercise,
    GetExercisesUseCase getExercises) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? name, CancellationToken ct)
    {
        var result = await getExercises.ExecuteAsync(name, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Trainer,Admin,Owner")]
    public async Task<IActionResult> Create([FromBody] CreateExerciseRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await createExercise.ExecuteAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetAll), result);
    }
}