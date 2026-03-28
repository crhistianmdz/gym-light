using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.WebAPI.Controllers;

/// <summary>
/// CRUD de socios.
/// HU-02: POST /api/members — registro con foto WebP obligatoria.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Receptionist,Admin")]
public class MembersController : ControllerBase
{
    private readonly RegisterMemberUseCase _registerMember;

    public MembersController(RegisterMemberUseCase registerMember)
    {
        _registerMember = registerMember;
    }

    /// <summary>
    /// POST /api/members
    ///
    /// Registra un nuevo socio con foto WebP obligatoria.
    ///
    /// La foto debe ser enviada como data URI WebP desde el frontend:
    ///   "data:image/webp;base64,{payload}"
    ///
    /// El frontend es responsable de comprimir la imagen a WebP antes de enviarla
    /// (HU-02 CA-2 — imageService.compressToWebP()).
    ///
    /// Respuestas:
    ///   201 Created     → socio registrado, retorna MemberDto
    ///   400 Bad Request → validación fallida (sin foto, formato incorrecto, fecha pasada)
    ///   500             → error al guardar la foto
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] CreateMemberDto request,
        CancellationToken ct)
    {
        var result = await _registerMember.ExecuteAsync(request, ct);

        return result.StatusCode switch
        {
            200 => CreatedAtAction(
                        nameof(GetById),
                        new { id = result.Value!.Id },
                        result.Value),
            400 => BadRequest(new ValidationProblemDetails
                    {
                        Title = "Datos de registro inválidos.",
                        Detail = result.Error,
                        Status = 400
                    }),
            500 => StatusCode(500, new ProblemDetails
                    {
                        Title = "Error interno al registrar el socio.",
                        Detail = result.Error,
                        Status = 500
                    }),
            _   => StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error })
        };
    }

    /// <summary>
    /// GET /api/members/{id}
    /// Retorna los datos de un socio para hidratación del cliente o auditoría.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        // Placeholder — se implementa en HU posterior con GetMemberUseCase
        return NotFound();
    }
}
