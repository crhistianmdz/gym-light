import { db } from '@/db/gymflow.db';
import { fetchWithAuth } from '@/services/httpClient';

export interface MonthlyBreakdown {
  year: number;
  month: number;
  membership: number;
  pos: number;
  total: number;
}

export interface IncomeReport {
  from: string;
  to: string;
  totalIncome: number;
  byMonth: MonthlyBreakdown[];
  isOffline?: boolean;
}

export interface ChurnReport {
  year: number;
  totalMembers: number;
  activeMembers: number;
  notRenewed: number;
  churnRate: number;
  isOffline?: boolean;
}

export interface RegisterPaymentRequest {
  memberId?: string;
  amount: number;
  category: 0 | 1;
  clientGuid: string;
  notes?: string;
  saleId?: string;
}

export const dashboardService = {
  async getIncomeReport(from: string, to: string): Promise<IncomeReport> {
    try {
      const res = await fetchWithAuth(`/api/admin/metrics/income?from=${from}&to=${to}`);
      if (!res.ok) throw new Error('Network error');
      return await res.json();
    } catch {
      const payments = await db.payments.toArray();
      const filtered = payments.filter(p => {
        const t = new Date(p.timestamp).toISOString().substring(0, 10);
        return t >= from && t <= to;
      });
      const byMonthMap = new Map<string, MonthlyBreakdown>();
      for (const p of filtered) {
        const d = new Date(p.timestamp);
        const key = `${d.getFullYear()}-${d.getMonth() + 1}`;
        if (!byMonthMap.has(key)) {
          byMonthMap.set(key, {
            year: d.getFullYear(),
            month: d.getMonth() + 1,
            membership: 0,
            pos: 0,
            total: 0,
          });
        }
        const row = byMonthMap.get(key)!;
        if (p.category === 0) row.membership += p.amount;
        else row.pos += p.amount;
        row.total += p.amount;
      }
      const byMonth = Array.from(byMonthMap.values()).sort((a, b) => a.year - b.year || a.month - b.month);
      const totalIncome = byMonth.reduce((sum, m) => sum + m.total, 0);
      return { from, to, totalIncome, byMonth, isOffline: true };
    }
  },

  async getChurnReport(year: number): Promise<ChurnReport> {
    try {
      const res = await fetchWithAuth(`/api/admin/metrics/churn?year=${year}`);
      if (!res.ok) throw new Error('Network error');
      return await res.json();
    } catch {
      const members = await db.users.toArray();
      const totalMembers = members.length;
      const activeMembers = members.filter(m => m.status === 'Active').length;
      const notRenewed = members.filter(m => m.status === 'Expired').length;
      const churnRate = totalMembers > 0 ? (notRenewed / totalMembers) * 100 : 0;
      return { year, totalMembers, activeMembers, notRenewed, churnRate, isOffline: true };
    }
  },

  async registerPayment(req: RegisterPaymentRequest, createdByUserId: string): Promise<void> {
    const res = await fetchWithAuth('/api/payments', {
      method: 'POST',
      body: JSON.stringify({ ...req, createdByUserId }),
    });
    if (!res.ok) throw new Error('Error al registrar pago');
    const payment = await res.json();
    await db.payments.put({ ...payment, syncStatus: 'synced', createdByUserId });
  },
};