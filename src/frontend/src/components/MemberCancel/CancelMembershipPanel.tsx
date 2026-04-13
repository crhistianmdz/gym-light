import { useState } from 'react'
import {
  Alert, Box, Button, CircularProgress, Modal, Typography,
} from '@mui/material'
import CancelIcon from '@mui/icons-material/CancelOutlined'
import { cancelMembership } from '@/services/cancelService'

interface Props {
  memberId: string
  memberStatus: 'Active' | 'Frozen' | 'Expired' | 'Cancelled'
  membershipEndDate: string
  onSuccess: () => void
}

export function CancelMembershipPanel({ memberId, memberStatus, membershipEndDate, onSuccess }: Props) {
  const [state, setState] = useState<'idle' | 'confirm' | 'loading' | 'success' | 'error' | 'offline'>('idle')
  const [error, setError] = useState<string | null>(null)
  
  const handleCancel = async () => {
    setState('loading')
    setError(null)

    try {
      await cancelMembership(memberId)
      setState('success')
      onSuccess()
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      if (error.message === 'OFFLINE_QUEUED') {
        setState('offline');
      } else {
        setError(error.message ?? 'Unexpected error occurred');
        setState('error');
      }
    }
  }

  const modalConfirm = (
    <Modal open={state === 'confirm'} onClose={() => setState('idle')}>
      <Box textAlign="center">
        <Typography variant="h6">Cancelar Membresía</Typography>
        <Typography variant="body2">
          Seguirás teniendo acceso hasta {membershipEndDate}. No habrá reembolsos.
        </Typography>
        <Button onClick={handleCancel} variant="contained" color="error" startIcon={<CancelIcon />}>Confirmar</Button>
        <Button onClick={() => setState('idle')}>Cancelar</Button>
      </Box>
    </Modal>
  )

  const alertOfflineQueued = state === 'offline' && <Alert severity="warning">Cancelación pendiente de sincronización.</Alert>
  const alertError = state === 'error' && <Alert severity="error">{error}</Alert>
  const alertSuccess = state === 'success' && <Alert severity="success">Membresía cancelada con acceso hasta {membershipEndDate}</Alert>

  return (
    <Box>
      {alertOfflineQueued}{alertError}{alertSuccess}
      {modalConfirm}

      <Button
        disabled={memberStatus === 'Expired' || state === 'loading'}
        startIcon={state === 'loading' ? <CircularProgress size={15} /> : <CancelIcon />}
        variant="outlined"
        color="error"
        onClick={() => setState('confirm')}
      >
        Cancelar Membresía
      </Button>
    </Box>
  )
}