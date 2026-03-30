namespace GymFlow.Application.DTOs;

public class AccessLogDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty; // "Allowed" or "Denied"
    public string? DenialReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ClientGuid { get; set; }
}