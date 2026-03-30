/**
 * FreezeHistoryList — HU-07
 *
 * Muestra el historial de congelamientos de un socio.
 * Solo Admin y Owner pueden ver este componente.
 *
 * Props:
 *   memberId — Id del socio cuyo historial se muestra
 */

import React, { useEffect, useState } from 'react'
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import HistoryIcon from '@mui/icons-material/History'
import { getFreezeHistory } from '@/services/freezeService'
import type { MembershipFreeze } from '@/types/freeze'

interface Props {
  memberId: string
}

export function FreezeHistoryList({ memberId }: Props) {
  const [freezes, setFreezes] = useState<MembershipFreeze[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        const data = await getFreezeHistory(memberId)
        if (!cancelled) setFreezes(data)
      } catch (err) {
        if (!cancelled)
          setError(err instanceof Error ? err.message : 'Error al cargar el historial.')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void load()
    return () => { cancelled = true }
  }, [memberId])

  // Congelamientos en el año actual (para mostrar el contador HU-07 R1)
  const currentYear   = new Date().getFullYear()
  const freezesThisYear = freezes.filter(
    f => new Date(f.startDate).getFullYear() === currentYear,
  ).length

  return (
    <Box>
      {/* Header */}
      <Stack direction="row" alignItems="center" spacing={1} mb={2}>
        <HistoryIcon color="action" />
        <Typography variant="subtitle1">Historial de Congelamientos</Typography>
        <Chip
          label={`${freezesThisYear}/4 este año`}
          color={freezesThisYear >= 4 ? 'error' : freezesThisYear >= 3 ? 'warning' : 'default'}
          size="small"
          title="Límite HU-07: máximo 4 congelamientos por año calendario"
        />
      </Stack>

      {/* Estado de carga */}
      {loading && (
        <Box display="flex" justifyContent="center" py={3}>
          <CircularProgress size={28} />
        </Box>
      )}

      {/* Error */}
      {!loading && error && (
        <Alert severity="error">{error}</Alert>
      )}

      {/* Sin historial */}
      {!loading && !error && freezes.length === 0 && (
        <Typography variant="body2" color="text.secondary">
          Este socio no tiene congelamientos registrados.
        </Typography>
      )}

      {/* Tabla de historial */}
      {!loading && !error && freezes.length > 0 && (
        <Paper variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Inicio</TableCell>
                <TableCell>Fin</TableCell>
                <TableCell align="right">Días</TableCell>
                <TableCell>Registrado</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {freezes.map(f => {
                const today    = new Date()
                const start    = new Date(f.startDate)
                const end      = new Date(f.endDate)
                const isActive = start <= today && today <= end

                return (
                  <TableRow
                    key={f.id}
                    sx={isActive ? { backgroundColor: 'action.hover' } : undefined}
                  >
                    <TableCell>
                      {f.startDate}
                      {isActive && (
                        <Chip
                          label="activo"
                          color="info"
                          size="small"
                          sx={{ ml: 1 }}
                        />
                      )}
                    </TableCell>
                    <TableCell>{f.endDate}</TableCell>
                    <TableCell align="right">{f.durationDays}</TableCell>
                    <TableCell>
                      {new Date(f.createdAt).toLocaleDateString('es-AR', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </Paper>
      )}
    </Box>
  )
}
