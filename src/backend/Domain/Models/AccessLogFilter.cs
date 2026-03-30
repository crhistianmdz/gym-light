namespace GymFlow.Domain.Models;

public class AccessLogFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public Guid? MemberId { get; set; }
    public string? Result { get; set; } // "Allowed" or "Denied"
}