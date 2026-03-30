using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.WebAPI.Controllers.Admin;

[ApiController]
[Route("api/admin/access-logs")]
[Authorize(Roles = "Admin,Owner")]
public class AccessLogsController : ControllerBase
{
    private readonly GetAccessLogsUseCase _getAccessLogs;
    private readonly ExportAccessLogsUseCase _exportAccessLogs;

    public AccessLogsController(
        GetAccessLogsUseCase getAccessLogs,
        ExportAccessLogsUseCase exportAccessLogs)
    {
        _getAccessLogs = getAccessLogs;
        _exportAccessLogs = exportAccessLogs;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccessLogs(
        [FromQuery] AccessLogFilterDto filterDto,
        CancellationToken ct)
    {
        var result = await _getAccessLogs.ExecuteAsync(filterDto, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error });
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAccessLogs(
        [FromQuery] AccessLogFilterDto filterDto,
        string format,
        CancellationToken ct)
    {
        var result = await _exportAccessLogs.ExecuteAsync(filterDto, format, ct);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error });
        }

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return File(result.Value, "text/csv", $"access-logs-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return StatusCode(501, new ProblemDetails { Title = "Format not supported." });
    }
}