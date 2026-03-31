import { useEffect, useState } from 'react';
import { measurementService } from '@/services/measurementService';
import type { MeasurementDto } from '@/types/measurement';

interface UseMemberProgressResult {
  measurements: MeasurementDto[];
  isLoading: boolean;
  error: string | null;
}

/**
 * Fetches body measurements for a given member.
 * Falls back to IndexedDB cache when offline (handled by measurementService).
 */
export function useMemberProgress(memberId: string): UseMemberProgressResult {
  const [measurements, setMeasurements] = useState<MeasurementDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!memberId) return;

    let cancelled = false;

    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await measurementService.getMeasurements(memberId);
        // Sort ascending by date for correct chart rendering
        const sorted = [...data].sort(
          (a, b) => new Date(a.recordedAt).getTime() - new Date(b.recordedAt).getTime()
        );
        if (!cancelled) setMeasurements(sorted);
      } catch (err) {
        if (!cancelled) setError('No se pudieron cargar las medidas.');
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [memberId]);

  return { measurements, isLoading, error };
}
