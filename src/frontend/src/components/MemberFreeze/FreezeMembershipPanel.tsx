/**
 * FreezeMembershipPanel — HU-07
 *
 * Panel para Admin/Owner que permite congelar o descongelar
 * la membresía de un socio.
 *
 * Validaciones cliente (espejo de las del servidor):
 *   - StartDate >= hoy
 *   - Duración mínima 7 días
 *
 * Props:
 *   memberId  — Id del socio
 *   memberStatus — estado actual ('Active' | 'Frozen' | 'Expired')
 *   membershipEndDate — fecha de vencimiento actual
 *   onSuccess — callback para refrescar el componente padre tras la operación
 */

import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Chip,
  Divider,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import AcUnitIcon from '@mui/icons-material/AcUnit'
import WbSunnyIcon from '@mui/icons-material/WbSunny'
import { freezeMember, unfreezeMember } from '@/services/freezeService'

interface Props {
  memberId: string
  memberStatus: 'Active' | 'Frozen' | 'Expired'
  membershipEndDate: string
  onSuccess: () => void
}

export function FreezeMembershipPanel({
  memberId,
  memberStatus,
  membershipEndDate,
  onSuccess,
}: Props) {
  const today = new Date().toISOString().split('T')[0] // 'YYYY-MM-DD'

  const [startDate, setStartDate] = useState(today)
  const [endDate, setEndDate]     = useState('')
  const [loading, setLoading]     = useState(false)
  const [error, setError]         = useState<string | null>(null)
  const [success, setSuccess]     = useState<string | null>(null)

  // ── Validación local (CA heurístico, el servidor también valida) ────────────
  const validateForm = (): string | null => {
    if (!startDate) return 'La fecha de inicio es obligatoria.'
    if (!endDate)   return 'La fecha de fin es obligatoria.'

    const start    = new Date(startDate)
    const end      = new Date(endDate)
    const todayObj = new Date(today)

    if (start < todayObj) return 'La fecha de inicio no puede ser anterior a hoy.'
    if (end <= start)     return 'La fecha de fin debe ser posterior a la fecha de inicio.'

    const durationDays = Math.floor((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1
    if (durationDays < 7)
      return `La duración mínima es 7 días. El rango indicado tiene ${durationDays} día(s).`

    return null
  }

  const handleFreeze = async () => {
    const validationError = validateForm()
    if (validationError) {
      setError(validationError)
      return
    }

    setLoading(true)
    setError(null)
    setSuccess(null)

    try {
      const freeze = await freezeMember(memberId, { startDate, endDate })
      const durationLabel = freeze.durationDays === 1 ? 'día' : 'días'
      setSuccess(
        `Membresía congelada por ${freeze.durationDays} ${durationLabel}. ` +
        `Vencimiento extendido automáticamente.`,
      )
      onSuccess()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al congelar la membresía.')
    } finally {
      setLoading(false)
    }
  }

  const handleUnfreeze = async () => {
    setLoading(true)
    setError(null)
    setSuccess(null)

    try {
      await unfreezeMember(memberId)
      setSuccess('Membresía descongelada correctamente.')
      onSuccess()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al descongelar la membresía.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box sx={{ p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
      {/* Header */}
      <Stack direction="row" alignItems="center" spacing={1} mb={2}>
        <AcUnitIcon color="primary" />
        <Typography variant="h6">Congelamiento de Membresía</Typography>
        <Chip
          label={memberStatus}
          color={
            memberStatus === 'Active' ? 'success'
            : memberStatus === 'Frozen' ? 'info'
            : 'error'
          }
          size="small"
        />
      </Stack>

      {/* Vencimiento actual */}
      <Typography variant="body2" color="text.secondary" mb={2}>
        Vencimiento actual: <strong>{membershipEndDate}</strong>
      </Typography>

      {/* Alertas */}
      {error   && <Alert severity="error"   sx={{ mb: 2 }}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }}>{success}</Alert>}

      <Divider sx={{ mb: 2 }} />

      {/* Formulario — solo visible si el socio está Active */}
      {memberStatus === 'Active' && (
        <>
          <Typography variant="subtitle2" gutterBottom>
            Aplicar congelamiento (mínimo 7 días)
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2}>
            <TextField
              label="Fecha de inicio"
              type="date"
              size="small"
              value={startDate}
              onChange={e => setStartDate(e.target.value)}
              inputProps={{ min: today }}
              InputLabelProps={{ shrink: true }}
              fullWidth
            />
            <TextField
              label="Fecha de fin"
              type="date"
              size="small"
              value={endDate}
              onChange={e => setEndDate(e.target.value)}
              inputProps={{ min: startDate }}
              InputLabelProps={{ shrink: true }}
              fullWidth
            />
          </Stack>
          <Button
            variant="contained"
            color="primary"
            startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <AcUnitIcon />}
            onClick={handleFreeze}
            disabled={loading || !startDate || !endDate}
            fullWidth
          >
            Congelar Membresía
          </Button>
        </>
      )}

      {/* Botón descongelar — solo si está Frozen */}
      {memberStatus === 'Frozen' && (
        <>
          <Alert severity="info" sx={{ mb: 2 }}>
            La membresía está actualmente congelada. El acceso está bloqueado.
          </Alert>
          <Button
            variant="contained"
            color="warning"
            startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <WbSunnyIcon />}
            onClick={handleUnfreeze}
            disabled={loading}
            fullWidth
          >
            Descongelar Membresía
          </Button>
        </>
      )}

      {/* Socio expirado */}
      {memberStatus === 'Expired' && (
        <Alert severity="warning">
          La membresía está vencida. No se puede congelar un socio con membresía vencida.
        </Alert>
      )}
    </Box>
  )
}
