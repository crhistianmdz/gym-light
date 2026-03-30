export type UnitSystem = 'metric' | 'imperial';

export interface MeasurementDto {
  id: string;
  memberId: string;
  recordedById: string;
  recordedAt: string; // ISO UTC
  weightKg: number;
  bodyFatPct: number;
  chestCm: number;
  waistCm: number;
  hipCm: number;
  armCm: number;
  legCm: number;
  unitSystem: UnitSystem;
  notes?: string;
  clientGuid: string;
}

export interface AddMeasurementRequest {
  clientGuid: string;
  recordedAt: string;
  weightKg: number;
  bodyFatPct: number;
  chestCm: number;
  waistCm: number;
  hipCm: number;
  armCm: number;
  legCm: number;
  unitSystem: UnitSystem;
  notes?: string;
}