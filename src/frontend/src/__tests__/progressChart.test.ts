import { describe, it, expect } from 'vitest';
import {
  buildChartData,
  formatDate,
  getUnit,
  MEASUREMENT_OPTIONS,
  type ChartDataPoint,
} from '@/types/progressChart';
import type { MeasurementDto } from '@/types/measurement';

// ─── Fixtures ────────────────────────────────────────────────────────────────

const baseMeasurement: MeasurementDto = {
  id:           'abc-1',
  memberId:     'member-1',
  recordedById: 'trainer-1',
  recordedAt:   '2026-01-15T10:00:00Z',
  weightKg:     75.5,
  bodyFatPct:   18.0,
  chestCm:      100.0,
  waistCm:       80.0,
  hipCm:         95.0,
  armCm:         35.0,
  legCm:         55.0,
  unitSystem:   'metric',
  clientGuid:   'guid-1',
};

const imperialMeasurement: MeasurementDto = {
  ...baseMeasurement,
  id:         'abc-2',
  recordedAt: '2026-02-10T08:00:00Z',
  weightKg:   165.0, // stored as lbs (as-recorded)
  unitSystem: 'imperial',
  clientGuid: 'guid-2',
};

// ─── formatDate ───────────────────────────────────────────────────────────────

describe('formatDate', () => {
  it('formats ISO date to DD/MM/YYYY', () => {
    expect(formatDate('2026-01-15T10:00:00Z')).toBe('15/01/2026');
  });

  it('pads single-digit day and month', () => {
    expect(formatDate('2026-03-05T00:00:00Z')).toBe('05/03/2026');
  });
});

// ─── getUnit ─────────────────────────────────────────────────────────────────

describe('getUnit', () => {
  const weightMeta = MEASUREMENT_OPTIONS.find((m) => m.key === 'weightKg')!;
  const chestMeta  = MEASUREMENT_OPTIONS.find((m) => m.key === 'chestCm')!;
  const fatMeta    = MEASUREMENT_OPTIONS.find((m) => m.key === 'bodyFatPct')!;

  it('returns "kg" for weight metric', () => {
    expect(getUnit(weightMeta, 'metric')).toBe('kg');
  });

  it('returns "lbs" for weight imperial', () => {
    expect(getUnit(weightMeta, 'imperial')).toBe('lbs');
  });

  it('returns "cm" for chest metric', () => {
    expect(getUnit(chestMeta, 'metric')).toBe('cm');
  });

  it('returns "in" for chest imperial', () => {
    expect(getUnit(chestMeta, 'imperial')).toBe('in');
  });

  it('returns "%" for bodyFatPct regardless of unit system', () => {
    expect(getUnit(fatMeta, 'metric')).toBe('%');
    expect(getUnit(fatMeta, 'imperial')).toBe('%');
  });
});

// ─── buildChartData ───────────────────────────────────────────────────────────

describe('buildChartData', () => {
  it('returns empty array when no measurements provided', () => {
    const result = buildChartData([], 'weightKg');
    expect(result).toHaveLength(0);
  });

  it('maps a single metric measurement correctly', () => {
    const result: ChartDataPoint[] = buildChartData([baseMeasurement], 'weightKg');
    expect(result).toHaveLength(1);
    expect(result[0].value).toBe(75.5);
    expect(result[0].unit).toBe('kg');
    expect(result[0].date).toBe('15/01/2026');
    expect(result[0].rawDate).toBe('2026-01-15T10:00:00Z');
  });

  it('maps an imperial measurement with correct unit', () => {
    const result = buildChartData([imperialMeasurement], 'weightKg');
    expect(result[0].value).toBe(165.0);
    expect(result[0].unit).toBe('lbs');
  });

  it('handles mixed unit systems — each point carries its own unit', () => {
    const result = buildChartData([baseMeasurement, imperialMeasurement], 'weightKg');
    expect(result[0].unit).toBe('kg');
    expect(result[1].unit).toBe('lbs');
  });

  it('maps all 7 fields correctly', () => {
    const fields: Array<keyof typeof baseMeasurement> = [
      'weightKg', 'bodyFatPct', 'chestCm', 'waistCm', 'hipCm', 'armCm', 'legCm',
    ];
    for (const field of fields) {
      const result = buildChartData([baseMeasurement], field as Parameters<typeof buildChartData>[1]);
      expect(result[0].value).toBe(baseMeasurement[field]);
    }
  });

  it('returns one point per measurement', () => {
    const result = buildChartData([baseMeasurement, imperialMeasurement], 'chestCm');
    expect(result).toHaveLength(2);
  });
});

// ─── MEASUREMENT_OPTIONS ─────────────────────────────────────────────────────

describe('MEASUREMENT_OPTIONS', () => {
  it('contains exactly 7 options', () => {
    expect(MEASUREMENT_OPTIONS).toHaveLength(7);
  });

  it('has unique keys', () => {
    const keys = MEASUREMENT_OPTIONS.map((m) => m.key);
    const unique = new Set(keys);
    expect(unique.size).toBe(7);
  });

  it('all options have non-empty labels and units', () => {
    for (const opt of MEASUREMENT_OPTIONS) {
      expect(opt.label.length).toBeGreaterThan(0);
      expect(opt.unitMetric.length).toBeGreaterThan(0);
      expect(opt.unitImperial.length).toBeGreaterThan(0);
    }
  });
});
