import { useEffect, useState } from 'react'
import {
  Box, Card, CardContent, CardHeader, CircularProgress,
  Typography, Accordion, AccordionSummary, AccordionDetails, Alert
} from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import { getMemberRoutines } from '../../services/routineService'
import { WorkoutLogPanel } from './WorkoutLogPanel'
import type { RoutineAssignment, WorkoutEntry, RoutineExercise } from '../../types/routine'

interface Props {
  memberId: string
}

function exerciseToEntry(ex: RoutineExercise): WorkoutEntry {
  return {
    routineExerciseId: ex.id ?? '',
    exerciseName: ex.customName ?? '—',
    sets: ex.sets,
    reps: ex.reps,
    completed: false
  }
}

export function MemberRoutineView({ memberId }: Props) {
  const [assignments, setAssignments] = useState<RoutineAssignment[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getMemberRoutines(memberId)
      .then(setAssignments)
      .catch(() => setError('No se pudieron cargar las rutinas.'))
      .finally(() => setLoading(false))
  }, [memberId])

  if (loading) return <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}><CircularProgress /></Box>
  if (error) return <Alert severity="error">{error}</Alert>
  if (assignments.length === 0) return <Alert severity="info">No tenés rutinas asignadas todavía.</Alert>

  return (
    <Box>
      {assignments.map(assignment => (
        <Card key={assignment.id} sx={{ mb: 2 }}>
          <CardHeader
            title={assignment.routineName}
            subheader={`Asignada el ${new Date(assignment.assignedAt).toLocaleDateString('es-AR')}`}
          />
          <CardContent>
            <Accordion>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography>Iniciar Sesión</Typography>
              </AccordionSummary>
              <AccordionDetails>
                <WorkoutLogPanel
                  assignment={assignment}
                  exercises={[]}
                />
              </AccordionDetails>
            </Accordion>
          </CardContent>
        </Card>
      ))}
    </Box>
  )
}