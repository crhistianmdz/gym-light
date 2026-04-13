import { db } from '@/db/gymflow.db';
import { fetchWithAuth } from './httpClient';
import type { AddMeasurementRequest, MeasurementDto } from '@/types/measurement';

export const measurementService = {
  async addMeasurement(memberId: string, request: AddMeasurementRequest): Promise<MeasurementDto> {
    const clientGuid = crypto.randomUUID(); // Generate unique GUID
    try {
      const response = await fetchWithAuth(`/api/members/${memberId}/measurements`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Client-Guid': clientGuid },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        throw new Error(`Unable to add measurement: ${response.statusText}`);
      }

      const dto: MeasurementDto = await response.json();
      await db.measurements.put({ ...dto, syncStatus: 'synced' }); // Cache server response immediately
      return dto;
    } catch (error) {
      // Offline fallback
      const localMeasurement = { ...request, memberId, syncStatus: 'pending', clientGuid };
      await db.measurements.put(localMeasurement);
      await db.sync_queue.add({
        type: 'HealthUpdate',
        guid: clientGuid,
        payload: JSON.stringify({ memberId, ...request }),
        timestamp: Date.now(),
        retryCount: 0,
        isOffline: true,
      });

      return { ...localMeasurement, id: '', recordedById: '' } as MeasurementDto; // Simulate server response format
    }
  },

  async getMeasurements(memberId: string): Promise<MeasurementDto[]> {
    try {
      const response = await fetchWithAuth(`/api/members/${memberId}/measurements`, {
        method: 'GET',
      });

      if (!response.ok) {
        throw new Error(`Unable to fetch measurements: ${response.statusText}`);
      }

      const measurements: MeasurementDto[] = await response.json();

      // Cache results locally
      for (const measurement of measurements) {
        await db.measurements.put({ ...measurement, syncStatus: 'synced' });
      }

      return measurements;
    } catch (error) {
      // Offline fallback
      const offlineMeasurements = await db.measurements
        .where('memberId')
        .equals(memberId)
        .toArray();

return offlineMeasurements.sort((a: MeasurementDto, b: MeasurementDto) => new Date(b.recordedAt).getTime() - new Date(a.recordedAt).getTime());
    }
  },
};