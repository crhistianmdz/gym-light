using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GymFlow.WebAPI.Controllers;

/// <summary>
/// CRUD de socios.
/// HU-02: POST /api/members — registro con foto WebP obligatoria.
/// HU-07: POST/DELETE /api/members/{id}/freeze — congelamiento de membresía (Admin/Owner).
/// HU-08: POST /api/members/{id}/cancel — cancelación con acceso residual.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Receptionist,Admin,Owner")]
public class MembersController : ControllerBase
{
    private readonly RegisterMemberUseCase _registerMember;
    private readonly FreezeMembershipUseCase _freezeMembership;
    private readonly UnfreezeMembershipUseCase _unfreezeMembership;
    private readonly CancelMembershipUseCase _cancelMembership;

    public MembersController(
        RegisterMemberUseCase registerMember,
        FreezeMembershipUseCase freezeMembership,
        UnfreezeMembershipUseCase unfreezeMembership,
        CancelMembershipUseCase cancelMembership)
    {
        _registerMember     = registerMember;
        _freezeMembership   = freezeMembership;
        _unfreezeMembership = unfreezeMembership;
        _cancelMembership   = cancelMembership;
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

    // ─────────────────────────────────────────────────────────────────────────
    // HU-07 — Congelamiento de membresía
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/members/{id}/freeze
    ///
    /// Congela la membresía de un socio. Solo Admin y Owner.
    ///
    /// Reglas HU-07:
    ///   - Mínimo 7 días.
    ///   - Máximo 4 congelamientos por año calendario.
    ///   - Status cambia a Frozen inmediatamente.
    ///   - MembershipEndDate se extiende sumando los días de pausa.
    ///
    /// Body: { "startDate": "2026-04-01", "endDate": "2026-04-14" }
    ///
    /// Respuestas:
    ///   200 OK          → congelamiento aplicado, retorna MembershipFreezeDto
    ///   400 Bad Request → validación fallida (duración < 7 días, límite anual alcanzado, socio no Active)
    ///   404 Not Found   → socio no encontrado
    ///   403 Forbidden   → rol insuficiente
    /// </summary>
    [HttpPost("{id:guid}/freeze")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(typeof(MembershipFreezeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Freeze(Guid id, [FromBody] FreezeRequestBody body, CancellationToken ct)
    {
        var callerId = GetCurrentUserId();
        if (callerId is null)
            return Unauthorized();

        var dto    = new FreezeMembershipDto(id, body.StartDate, body.EndDate);
        var result = await _freezeMembership.ExecuteAsync(dto, callerId.Value, ct);

        return result.StatusCode switch
        {
            200 => Ok(result.Value),
            400 => BadRequest(new ProblemDetails
                    {
                        Title  = "No se pudo congelar la membresía.",
                        Detail = result.Error,
                        Status = 400
                    }),
            404 => NotFound(new ProblemDetails
                    {
                        Title  = "Socio no encontrado.",
                        Detail = result.Error,
                        Status = 404
                    }),
            _   => StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error })
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HU-08 — Cancelación con acceso residual
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/members/{id}/cancel
    ///
    /// Cancela la membresía de un socio (sin reembolso). El socio conserva
    /// acceso residual hasta su MembershipEndDate original.
    ///
    /// RBAC:
    ///   - Member: solo puede cancelar su propio id.
    ///   - Admin / Owner: pueden cancelar cualquier socio.
    ///   - Receptionist / Trainer: 403 Forbidden.
    ///
    /// Idempotente via ClientGuid — si el guid ya fue procesado, retorna 200 sin reprocesar.
    ///
    /// Respuestas:
    ///   200 OK          → cancelación aplicada o ya estaba cancelado (idempotente), retorna MemberDto
    ///   400 Bad Request → membresía ya vencida (Expired)
    ///   403 Forbidden   → rol insuficiente o Member intentando cancelar id ajeno
    ///   404 Not Found   → socio no encontrado
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [AllowAnonymous]   // JWT sigue siendo validado manualmente — clase bloquea Member role
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelMembership(Guid id, [FromBody] CancelMembershipRequestDto body, CancellationToken ct)
    {
        var callerId = GetCurrentUserId();
        if (callerId is null)
            return Unauthorized();

        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (callerRole is "Receptionist" or "Trainer")
            return Forbid();

        var result = await _cancelMembership.ExecuteAsync(id, callerId.Value, callerRole, body, ct);

        return result.StatusCode switch
        {
            200 => Ok(result.Value),
            400 => BadRequest(new ProblemDetails
                    {
                        Title  = "No se pudo cancelar la membresía.",
                        Detail = result.Error,
                        Status = 400
                    }),
            403 => Forbid(),
            404 => NotFound(new ProblemDetails
                    {
                        Title  = "Socio no encontrado.",
                        Detail = result.Error,
                        Status = 404
                    }),
            _   => StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error })
        };
    }

    /// <summary>
    /// DELETE /api/members/{id}/freeze
    ///
    /// Descongela la membresía de un socio. Solo Admin y Owner.
    /// El MembershipEndDate NO se revierte (ya fue extendido al congelar).
    ///
    /// Respuestas:
    ///   200 OK          → membresía activa de nuevo, retorna MemberDto actualizado
    ///   400 Bad Request → socio no está Frozen
    ///   404 Not Found   → socio no encontrado
    ///   403 Forbidden   → rol insuficiente
    /// </summary>
    [HttpDelete("{id:guid}/freeze")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Unfreeze(Guid id, CancellationToken ct)
    {
        var result = await _unfreezeMembership.ExecuteAsync(id, ct);

        return result.StatusCode switch
        {
            200 => Ok(result.Value),
            400 => BadRequest(new ProblemDetails
                    {
                        Title  = "No se pudo descongelar la membresía.",
                        Detail = result.Error,
                        Status = 400
                    }),
            404 => NotFound(new ProblemDetails
                    {
                        Title  = "Socio no encontrado.",
                        Detail = result.Error,
                        Status = 404
                    }),
            _   => StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error })
        };
    }

    /// <summary>
    /// GET /api/members/{id}/freezes
    ///
    /// Retorna el historial de congelamientos de un socio. Solo Admin y Owner.
    /// </summary>
    [HttpGet("{id:guid}/freezes")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(typeof(IReadOnlyList<MembershipFreezeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFreezeHistory(
        Guid id,
        [FromServices] Domain.Interfaces.IMembershipFreezeRepository freezeRepo,
        [FromServices] Domain.Interfaces.IMemberRepository memberRepo,
        CancellationToken ct)
    {
        var member = await memberRepo.GetByIdAsync(id, ct);
        if (member is null)
            return NotFound();

        var freezes = await freezeRepo.GetByMemberAsync(id, ct);

        var dtos = freezes.Select(f => new MembershipFreezeDto(
            f.Id, f.MemberId, f.StartDate, f.EndDate,
            f.DurationDays, f.CreatedByUserId, f.CreatedAt))
            .ToList();

        return Ok(dtos);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

/// <summary>Body para el endpoint POST /freeze.</summary>
public record FreezeRequestBody(DateOnly StartDate, DateOnly EndDate);

