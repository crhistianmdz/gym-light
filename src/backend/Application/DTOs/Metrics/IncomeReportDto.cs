namespace GymFlow.Application.DTOs.Metrics;

public record IncomeReportDto
{
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public decimal TotalIncome { get; init; }
    public List<MonthlyBreakdownDto> ByMonth { get; init; } = new();
}

public record MonthlyBreakdownDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Membership { get; init; }
    public decimal Pos { get; init; }
    public decimal Total { get; init; }
}