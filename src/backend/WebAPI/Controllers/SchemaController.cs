// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using GymFlow.Application.DTOs.Schema;
using GymFlow.Application.UseCases.Schema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.WebAPI.Controllers;

/// <summary>
/// Endpoints for schema versioning operations (HU-017).
/// Provides upgrade, status, and validation endpoints for the GymFlow CLI.
///
/// These endpoints are consumed by the GymFlow.Cli tool via HTTP.
/// Authorization: Admin role required for all schema operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SchemaController : ControllerBase
{
    private readonly UpgradeSchemaUseCase _upgradeUseCase;
    private readonly GetSchemaStatusUseCase _statusUseCase;
    private readonly ValidateSchemaUseCase _validateUseCase;
    private readonly IConfiguration _configuration;

    public SchemaController(
        UpgradeSchemaUseCase upgradeUseCase,
        GetSchemaStatusUseCase statusUseCase,
        ValidateSchemaUseCase validateUseCase,
        IConfiguration configuration)
    {
        _upgradeUseCase = upgradeUseCase ?? throw new ArgumentNullException(nameof(upgradeUseCase));
        _statusUseCase = statusUseCase ?? throw new ArgumentNullException(nameof(statusUseCase));
        _validateUseCase = validateUseCase ?? throw new ArgumentNullException(nameof(validateUseCase));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// POST /api/schema/upgrade
    ///
    /// Ejecuta un upgrade de esquema hacia una versión objetivo.
    /// Soporta flags: --target, --skip-backup, --dry-run, --verbose.
    ///
    /// Respuestas:
    ///   200 OK              → upgrade completado exitosamente
    ///   400 Bad Request     → parámetros inválidos (version mal formada, directorio no existe)
    ///   409 Conflict        → otro upgrade está en curso, o falló por lock/pre-check
    ///   500 Internal Error  → error inesperado
    /// </summary>
    [HttpPost("upgrade")]
    [ProducesResponseType(typeof(UpgradeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upgrade(
        [FromBody] UpgradeRequest request,
        CancellationToken ct)
    {
        var migrationsDir = _configuration.GetValue<string>("Schema:MigrationsDirectory")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Persistence", "Migrations");

        var result = await _upgradeUseCase.ExecuteAsync(
            targetVersion: request.TargetVersion,
            appliedBy: request.AppliedBy ?? "api-upgrade",
            migrationsDirectory: migrationsDir,
            skipBackup: request.SkipBackup,
            dryRun: request.DryRun,
            verbose: request.Verbose,
            ct: ct);

        return result.StatusCode switch
        {
            200 when result.Value!.Success => Ok(result.Value),
            200 when !result.Value!.Success => StatusCode(StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Title = "Upgrade fallido.",
                    Detail = result.Value.ErrorMessage,
                    Status = 409
                }),
            400 => BadRequest(new ProblemDetails
            {
                Title = "Error de validación.",
                Detail = result.Error,
                Status = 400
            }),
            409 => Conflict(new ProblemDetails
            {
                Title = "Conflicto.",
                Detail = result.Error,
                Status = 409
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error interno.",
                    Detail = result.Error,
                    Status = 500
                })
        };
    }

    /// <summary>
    /// GET /api/schema/status
    ///
    /// Obtiene el estado actual del esquema: versión actual, pendientes, espacio en disco,
    /// versión de PostgreSQL, estado del advisory lock, y versiones por módulo.
    ///
    /// Respuestas:
    ///   200 OK              → estado del esquema
    ///   500 Internal Error  → error al consultar
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SchemaStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var migrationsDir = _configuration.GetValue<string>("Schema:MigrationsDirectory")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Persistence", "Migrations");

        var result = await _statusUseCase.ExecuteAsync(
            migrationsDirectory: migrationsDir,
            ct: ct);

        return result.StatusCode switch
        {
            200 => Ok(result.Value),
            _ => StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error interno.",
                    Detail = result.Error,
                    Status = 500
                })
        };
    }

    /// <summary>
    /// GET /api/schema/validate
    ///
    /// Valida la consistencia del esquema: correspondencia schema_version ↔ __EFMigrationsHistory,
    /// migraciones huérfanas, y violaciones de política aditiva.
    ///
    /// Respuestas:
    ///   200 OK              → resultado de validación (puede ser inválido)
    ///   500 Internal Error  → error al validar
    /// </summary>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(SchemaValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Validate(CancellationToken ct)
    {
        var migrationsDir = _configuration.GetValue<string>("Schema:MigrationsDirectory")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Persistence", "Migrations");

        var result = await _validateUseCase.ExecuteAsync(
            migrationsDirectory: migrationsDir,
            ct: ct);

        return result.StatusCode switch
        {
            200 => Ok(result.Value),
            _ => StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error interno.",
                    Detail = result.Error,
                    Status = 500
                })
        };
    }
}

/// <summary>
/// Request body for the upgrade endpoint.
/// </summary>
public record UpgradeRequest(
    string? TargetVersion,
    bool SkipBackup = false,
    bool DryRun = false,
    bool Verbose = false,
    string? AppliedBy = null
);
