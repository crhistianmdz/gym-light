import { useState, useEffect } from 'react'
import {
  Autocomplete, TextField, Box, Chip, Button, Typography
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import { getExerciseCatalog } from '../../services/routineService'
import type { ExerciseCatalogItem, CreateRoutineExerciseRequest } from '../../types/routine'

interface Props {
  onAdd: (exercise: CreateRoutineExerciseRequest & { displayName: string }) => void
}

export function ExerciseSelector({ onAdd }: Props) {
  const [catalog, setCatalog] = useState<ExerciseCatalogItem[]>([])
  const [selected, setSelected] = useState<ExerciseCatalogItem | null>(null)
  const [customName, setCustomName] = useState('')
  const [sets, setSets] = useState(3)
  const [reps, setReps] = useState(10)
  const [notes, setNotes] = useState('')
  const [order, setOrder] = useState(1)

  useEffect(() => { getExerciseCatalog().then(setCatalog) }, [])

  const handleAdd = () => {
    if (!selected && !customName.trim()) return

    onAdd({
      exerciseCatalogId: selected?.id,
      customName: selected ? undefined : customName.trim(),
      displayName: selected?.name ?? customName.trim(),
      order,
      sets,
      reps,
      notes: notes.trim() || undefined
    })

    setSelected(null)
    setCustomName('')
    setNotes('')
    setOrder(prev => prev + 1)
  }

  return (
    <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 2, mb: 2 }}>
      <Typography variant="subtitle2" gutterBottom>Agregar ejercicio</Typography>

      <Autocomplete
        options={catalog}
        getOptionLabel={o => o.name}
        value={selected}
        onChange={(_, val) => { setSelected(val); setCustomName('') }}
        renderInput={params => <TextField {...params} label="Buscar en catálogo" size="small" />}
        sx={{ mb: 1 }}
      />

      {!selected && (
        <TextField
          label="Nombre personalizado"
          value={customName}
          onChange={e => setCustomName(e.target.value)}
          size="small"
          fullWidth
          sx={{ mb: 1 }}
          helperText="Si no encontrás el ejercicio en el catálogo, escribí el nombre"
        />
      )}

      <Box sx={{ display: 'flex', gap: 1, mb: 1 }}>
        <TextField
          label="Series"
          type="number"
          value={sets}
          onChange={e => setSets(Number(e.target.value))}
          size="small"
          inputProps={{ min: 1 }}
          sx={{ flex: 1 }}
        />
        <TextField
          label="Reps"
          type="number"
          value={reps}
          onChange={e => setReps(Number(e.target.value))}
          size="small"
          inputProps={{ min: 1 }}
          sx={{ flex: 1 }}
        />
      </Box>

      <TextField
        label="Notas (opcional)"
        value={notes}
        onChange={e => setNotes(e.target.value)}
        size="small"
        fullWidth
        sx={{ mb: 1 }}
      />

      <Button
        variant="outlined"
        startIcon={<AddIcon />}
        onClick={handleAdd}
        disabled={!selected && !customName.trim()}
        fullWidth
      >
        Agregar ejercicio
      </Button>
    </Box>
  )
}