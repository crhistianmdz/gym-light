import { Box, Checkbox, FormControlLabel, Typography, Chip } from '@mui/material'
import type { WorkoutEntry } from '../../types/routine'

interface Props {
  entry: WorkoutEntry
  index: number
  onChange: (index: number, completed: boolean) => void
  disabled?: boolean
}

export function ExerciseRow({ entry, index, onChange, disabled }: Props) {
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        p: 1.5,
        borderRadius: 1,
        bgcolor: entry.completed ? 'success.light' : 'background.paper',
        border: '1px solid',
        borderColor: entry.completed ? 'success.main' : 'divider',
        mb: 1,
        transition: 'background-color 0.2s'
      }}
    >
      <FormControlLabel
        control={
          <Checkbox
            checked={entry.completed}
            onChange={(e) => onChange(index, e.target.checked)}
            disabled={disabled}
            color="success"
          />
        }
        label={
          <Box>
            <Typography variant="body1" sx={{ textDecoration: entry.completed ? 'line-through' : 'none' }}>
              {entry.exerciseName}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {entry.sets} sets × {entry.reps} reps
            </Typography>
          </Box>
        }
      />
      {entry.completed && entry.completedAt && (
        <Chip
          label={new Date(entry.completedAt).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}
          size="small"
          color="success"
          variant="outlined"
        />
      )}
    </Box>
  )
}