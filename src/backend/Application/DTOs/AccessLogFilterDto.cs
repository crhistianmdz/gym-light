namespace GymFlow.Application.DTOs;

public class AccessLogFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public Guid? MemberId { get; set; }
    public string? Result { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}