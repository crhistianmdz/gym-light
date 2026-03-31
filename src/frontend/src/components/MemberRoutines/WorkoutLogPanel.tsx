import { useState } from 'react'
import {
  Box, Button, CircularProgress, Typography, Alert
} from '@mui/material'
import FitnessCenterIcon from '@mui/icons-material/FitnessCenter'
import { ExerciseRow } from './ExerciseRow'
import { createWorkoutLog } from '../../services/routineService'
import type { RoutineAssignment, WorkoutEntry } from '../../types/routine'

interface Props {
  assignment: RoutineAssignment
  exercises: WorkoutEntry[]
}

export function WorkoutLogPanel({ assignment, exercises: initialExercises }: Props) {
  const [entries, setEntries] = useState<WorkoutEntry[]>(initialExercises)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleToggle = (index: number, completed: boolean) => {
    setEntries(prev => prev.map((e, i) =>
      i === index
        ? { ...e, completed, completedAt: completed ? new Date().toISOString() : undefined }
        : e
    ))
  }

  const handleSave = async () => {
    setSaving(true)
    setError(null)
    try {
      await createWorkoutLog({
        assignmentId: assignment.id,
        sessionDate: new Date().toISOString(),
        clientGuid: crypto.randomUUID(),
        entries: entries.map(e => ({
          routineExerciseId: e.routineExerciseId,
          completed: e.completed,
          notes: e.notes
        }))
      })
      setSaved(true)
    } catch (err) {
      setError('Error al guardar. Se guardó localmente para sincronizar después.')
      setSaved(true) // El servicio ya guardó offline
    } finally {
      setSaving(false)
    }
  }

  const completedCount = entries.filter(e => e.completed).length

  if (saved) {
    return (
      <Alert severity="success" sx={{ mt: 2 }}>
        ¡Sesión registrada! {completedCount}/{entries.length} ejercicios completados.
      </Alert>
    )
  }

  return (
    <Box>
      <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
        {completedCount}/{entries.length} ejercicios completados
      </Typography>

      {entries.map((entry, i) => (
        <ExerciseRow
          key={entry.routineExerciseId}
          entry={entry}
          index={i}
          onChange={handleToggle}
          disabled={saving}
        />
      ))}

      {error && <Alert severity="warning" sx={{ mt: 1 }}>{error}</Alert>}

      <Button
        variant="contained"
        color="primary"
        fullWidth
        startIcon={saving ? <CircularProgress size={18} /> : <FitnessCenterIcon />}
        onClick={handleSave}
        disabled={saving}
        sx={{ mt: 2 }}
      >
        {saving ? 'Guardando...' : 'Registrar Sesión'}
      </Button>
    </Box>
  )
}