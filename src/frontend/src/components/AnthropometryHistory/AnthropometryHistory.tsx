import React, { useEffect, useState } from 'react';
import {

  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Alert,
} from '@mui/material';
import { measurementService } from '@/services/measurementService';
import type { MeasurementDto } from '@/types/measurement';

interface AnthropometryHistoryProps {
  memberId: string;
}

export const AnthropometryHistory: React.FC<AnthropometryHistoryProps> = ({ memberId }) => {
  const [measurements, setMeasurements] = useState<MeasurementDto[] | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchMeasurements = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const data = await measurementService.getMeasurements(memberId);
        setMeasurements(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Error fetching measurements.');
      } finally {
        setLoading(false);
      }
    };

    fetchMeasurements();
  }, [memberId]);

  if (loading) {
    return <CircularProgress sx={{ m: 'auto', display: 'block' }} />;
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  if (!measurements || measurements.length === 0) {
    return <Typography variant="body1">Sin medidas registradas.</Typography>;
  }

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Fecha</TableCell>
            <TableCell>Peso</TableCell>
            <TableCell>% Grasa</TableCell>
            <TableCell>Pecho</TableCell>
            <TableCell>Cintura</TableCell>
            <TableCell>Cadera</TableCell>
            <TableCell>Brazo</TableCell>
            <TableCell>Pierna</TableCell>
            <TableCell>Unidades</TableCell>
            <TableCell>Notas</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {measurements.map((m) => (
            <TableRow key={m.id}>
              <TableCell>{new Date(m.recordedAt).toLocaleDateString()}</TableCell>
              <TableCell>{m.weightKg} {m.unitSystem === 'metric' ? 'kg' : 'lbs'}</TableCell>
              <TableCell>{m.bodyFatPct}%</TableCell>
              <TableCell>{m.chestCm} {m.unitSystem === 'metric' ? 'cm' : 'in'}</TableCell>
              <TableCell>{m.waistCm} {m.unitSystem === 'metric' ? 'cm' : 'in'}</TableCell>
              <TableCell>{m.hipCm} {m.unitSystem === 'metric' ? 'cm' : 'in'}</TableCell>
              <TableCell>{m.armCm} {m.unitSystem === 'metric' ? 'cm' : 'in'}</TableCell>
              <TableCell>{m.legCm} {m.unitSystem === 'metric' ? 'cm' : 'in'}</TableCell>
              <TableCell>{m.unitSystem === 'metric' ? 'Métrico' : 'Imperial'}</TableCell>
              <TableCell>{m.notes || '—'}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};