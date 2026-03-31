import { useState } from 'react'
import {
  Box, TextField, FormControlLabel, Switch, Button,
  List, ListItem, ListItemText, IconButton, Typography,
  CircularProgress, Alert
} from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import { ExerciseSelector } from './ExerciseSelector'
import { createRoutine } from '../../services/routineService'
import type { CreateRoutineExerciseRequest } from '../../types/routine'

interface ExerciseItem extends CreateRoutineExerciseRequest {
  displayName: string
}

interface Props {
  onCreated?: () => void
}

export function RoutineBuilder({ onCreated }: Props) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [isPublic, setIsPublic] = useState(false)
  const [exercises, setExercises] = useState<ExerciseItem[]>([])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const handleAddExercise = (ex: ExerciseItem) => {
    setExercises(prev => [...prev, { ...ex, order: prev.length + 1 }])
  }

  const handleRemoveExercise = (index: number) => {
    setExercises(prev => prev.filter((_, i) => i !== index).map((e, i) => ({ ...e, order: i + 1 })))
  }

  const handleSubmit = async () => {
    if (!name.trim() || exercises.length === 0) return
    setSaving(true)
    setError(null)
    try {
      await createRoutine({
        name: name.trim(),
        description: description.trim() || undefined,
        isPublic,
        exercises: exercises.map(({ displayName: _, ...ex }) => ex)
      })
      setSuccess(true)
      onCreated?.()
    } catch {
      setError('Error al crear la rutina. Intentá nuevamente.')
    } finally {
      setSaving(false)
    }
  }

  if (success) return <Alert severity="success">¡Rutina creada exitosamente!</Alert>

  return (
    <Box component="form" noValidate>
      <TextField
        label="Nombre de la rutina"
        value={name}
        onChange={e => setName(e.target.value)}
        fullWidth
        required
        sx={{ mb: 2 }}
      />

      <TextField
        label="Descripción (opcional)"
        value={description}
        onChange={e => setDescription(e.target.value)}
        fullWidth
        multiline
        rows={2}
        sx={{ mb: 2 }}
      />

      <FormControlLabel
        control={<Switch checked={isPublic} onChange={e => setIsPublic(e.target.checked)} />}
        label="Rutina pública (visible para todos los entrenadores)"
        sx={{ mb: 2 }}
      />

      <Typography variant="subtitle1" gutterBottom>Ejercicios</Typography>

      <ExerciseSelector onAdd={handleAddExercise} />

      {exercises.length > 0 && (
        <List dense>
          {exercises.map((ex, i) => (
            <ListItem
              key={i}
              secondaryAction={
                <IconButton edge="end" onClick={() => handleRemoveExercise(i)}>
                  <DeleteIcon />
                </IconButton>
              }
              sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, mb: 0.5 }}
            >
              <ListItemText
                primary={`${ex.order}. ${ex.displayName}`}
                secondary={`${ex.sets} series × ${ex.reps} reps${ex.notes ? ` — ${ex.notes}` : ''}`}
              />
            </ListItem>
          ))}
        </List>
      )}

      {error && <Alert severity="error" sx={{ mb: 1 }}>{error}</Alert>}

      <Button
        variant="contained"
        fullWidth
        onClick={handleSubmit}
        disabled={saving || !name.trim() || exercises.length === 0}
        startIcon={saving ? <CircularProgress size={18} /> : undefined}
        sx={{ mt: 2 }}
      >
        {saving ? 'Creando...' : 'Crear Rutina'}
      </Button>
    </Box>
  )
}