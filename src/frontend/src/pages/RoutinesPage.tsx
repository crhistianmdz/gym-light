import { useState, useEffect } from 'react'
import {
  Box, Container, Typography, Button, Dialog,
  DialogTitle, DialogContent, Card, CardContent,
  CardHeader, Chip, CircularProgress, Alert, Divider
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import FitnessCenterIcon from '@mui/icons-material/FitnessCenter'
import { getRoutines } from '../services/routineService'
import { RoutineBuilder } from '../components/RoutineBuilder/RoutineBuilder'
import type { Routine } from '../types/routine'

export function RoutinesPage() {
  const [routines, setRoutines] = useState<Routine[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [openBuilder, setOpenBuilder] = useState(false)

  const loadRoutines = () => {
    setLoading(true)
    getRoutines()
      .then(setRoutines)
      .catch(() => setError('No se pudieron cargar las rutinas.'))
      .finally(() => setLoading(false))
  }

  useEffect(loadRoutines, [])

  const handleCreated = () => {
    setOpenBuilder(false)
    loadRoutines()
  }

  return (
    <Container maxWidth="md" sx={{ py: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FitnessCenterIcon color="primary" />
          <Typography variant="h5">Rutinas</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpenBuilder(true)}>
          Nueva Rutina
        </Button>
      </Box>

      {loading && <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}><CircularProgress /></Box>}
      {error && <Alert severity="error">{error}</Alert>}

      {!loading && routines.map(routine => (
        <Card key={routine.id} sx={{ mb: 2 }}>
          <CardHeader
            title={routine.name}
            subheader={`${routine.exercises.length} ejercicios`}
            action={
              routine.isPublic
                ? <Chip label="Pública" color="primary" size="small" />
                : <Chip label="Privada" size="small" />
            }
          />
          {routine.description && (
            <CardContent>
              <Typography variant="body2" color="text.secondary">{routine.description}</Typography>
            </CardContent>
          )}
        </Card>
      ))}

      {!loading && routines.length === 0 && !error && (
        <Alert severity="info">No hay rutinas todavía. ¡Creá la primera!</Alert>
      )}

      <Dialog open={openBuilder} onClose={() => setOpenBuilder(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Nueva Rutina</DialogTitle>
        <Divider />
        <DialogContent>
          <RoutineBuilder onCreated={handleCreated} />
        </DialogContent>
      </Dialog>
    </Container>
  )
}