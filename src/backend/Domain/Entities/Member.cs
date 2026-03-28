using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa a un socio del gimnasio.
/// La foto en formato WebP es requerida para habilitar el check-in (PRD §4.2, HU-01 CA-2).
/// </summary>
public class Member
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// URL relativa o absoluta al archivo WebP almacenado.
    /// Requerido: sin foto no hay check-in válido.
    /// </summary>
    public string PhotoWebPUrl { get; private set; } = string.Empty;

    public MemberStatus Status { get; private set; }
    public DateOnly MembershipEndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private Member() { }

    public static Member Create(
        string fullName,
        string photoWebPUrl,
        DateOnly membershipEndDate)
        => CreateWithId(Guid.NewGuid(), fullName, photoWebPUrl, membershipEndDate);

    /// <summary>
    /// Factory con Id explícito — usado cuando el Id fue pre-generado (ej: para nombrar el archivo de foto).
    /// HU-02: el Use Case genera el Id antes de llamar a IPhotoStorageService para usarlo como nombre de archivo.
    /// </summary>
    public static Member CreateWithId(
        Guid id,
        string fullName,
        string photoWebPUrl,
        DateOnly membershipEndDate)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre del socio es obligatorio.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(photoWebPUrl))
            throw new ArgumentException("La foto WebP es obligatoria para habilitar el check-in.", nameof(photoWebPUrl));

        return new Member
        {
            Id = id,
            FullName = fullName,
            PhotoWebPUrl = photoWebPUrl,
            Status = MemberStatus.Active,
            MembershipEndDate = membershipEndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Determina si el socio tiene acceso permitido.
    /// Regla PRD §4.1: Frozen y Expired deniegan el acceso.
    /// </summary>
    public bool CanAccess() =>
        Status == MemberStatus.Active && MembershipEndDate >= DateOnly.FromDateTime(DateTime.UtcNow);

    public string? GetDenialReason() => Status switch
    {
        MemberStatus.Frozen  => "La membresía está congelada.",
        MemberStatus.Expired => "La membresía está vencida.",
        _                    => MembershipEndDate < DateOnly.FromDateTime(DateTime.UtcNow)
                                    ? "La membresía ha expirado."
                                    : null
    };

    public void Expire()
    {
        Status = MemberStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Freeze()
    {
        Status = MemberStatus.Frozen;
        UpdatedAt = DateTime.UtcNow;
    }
}
