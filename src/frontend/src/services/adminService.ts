import { fetchWithAuth } from '@/services/httpClient';
import type { AccessLogDto, AccessLogFilter, PagedResult } from '@/types/accessLog';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

export async function getAccessLogs(filter: AccessLogFilter): Promise<PagedResult<AccessLogDto>> {
  const query = new URLSearchParams(
    Object.entries(filter).reduce((acc, [key, value]) => {
      if (value !== undefined && value !== '') {
        acc[key] = String(value);
      }
      return acc;
    }, {} as Record<string, string>)
  );

  const response = await fetchWithAuth(`${API_BASE}/api/admin/access-logs?${query}`, {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch access logs: ${response.statusText}`);
  }

  return response.json();
}

export async function exportAccessLogs(
  filter: Omit<AccessLogFilter, 'page' | 'pageSize'>,
  format: 'csv' | 'pdf'
): Promise<Blob> {
  const query = new URLSearchParams(
    Object.entries({ ...filter, format }).reduce((acc, [key, value]) => {
      if (value !== undefined && value !== '') {
        acc[key] = String(value);
      }
      return acc;
    }, {} as Record<string, string>)
  );

  const response = await fetchWithAuth(`${API_BASE}/api/admin/access-logs/export?${query}`, {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`Failed to export access logs: ${response.statusText}`);
  }

  return response.blob();
}