import React, { useState } from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Snackbar,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
  Alert,
} from '@mui/material';
import { measurementService } from '@/services/measurementService';
import type { AddMeasurementRequest, UnitSystem } from '@/types/measurement';

interface AnthropometryFormProps {
  memberId: string;
  onSuccess?: () => void;
}

export const AnthropometryForm: React.FC<AnthropometryFormProps> = ({ memberId, onSuccess }) => {
  const [unitSystem, setUnitSystem] = useState<UnitSystem>('metric');
  const [formData, setFormData] = useState({
    weight: '',
    bodyFat: '',
    chest: '',
    waist: '',
    hip: '',
    arm: '',
    leg: '',
    notes: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleUnitSystemChange = (_event: React.MouseEvent<HTMLElement>, newUnit: UnitSystem | null) => {
    if (newUnit) setUnitSystem(newUnit);
  };

  const validateForm = (): string | null => {
    for (const [key, value] of Object.entries(formData)) {
      if (key !== 'notes' && (!value || isNaN(Number(value)) || Number(value) <= 0)) {
        return `Invalid value for ${key}. All fields must be greater than 0.`;
      }
    }
    return null;
  };

  const handleSubmit = async () => {
    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    setLoading(true);

    const request: AddMeasurementRequest = {
      clientGuid: crypto.randomUUID(),
      recordedAt: new Date().toISOString(),
      weightKg: parseFloat(formData.weight),
      bodyFatPct: parseFloat(formData.bodyFat),
      chestCm: parseFloat(formData.chest),
      waistCm: parseFloat(formData.waist),
      hipCm: parseFloat(formData.hip),
      armCm: parseFloat(formData.arm),
      legCm: parseFloat(formData.leg),
      unitSystem,
      notes: formData.notes || undefined,
    };

    try {
      await measurementService.addMeasurement(memberId, request);
      setSuccess(true);
      setFormData({
        weight: '',
        bodyFat: '',
        chest: '',
        waist: '',
        hip: '',
        arm: '',
        leg: '',
        notes: '',
      });
      if (onSuccess) onSuccess();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save measurement.');
    } finally {
      setLoading(false);
    }
  };

  const fieldLabels = unitSystem === 'metric'
    ? ['Peso (kg)', '% Grasa corporal', 'Pecho (cm)', 'Cintura (cm)', 'Cadera (cm)', 'Brazo (cm)', 'Pierna (cm)']
    : ['Peso (lbs)', '% Grasa corporal', 'Pecho (in)', 'Cintura (in)', 'Cadera (in)', 'Brazo (in)', 'Pierna (in)'];

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        Registrar Antropometría
      </Typography>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <ToggleButtonGroup
        value={unitSystem}
        exclusive
        onChange={handleUnitSystemChange}
        size="small"
        sx={{ mb: 2 }}
      >
        <ToggleButton value="metric">Métrico (kg, cm)</ToggleButton>
        <ToggleButton value="imperial">Imperial (lbs, in)</ToggleButton>
      </ToggleButtonGroup>

      {fieldLabels.map((label, index) => (
        <TextField
          key={label}
          label={label}
          name={['weight', 'bodyFat', 'chest', 'waist', 'hip', 'arm', 'leg'][index]}
          type="number"
          value={(formData as any)[['weight', 'bodyFat', 'chest', 'waist', 'hip', 'arm', 'leg'][index]]}
          onChange={handleInputChange}
          size="small"
          margin="normal"
          fullWidth
        />
      ))}

      <TextField
        label="Notas"
        name="notes"
        value={formData.notes}
        onChange={handleInputChange}
        size="small"
        multiline
        rows={3}
        margin="normal"
        fullWidth
      />

      <Button
        variant="contained"
        color="primary"
        disabled={loading}
        onClick={handleSubmit}
        fullWidth
      >
        {loading ? <CircularProgress size={20} color="inherit" /> : 'Guardar medidas'}
      </Button>

      <Snackbar
        open={success}
        autoHideDuration={4000}
        onClose={() => setSuccess(false)}
        message="Medidas guardadas exitosamente"
      />
    </Box>
  );
};