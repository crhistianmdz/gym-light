namespace GymFlow.Application.DTOs.Metrics;

public record ChurnReportDto
{
    public int Year { get; init; }
    public int TotalMembers { get; init; }
    public int ActiveMembers { get; init; }
    public int NotRenewed { get; init; }
    public double ChurnRate { get; init; }
}