namespace GymFlow.Application.Interfaces;

/// <summary>
/// Contrato para el servicio de almacenamiento de fotos de socios.
/// La implementación concreta (local, S3, Azure Blob) vive en Infrastructure.
/// El Use Case solo conoce esta interfaz — nunca la implementación.
/// </summary>
public interface IPhotoStorageService
{
    /// <summary>
    /// Persiste una imagen WebP codificada en base64 y retorna la URL relativa pública.
    /// </summary>
    /// <param name="base64DataUri">Data URI completo: "data:image/webp;base64,{payload}"</param>
    /// <param name="memberId">GUID del socio — define el nombre de archivo</param>
    /// <returns>URL relativa pública accesible desde el cliente, ej: "/photos/{memberId}.webp"</returns>
    Task<string> SavePhotoAsync(string base64DataUri, Guid memberId, CancellationToken ct = default);

    /// <summary>
    /// Elimina la foto asociada a un socio. Usado en caso de rollback o borrado de cuenta.
    /// </summary>
    Task DeletePhotoAsync(Guid memberId, CancellationToken ct = default);
}
