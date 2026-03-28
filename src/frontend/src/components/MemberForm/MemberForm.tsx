import { useState, type FormEvent } from 'react'
import { useRegisterMember } from '@/hooks/useRegisterMember'
import { PhotoCapture } from './PhotoCapture'
import type { MemberFormData, MemberFormErrors } from './MemberForm.types'

interface MemberFormProps {
  onSuccess?: (memberId: string, fullName: string) => void
}

/**
 * Formulario de registro de nuevo socio.
 *
 * HU-02 CA-1: el botón "Guardar" permanece deshabilitado hasta que
 *              PhotoCapture llame a onPhotoReady con una imagen válida.
 * HU-02 CA-2: la compresión a WebP ocurre dentro de PhotoCapture,
 *              transparente para este componente.
 */
export function MemberForm({ onSuccess }: MemberFormProps) {
  const { register, status, result, loading, error } = useRegisterMember()

  const [formData, setFormData] = useState<MemberFormData>({
    fullName: '',
    photo: null,
    membershipEndDate: '',
  })
  const [errors, setErrors] = useState<MemberFormErrors>({})

  // Fecha mínima = mañana (membresía debe ser futura)
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  const minDate = tomorrow.toISOString().slice(0, 10)

  const handlePhotoReady = (file: File) => {
    setFormData((prev) => ({ ...prev, photo: file }))
    setErrors((prev) => ({ ...prev, photo: undefined }))
  }

  const handlePhotoCleared = () => {
    setFormData((prev) => ({ ...prev, photo: null }))
  }

  const validate = (): boolean => {
    const newErrors: MemberFormErrors = {}

    if (!formData.fullName.trim())
      newErrors.fullName = 'El nombre es obligatorio.'

    if (!formData.photo)
      newErrors.photo = 'La foto es obligatoria para habilitar el check-in.'

    if (!formData.membershipEndDate)
      newErrors.membershipEndDate = 'La fecha de vencimiento es obligatoria.'
    else if (formData.membershipEndDate < minDate)
      newErrors.membershipEndDate = 'La fecha debe ser posterior a hoy.'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!validate() || !formData.photo) return

    await register({
      fullName: formData.fullName.trim(),
      photo: formData.photo,
      membershipEndDate: formData.membershipEndDate,
    })
  }

  // Notificar al padre tras registro exitoso
  if (status === 'success' && result) {
    onSuccess?.(result.id, result.fullName)
  }

  const submitLabel = {
    idle:        'Registrar socio',
    compressing: 'Comprimiendo imagen...',
    uploading:   'Guardando...',
    success:     '✅ Registrado',
    error:       'Reintentar',
  }[status]

  // CA-1: deshabilitado si no hay foto o está cargando
  const isSubmitDisabled = loading || !formData.photo || status === 'success'

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      style={{ display: 'flex', flexDirection: 'column', gap: 20, maxWidth: 420 }}
      aria-label="Formulario de registro de socio"
    >
      <h2 style={{ margin: 0 }}>Nuevo Socio</h2>

      {/* ── Nombre ─────────────────────────────────────────────────────────── */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
        <label htmlFor="fullName" style={{ fontWeight: 500 }}>
          Nombre completo <span style={{ color: '#f44336' }}>*</span>
        </label>
        <input
          id="fullName"
          type="text"
          value={formData.fullName}
          onChange={(e) => setFormData((prev) => ({ ...prev, fullName: e.target.value }))}
          disabled={loading}
          placeholder="Ej: Juan Pérez"
          style={{
            padding: '10px 12px',
            fontSize: 15,
            border: `1px solid ${errors.fullName ? '#f44336' : '#ccc'}`,
            borderRadius: 6,
          }}
          aria-required="true"
          aria-invalid={!!errors.fullName}
          aria-describedby={errors.fullName ? 'fullName-error' : undefined}
        />
        {errors.fullName && (
          <span id="fullName-error" style={{ fontSize: 12, color: '#f44336' }} role="alert">
            {errors.fullName}
          </span>
        )}
      </div>

      {/* ── Foto (CA-1 y CA-2) ─────────────────────────────────────────────── */}
      <PhotoCapture
        onPhotoReady={handlePhotoReady}
        onPhotoCleared={handlePhotoCleared}
        error={errors.photo}
      />

      {/* ── Fecha de vencimiento ────────────────────────────────────────────── */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
        <label htmlFor="membershipEndDate" style={{ fontWeight: 500 }}>
          Vencimiento de membresía <span style={{ color: '#f44336' }}>*</span>
        </label>
        <input
          id="membershipEndDate"
          type="date"
          value={formData.membershipEndDate}
          min={minDate}
          onChange={(e) =>
            setFormData((prev) => ({ ...prev, membershipEndDate: e.target.value }))
          }
          disabled={loading}
          style={{
            padding: '10px 12px',
            fontSize: 15,
            border: `1px solid ${errors.membershipEndDate ? '#f44336' : '#ccc'}`,
            borderRadius: 6,
          }}
          aria-required="true"
          aria-invalid={!!errors.membershipEndDate}
        />
        {errors.membershipEndDate && (
          <span style={{ fontSize: 12, color: '#f44336' }} role="alert">
            {errors.membershipEndDate}
          </span>
        )}
      </div>

      {/* ── Error del servidor ──────────────────────────────────────────────── */}
      {error && (
        <div
          role="alert"
          style={{
            padding: 12,
            backgroundColor: '#fff3e0',
            border: '1px solid #ff9800',
            borderRadius: 6,
            fontSize: 14,
            color: '#e65100',
          }}
        >
          ⚠️ {error}
        </div>
      )}

      {/* ── Submit — CA-1: deshabilitado sin foto ───────────────────────────── */}
      <button
        type="submit"
        disabled={isSubmitDisabled}
        title={!formData.photo ? 'Agregá una foto para continuar' : undefined}
        style={{
          padding: '13px 0',
          fontSize: 16,
          fontWeight: 700,
          backgroundColor: isSubmitDisabled ? '#bdbdbd' : '#1976d2',
          color: '#fff',
          border: 'none',
          borderRadius: 6,
          cursor: isSubmitDisabled ? 'not-allowed' : 'pointer',
          transition: 'background-color 0.2s',
        }}
        aria-disabled={isSubmitDisabled}
      >
        {submitLabel}
      </button>
    </form>
  )
}
