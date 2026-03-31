import type { MeasurementDto, UnitSystem } from '@/types/measurement';

/**
 * Keys of numeric measurement fields available for charting.
 */
export type MeasurementKey =
  | 'weightKg'
  | 'bodyFatPct'
  | 'chestCm'
  | 'waistCm'
  | 'hipCm'
  | 'armCm'
  | 'legCm';

export interface MeasurementMeta {
  key: MeasurementKey;
  label: string;
  unitMetric: string;
  unitImperial: string;
}

/**
 * All 7 measurement fields with their display labels and units.
 */
export const MEASUREMENT_OPTIONS: MeasurementMeta[] = [
  { key: 'weightKg',   label: 'Peso',         unitMetric: 'kg',  unitImperial: 'lbs' },
  { key: 'bodyFatPct', label: '% Grasa',       unitMetric: '%',   unitImperial: '%'   },
  { key: 'chestCm',    label: 'Pecho',         unitMetric: 'cm',  unitImperial: 'in'  },
  { key: 'waistCm',    label: 'Cintura',       unitMetric: 'cm',  unitImperial: 'in'  },
  { key: 'hipCm',      label: 'Cadera',        unitMetric: 'cm',  unitImperial: 'in'  },
  { key: 'armCm',      label: 'Brazo',         unitMetric: 'cm',  unitImperial: 'in'  },
  { key: 'legCm',      label: 'Pierna',        unitMetric: 'cm',  unitImperial: 'in'  },
];

/**
 * Returns the display unit for a measurement field given its UnitSystem.
 */
export function getUnit(meta: MeasurementMeta, unitSystem: UnitSystem): string {
  return unitSystem === 'metric' ? meta.unitMetric : meta.unitImperial;
}

/**
 * Formats an ISO date string to DD/MM/YYYY for chart X axis.
 */
export function formatDate(isoDate: string): string {
  const d = new Date(isoDate);
  const day   = String(d.getDate()).padStart(2, '0');
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const year  = d.getFullYear();
  return `${day}/${month}/${year}`;
}

/**
 * Transforms raw measurements into recharts-compatible data points
 * for the selected field.
 */
export interface ChartDataPoint {
  date: string;         // formatted date for X axis
  value: number;        // raw numeric value (as recorded)
  unit: string;         // unit string for this specific point
  rawDate: string;      // ISO string for sorting/tooltip
}

export function buildChartData(
  measurements: MeasurementDto[],
  fieldKey: MeasurementKey,
): ChartDataPoint[] {
  const meta = MEASUREMENT_OPTIONS.find((m) => m.key === fieldKey)!;
  return measurements.map((m) => ({
    date:    formatDate(m.recordedAt),
    value:   m[fieldKey],
    unit:    getUnit(meta, m.unitSystem),
    rawDate: m.recordedAt,
  }));
}
