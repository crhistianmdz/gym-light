import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { vi } from 'vitest';
import { useAuth } from '@/contexts/AuthContext';
import { dashboardService } from '@/services/dashboardService';
import DashboardPage from '@/pages/DashboardPage';
import IncomeChart from '@/components/Dashboard/IncomeChart';
import ChurnStats from '@/components/Dashboard/ChurnStats';

vi.mock('@/contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}));

vi.mock('@/services/dashboardService', () => ({
  dashboardService: {
    getIncomeReport: vi.fn(),
    getChurnReport: vi.fn(),
  },
}));

vi.mock('@/components/Dashboard/IncomeChart', () => ({
  __esModule: true,
  default: () => <div data-testid="income-chart" />,
}));

vi.mock('@/components/Dashboard/ChurnStats', () => ({
  __esModule: true,
  default: () => <div data-testid="churn-stats" />,
}));

describe('DashboardPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('redirects if user role is not Owner or Admin', () => {
    vi.mocked(useAuth).mockReturnValueOnce({ user: { role: 'Member' } });
    render(<DashboardPage />);

    expect(screen.queryByText('Dashboard de Métricas')).not.toBeInTheDocument();
  });

  it('shows loading indicator while fetching data', async () => {
    vi.mocked(useAuth).mockReturnValueOnce({ user: { role: 'Admin' } });
    vi.mocked(dashboardService.getIncomeReport).mockImplementation(() => new Promise(() => {}));
    vi.mocked(dashboardService.getChurnReport).mockImplementation(() => new Promise(() => {}));

    render(<DashboardPage />);

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('renders IncomeChart and ChurnStats with fetched data', async () => {
    vi.mocked(useAuth).mockReturnValueOnce({ user: { role: 'Owner' } });
    vi.mocked(dashboardService.getIncomeReport).mockResolvedValueOnce({
      from: '2025-01-01',
      to: '2025-12-31',
      totalIncome: 1500,
      byMonth: [
        { year: 2025, month: 1, membership: 1000, pos: 500, total: 1500 },
      ],
    });
    vi.mocked(dashboardService.getChurnReport).mockResolvedValueOnce({
      year: 2025,
      totalMembers: 10,
      activeMembers: 7,
      notRenewed: 3,
      churnRate: 30,
    });

    render(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByTestId('income-chart')).toBeInTheDocument();
      expect(screen.getByTestId('churn-stats')).toBeInTheDocument();
    });
  });

  it('shows error message if service fails', async () => {
    vi.mocked(useAuth).mockReturnValueOnce({ user: { role: 'Admin' } });
    vi.mocked(dashboardService.getIncomeReport).mockRejectedValueOnce(new Error('Network error'));
    vi.mocked(dashboardService.getChurnReport).mockRejectedValueOnce(new Error('Network error'));

    render(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByText('Error al cargar el informe de ingresos.')).toBeInTheDocument();
    });
  });
});