import { fetchWithAuth } from '@/services/httpClient';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

export interface PluginResponse {
  id: string;
  name: string;
  version: string;
  enabled: boolean;
  offlineCapable: boolean;
  installedAt: string;
  lastUpdated: string;
}

export async function getPlugins(): Promise<PluginResponse[]> {
  const response = await fetchWithAuth(`${API_BASE}/api/plugins`, {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch plugins: ${response.statusText}`);
  }

  return response.json();
}

export async function enablePlugin(id: string): Promise<PluginResponse> {
  const response = await fetchWithAuth(`${API_BASE}/api/plugins/${id}/enable`, {
    method: 'PATCH',
  });

  if (!response.ok) {
    throw new Error(`Failed to enable plugin: ${response.statusText}`);
  }

  return response.json();
}

export async function disablePlugin(id: string): Promise<PluginResponse> {
  const response = await fetchWithAuth(`${API_BASE}/api/plugins/${id}/disable`, {
    method: 'PATCH',
  });

  if (!response.ok) {
    throw new Error(`Failed to disable plugin: ${response.statusText}`);
  }

  return response.json();
}