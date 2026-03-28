namespace GymFlow.Application.DTOs;

/// <summary>
/// DTO de entrada para el registro de un nuevo socio.
///
/// HU-02 CA-1: PhotoWebPBase64 es obligatorio — sin foto no hay registro.
/// HU-02 CA-2: La imagen debe llegar ya comprimida en WebP desde el frontend.
///             El backend valida el prefijo del data URI para rechazar otros formatos.
/// </summary>
public record CreateMemberDto(
    string FullName,

    /// <summary>
    /// Imagen comprimida a WebP en el cliente, codificada en base64.
    /// Formato esperado: "data:image/webp;base64,{payload}"
    /// </summary>
    string PhotoWebPBase64,

    /// <summary>
    /// Fecha de vencimiento de la membresía. Debe ser posterior a hoy.
    /// </summary>
    DateOnly MembershipEndDate
);
