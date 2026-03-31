using GymFlow.Application.DTOs.Metrics;
using GymFlow.Application.UseCases.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.WebAPI.Controllers.Admin;

[ApiController]
[Route("api/admin/metrics")]
[Authorize(Roles = "Admin,Owner")]
public class MetricsController : ControllerBase
{
    private readonly GetIncomeReportUseCase _getIncomeReport;
    private readonly GetChurnReportUseCase _getChurnReport;

    public MetricsController(
        GetIncomeReportUseCase getIncomeReport,
        GetChurnReportUseCase getChurnReport)
    {
        _getIncomeReport = getIncomeReport;
        _getChurnReport = getChurnReport;
    }

    [HttpGet("income")]
    public async Task<IActionResult> GetIncome(
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken ct)
    {
        var parsedFrom = DateOnly.Parse(from);
        var parsedTo = DateOnly.Parse(to);

        var result = await _getIncomeReport.ExecuteAsync(parsedFrom, parsedTo, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error });
    }

    [HttpGet("churn")]
    public async Task<IActionResult> GetChurn(
        [FromQuery] int year,
        CancellationToken ct)
    {
        var result = await _getChurnReport.ExecuteAsync(year, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new ProblemDetails { Title = result.Error });
    }
}