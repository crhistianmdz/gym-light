import React from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Dot,
} from 'recharts';
import {
  Box,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  type SelectChangeEvent,
  Typography,
} from '@mui/material';
import {
  MEASUREMENT_OPTIONS,
  buildChartData,
  type MeasurementKey,
} from '@/types/progressChart';
import type { MeasurementDto } from '@/types/measurement';

interface ProgressChartProps {
  measurements: MeasurementDto[];
}

interface TooltipPayload {
  payload?: { unit?: string };
  value?: number;
}

interface CustomTooltipProps {
  active?: boolean;
  payload?: TooltipPayload[];
  label?: string;
}

const CustomTooltip: React.FC<CustomTooltipProps> = ({ active, payload, label }) => {
  if (!active || !payload?.length) return null;
  const entry = payload[0];
  const unit  = entry.payload?.unit ?? '';
  return (
    <Card elevation={3} sx={{ p: 1, minWidth: 140 }}>
      <Typography variant="caption" color="text.secondary" display="block">
        {label}
      </Typography>
      <Typography variant="body2" fontWeight="bold">
        {entry.value} {unit}
      </Typography>
    </Card>
  );
};

/**
 * ProgressChart — renders a recharts LineChart for one selected measurement field.
 * Shows a single dot when there is only 1 data point (no connecting line).
 * Shows an empty-state message when there are no measurements.
 */
export const ProgressChart: React.FC<ProgressChartProps> = ({ measurements }) => {
  const [selectedKey, setSelectedKey] = React.useState<MeasurementKey>('weightKg');

  const handleChange = (e: SelectChangeEvent) => {
    setSelectedKey(e.target.value as MeasurementKey);
  };

  const selectedMeta = MEASUREMENT_OPTIONS.find((m) => m.key === selectedKey)!;
  const chartData    = buildChartData(measurements, selectedKey);
  const isSinglePoint = chartData.length === 1;

  return (
    <Card variant="outlined" sx={{ mt: 2 }}>
      <CardContent>
        {/* Header */}
        <Box display="flex" alignItems="center" justifyContent="space-between" mb={2} flexWrap="wrap" gap={1}>
          <Typography variant="h6" component="h2">
            Evolución: {selectedMeta.label}
          </Typography>

          {/* Variable selector */}
          <FormControl size="small" sx={{ minWidth: 160 }}>
            <InputLabel id="measure-select-label">Variable</InputLabel>
            <Select
              labelId="measure-select-label"
              value={selectedKey}
              label="Variable"
              onChange={handleChange}
            >
              {MEASUREMENT_OPTIONS.map((opt) => (
                <MenuItem key={opt.key} value={opt.key}>
                  {opt.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>

        {/* Empty state */}
        {chartData.length === 0 && (
          <Box display="flex" justifyContent="center" alignItems="center" minHeight={200}>
            <Typography variant="body2" color="text.secondary">
              Aún no hay medidas registradas.
            </Typography>
          </Box>
        )}

        {/* Chart */}
        {chartData.length > 0 && (
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 8 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e0e0e0" />
              <XAxis
                dataKey="date"
                tick={{ fontSize: 12 }}
                tickLine={false}
              />
              <YAxis
                tick={{ fontSize: 12 }}
                tickLine={false}
                axisLine={false}
                width={45}
              />
              <Tooltip content={<CustomTooltip />} />
              <Line
                type="monotone"
                dataKey="value"
                stroke="#1976d2"
                strokeWidth={isSinglePoint ? 0 : 2}
                dot={<Dot r={5} fill="#1976d2" stroke="#fff" strokeWidth={2} />}
                activeDot={{ r: 7, fill: '#1565c0' }}
                isAnimationActive={!isSinglePoint}
              />
            </LineChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  );
};
