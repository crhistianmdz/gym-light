import fakeIndexedDB from 'fake-indexeddb'
import { db } from '@/db/gymflow.db'
import { dashboardService } from '@/services/dashboardService'
import { fetchWithAuth } from '@/services/httpClient'
import { vi, describe, beforeAll, afterEach, it, expect } from 'vitest'

vi.mock('@/services/httpClient', () => ({
  fetchWithAuth: vi.fn(),
}))

const mockFetch = vi.mocked(fetchWithAuth)

const INCOME_REPORT = {
  from: '2025-01-01',
  to: '2025-12-31',
  totalIncome: 1500,
  byMonth: [
    { year: 2025, month: 1, membership: 1000, pos: 500, total: 1500 },
  ],
}

const CHURN_REPORT = {
  year: 2025,
  totalMembers: 10,
  activeMembers: 7,
  notRenewed: 3,
  churnRate: 30,
}

describe('dashboardService', () => {
  beforeAll(() => {
    global.indexedDB = fakeIndexedDB
    vi.spyOn(db, 'open').mockResolvedValue(db as any)
  })

  afterEach(async () => {
    vi.clearAllMocks()
    await db.payments.clear()
    await db.users.clear()
  })

  describe('getIncomeReport', () => {
    it('returns server data when online', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => INCOME_REPORT,
      } as any)

      const report = await dashboardService.getIncomeReport('2025-01-01', '2025-12-31')

      expect(report.totalIncome).toBe(1500)
      expect(report.byMonth).toHaveLength(1)
      expect(report.isOffline).toBeUndefined()
    })

    it('falls back to Dexie payments when network fails', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      // Seed local payments
      await db.payments.add({
        id: 'p1',
        memberId: undefined,
        amount: 500,
        category: 0, // Membership
        timestamp: new Date('2025-03-15').getTime(),
        syncStatus: 'synced',
        clientGuid: 'cg-1',
        createdByUserId: 'user-1',
      })
      await db.payments.add({
        id: 'p2',
        memberId: undefined,
        amount: 200,
        category: 1, // POS
        timestamp: new Date('2025-03-20').getTime(),
        syncStatus: 'synced',
        clientGuid: 'cg-2',
        createdByUserId: 'user-1',
      })

      const report = await dashboardService.getIncomeReport('2025-01-01', '2025-12-31')

      expect(report.isOffline).toBe(true)
      expect(report.totalIncome).toBe(700)
      expect(report.byMonth).toHaveLength(1)
      expect(report.byMonth[0].membership).toBe(500)
      expect(report.byMonth[0].pos).toBe(200)
    })

    it('returns empty when no local payments exist offline', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      const report = await dashboardService.getIncomeReport('2025-01-01', '2025-12-31')

      expect(report.isOffline).toBe(true)
      expect(report.totalIncome).toBe(0)
      expect(report.byMonth).toHaveLength(0)
    })
  })

  describe('getChurnReport', () => {
    it('returns server data when online', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => CHURN_REPORT,
      } as any)

      const report = await dashboardService.getChurnReport(2025)

      expect(report.churnRate).toBe(30)
      expect(report.isOffline).toBeUndefined()
    })

    it('falls back to Dexie users when network fails', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      // Seed local members
      await db.users.add({
        id: 'm1',
        fullName: 'Alice',
        status: 'Active',
        membershipEndDate: '2025-12-31',
      })
      await db.users.add({
        id: 'm2',
        fullName: 'Bob',
        status: 'Expired',
        membershipEndDate: '2025-06-30',
      })

      const report = await dashboardService.getChurnReport(2025)

      expect(report.isOffline).toBe(true)
      expect(report.totalMembers).toBe(2)
      expect(report.activeMembers).toBe(1)
      expect(report.notRenewed).toBe(1)
      expect(report.churnRate).toBeCloseTo(50)
    })
  })
})
