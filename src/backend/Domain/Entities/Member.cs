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
    public bool AutoRenewEnabled { get; private set; } = true;
    public DateTime? CancelledAt { get; private set; }
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
        (Status == MemberStatus.Active || Status == MemberStatus.Cancelled) && MembershipEndDate >= DateOnly.FromDateTime(DateTime.UtcNow);

    public string? GetDenialReason() => Status switch
    {
        MemberStatus.Frozen  => "La membresía está congelada.",
        MemberStatus.Expired => "La membresía está vencida.",
        _                    => MembershipEndDate < DateOnly.FromDateTime(DateTime.UtcNow)
                                    ? "La membresía ha expirado."
                                    : null
    };

    public void Cancel()
    {
        if (Status == MemberStatus.Expired)
            throw new DomainException("Cannot cancel an expired membership.");

        AutoRenewEnabled = false;
        CancelledAt = DateTime.UtcNow;

        if (Status == MemberStatus.Active)
            Status = MemberStatus.Cancelled;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = MemberStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Aplica un congelamiento: cambia el status a Frozen y extiende MembershipEndDate
    /// sumando los días de pausa al vencimiento actual.
    ///
    /// HU-07 Regla R3: bloqueo de acceso inmediato.
    /// HU-07 Regla R4: EndDate se recalcula sumando durationDays.
    /// </summary>
    /// <param name="durationDays">Días efectivos del congelamiento (mín. 7, validado en MembershipFreeze.Create).</param>
    public void Freeze(int durationDays)
    {
        if (durationDays < 7)
            throw new ArgumentException(
                "No se puede congelar por menos de 7 días.",
                nameof(durationDays));

        Status = MemberStatus.Frozen;
        MembershipEndDate = MembershipEndDate.AddDays(durationDays);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Descongela la membresía: restaura el status a Active.
    /// El MembershipEndDate ya fue extendido al momento de congelar y NO se revierte.
    /// </summary>
    public void Unfreeze()
    {
        if (Status != MemberStatus.Frozen)
            throw new InvalidOperationException(
                $"No se puede descongelar un socio con status '{Status}'. Solo aplica a socios Frozen.");

        Status = MemberStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Valida la regla HU-07: máximo 4 congelamientos por año calendario.
    /// </summary>
    /// <param name="freezeCountThisYear">Cantidad de congelamientos ya registrados en el año actual.</param>
    /// <returns>True si puede congelarse, false si ya alcanzó el límite.</returns>
    public bool CanFreezeThisYear(int freezeCountThisYear) => freezeCountThisYear < 4;
}
