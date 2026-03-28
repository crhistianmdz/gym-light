import { useState, useRef, type ChangeEvent } from 'react'
import { compressToWebP, estimateSizeKB } from '@/services/imageService'

interface PhotoCaptureProps {
  onPhotoReady: (file: File, previewDataUri: string) => void
  onPhotoCleared: () => void
  error?: string
}

/**
 * Componente de captura y preview de foto para el registro de socio.
 *
 * HU-02 CA-1: mientras no haya foto válida, `onPhotoReady` no se llama
 *              y el botón submit del formulario padre permanece deshabilitado.
 * HU-02 CA-2: comprime la imagen a WebP en cuanto el usuario la selecciona,
 *              mostrando el preview del resultado comprimido.
 *
 * No usa librerías externas — solo Canvas API nativa.
 */
export function PhotoCapture({ onPhotoReady, onPhotoCleared, error }: PhotoCaptureProps) {
  const [preview, setPreview] = useState<string | null>(null)
  const [compressing, setCompressing] = useState(false)
  const [sizeKB, setSizeKB] = useState<number | null>(null)
  const [compressionError, setCompressionError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    setCompressing(true)
    setCompressionError(null)
    setPreview(null)

    try {
      // CA-2: comprimir a WebP aquí, antes de cualquier envío o caché
      const webPDataUri = await compressToWebP(file)
      const kb = estimateSizeKB(webPDataUri)

      setPreview(webPDataUri)
      setSizeKB(kb)

      // Crear un File desde el data URI para pasarlo al formulario padre
      const webPFile = dataUriToFile(webPDataUri, `photo-${Date.now()}.webp`)
      onPhotoReady(webPFile, webPDataUri)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error al procesar la imagen.'
      setCompressionError(message)
      onPhotoCleared()
    } finally {
      setCompressing(false)
    }
  }

  const handleClear = () => {
    setPreview(null)
    setSizeKB(null)
    setCompressionError(null)
    if (inputRef.current) inputRef.current.value = ''
    onPhotoCleared()
  }

  const borderColor = error || compressionError ? '#f44336' : preview ? '#4caf50' : '#ccc'

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
      <label style={{ fontWeight: 500 }}>
        Foto del socio <span style={{ color: '#f44336' }}>*</span>
      </label>

      {/* Preview */}
      <div
        style={{
          width: 120,
          height: 120,
          border: `2px dashed ${borderColor}`,
          borderRadius: 8,
          overflow: 'hidden',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: '#fafafa',
          cursor: 'pointer',
          position: 'relative',
        }}
        onClick={() => !preview && inputRef.current?.click()}
        role="button"
        aria-label="Seleccionar foto del socio"
        tabIndex={0}
        onKeyDown={(e) => e.key === 'Enter' && !preview && inputRef.current?.click()}
      >
        {compressing && (
          <span style={{ fontSize: 12, color: '#999', textAlign: 'center', padding: 8 }}>
            Comprimiendo...
          </span>
        )}

        {!compressing && preview && (
          <img
            src={preview}
            alt="Preview foto del socio"
            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
          />
        )}

        {!compressing && !preview && (
          <span style={{ fontSize: 12, color: '#bbb', textAlign: 'center', padding: 8 }}>
            📷 Clic para<br />seleccionar
          </span>
        )}
      </div>

      {/* Tamaño del archivo comprimido */}
      {preview && sizeKB !== null && (
        <span style={{ fontSize: 11, color: '#666' }}>
          ✅ WebP — {sizeKB} KB
        </span>
      )}

      {/* Botones de acción */}
      <div style={{ display: 'flex', gap: 8 }}>
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          style={{
            padding: '6px 12px',
            fontSize: 13,
            borderRadius: 4,
            border: '1px solid #ccc',
            cursor: 'pointer',
            background: '#fff',
          }}
        >
          {preview ? 'Cambiar foto' : 'Seleccionar foto'}
        </button>

        {preview && (
          <button
            type="button"
            onClick={handleClear}
            style={{
              padding: '6px 12px',
              fontSize: 13,
              borderRadius: 4,
              border: '1px solid #f44336',
              color: '#f44336',
              cursor: 'pointer',
              background: '#fff',
            }}
          >
            Quitar
          </button>
        )}
      </div>

      {/* Input oculto */}
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        onChange={handleFileChange}
        style={{ display: 'none' }}
        aria-hidden="true"
      />

      {/* Errores */}
      {(error || compressionError) && (
        <span style={{ fontSize: 12, color: '#f44336' }} role="alert">
          {compressionError ?? error}
        </span>
      )}
    </div>
  )
}

// ─── Helper ───────────────────────────────────────────────────────────────────

function dataUriToFile(dataUri: string, filename: string): File {
  const [header, base64] = dataUri.split(',')
  const mime = header.match(/:(.*?);/)?.[1] ?? 'image/webp'
  const binary = atob(base64)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i)
  }
  return new File([bytes], filename, { type: mime })
}
