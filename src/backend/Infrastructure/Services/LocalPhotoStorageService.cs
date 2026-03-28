using GymFlow.Application.Interfaces;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Implementación local de IPhotoStorageService.
/// Guarda las fotos WebP en el sistema de archivos bajo wwwroot/photos/.
///
/// Para producción, reemplazar esta implementación por una que use
/// Azure Blob Storage o S3 — sin cambiar el Use Case (principio de inversión de dependencias).
/// </summary>
public class LocalPhotoStorageService : IPhotoStorageService
{
    private const string WebPPrefix = "data:image/webp;base64,";
    private readonly string _photosDirectory;
    private readonly string _publicBasePath;

    /// <param name="webRootPath">IWebHostEnvironment.WebRootPath — inyectado desde Program.cs</param>
    public LocalPhotoStorageService(string webRootPath)
    {
        _photosDirectory = Path.Combine(webRootPath, "photos");
        _publicBasePath = "/photos";

        Directory.CreateDirectory(_photosDirectory);
    }

    public async Task<string> SavePhotoAsync(
        string base64DataUri,
        Guid memberId,
        CancellationToken ct = default)
    {
        if (!base64DataUri.StartsWith(WebPPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El data URI no corresponde a una imagen WebP.", nameof(base64DataUri));

        // Extraer payload base64 puro (quitar el prefijo del data URI)
        var base64Payload = base64DataUri[WebPPrefix.Length..];
        var imageBytes = Convert.FromBase64String(base64Payload);

        var fileName = $"{memberId}.webp";
        var filePath = Path.Combine(_photosDirectory, fileName);

        await File.WriteAllBytesAsync(filePath, imageBytes, ct);

        return $"{_publicBasePath}/{fileName}";
    }

    public Task DeletePhotoAsync(Guid memberId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_photosDirectory, $"{memberId}.webp");

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
