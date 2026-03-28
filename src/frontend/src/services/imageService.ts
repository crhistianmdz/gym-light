/**
 * Servicio de compresión y conversión de imágenes a WebP.
 *
 * HU-02 CA-2: El frontend DEBE comprimir la imagen a WebP antes de
 * enviarla al servidor o cachearla en IndexedDB.
 *
 * Implementación: API nativa del browser (Canvas + toDataURL).
 * Sin dependencias externas.
 */

const WEBP_QUALITY = 0.85
const MAX_DIMENSION_PX = 400

/**
 * Comprime una imagen (jpg, png, webp, etc.) a WebP y la retorna
 * como data URI: "data:image/webp;base64,{payload}".
 *
 * Aplica redimensionado proporcional si supera MAX_DIMENSION_PX
 * en cualquier eje para mantener archivos pequeños en la caché offline.
 */
export async function compressToWebP(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const img = new Image()
    const objectUrl = URL.createObjectURL(file)

    img.onload = () => {
      URL.revokeObjectURL(objectUrl)

      const { width, height } = calculateDimensions(img.naturalWidth, img.naturalHeight)

      const canvas = document.createElement('canvas')
      canvas.width = width
      canvas.height = height

      const ctx = canvas.getContext('2d')
      if (!ctx) {
        reject(new Error('No se pudo obtener el contexto 2D del canvas.'))
        return
      }

      ctx.drawImage(img, 0, 0, width, height)

      const dataUri = canvas.toDataURL('image/webp', WEBP_QUALITY)

      // Verificar que el browser soporta WebP (Safari antiguo devuelve PNG)
      if (!dataUri.startsWith('data:image/webp')) {
        reject(
          new Error(
            'Este navegador no soporta exportación WebP. Actualizá tu navegador.',
          ),
        )
        return
      }

      resolve(dataUri)
    }

    img.onerror = () => {
      URL.revokeObjectURL(objectUrl)
      reject(new Error('No se pudo cargar la imagen seleccionada.'))
    }

    img.src = objectUrl
  })
}

/**
 * Calcula las dimensiones finales manteniendo el aspect ratio.
 * Si la imagen es más pequeña que MAX_DIMENSION_PX, no la agranda.
 */
function calculateDimensions(
  naturalWidth: number,
  naturalHeight: number,
): { width: number; height: number } {
  const maxDim = MAX_DIMENSION_PX

  if (naturalWidth <= maxDim && naturalHeight <= maxDim) {
    return { width: naturalWidth, height: naturalHeight }
  }

  const ratio = Math.min(maxDim / naturalWidth, maxDim / naturalHeight)
  return {
    width: Math.round(naturalWidth * ratio),
    height: Math.round(naturalHeight * ratio),
  }
}

/** Retorna el tamaño aproximado en KB de un data URI base64. */
export function estimateSizeKB(dataUri: string): number {
  const base64 = dataUri.split(',')[1] ?? ''
  return Math.round((base64.length * 3) / 4 / 1024)
}
