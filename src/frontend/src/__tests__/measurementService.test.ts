import { db } from '@/db/gymflow.db';
import { measurementService } from '@/services/measurementService';
import { fakeIndexedDB } from 'fake-indexeddb';

describe('measurementService', () => {
  beforeAll(() => {
    db.open = jest.fn().mockImplementation(() => Promise.resolve());
    global.indexedDB = fakeIndexedDB;
  });

  afterEach(async () => {
    await db.measurements.clear();
    await db.sync_queue.clear();
  });

  test('addMeasurement (online) posts to API', async () => {
    global.fetch = jest.fn(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve({ id: 'server-id', weightKg: 70, clientGuid: '123', recordedAt: new Date().toISOString() }),
      })
    );

    const result = await measurementService.addMeasurement('member-id', {
      clientGuid: '123',
      recordedAt: new Date().toISOString(),
      weightKg: 70,
      bodyFatPct: 15,
      chestCm: 100,
      waistCm: 80,
      hipCm: 90,
      armCm: 30,
      legCm: 50,
      unitSystem: 'metric',
    });

    expect(fetch).toHaveBeenCalledWith('/api/members/member-id/measurements', expect.any(Object));
    expect(result.syncStatus).toEqual('synced');
  });

  test('addMeasurement (offline) saves to Dexie and sync_queue', async () => {
    global.fetch = jest.fn(() => Promise.reject(new Error('Offline')));

    const result = await measurementService.addMeasurement('member-id', {
      clientGuid: '123',
      recordedAt: new Date().toISOString(),
      weightKg: 70,
      bodyFatPct: 15,
      chestCm: 100,
      waistCm: 80,
      hipCm: 90,
      armCm: 30,
      legCm: 50,
      unitSystem: 'metric',
    });

    const offlineQueue = await db.sync_queue.toArray();

    expect(offlineQueue).toHaveLength(1);
    expect(result.syncStatus).toEqual('pending');
  });

  test('getMeasurements (offline) returns data from Dexie sorted newest first', async () => {
    await db.measurements.bulkAdd([
      {
        id: 1,
        memberId: 'member-id',
        recordedAt: '2026-03-30T10:00:00Z',
        syncStatus: 'synced',
      },
      {
        id: 2,
        memberId: 'member-id',
        recordedAt: '2026-03-31T10:00:00Z',
        syncStatus: 'synced',
      },
    ]);

    global.fetch = jest.fn(() => Promise.reject(new Error('Offline')));
    const results = await measurementService.getMeasurements('member-id');

    expect(results).toHaveLength(2);
  });
});