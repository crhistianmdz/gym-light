/**
 * fetchWithAuth — attaches in-memory access token, handles 401 → refresh → retry.
 * Usage: replace raw fetch() calls with fetchWithAuth() for authenticated endpoints.
 */

let _getToken: (() => string | null) | null = null;
let _refresh: (() => Promise<string>) | null = null;
let _logout: (() => void) | null = null;

export function initHttpClient(
  getToken: () => string | null,
  refresh: () => Promise<string>,
  onLogout: () => void,
): void {
  _getToken = getToken;
  _refresh  = refresh;
  _logout   = onLogout;
}

export async function fetchWithAuth(
  input: RequestInfo | URL,
  init: RequestInit = {},
): Promise<Response> {
  const token = _getToken?.();
  const headers = new Headers(init.headers);
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const res = await fetch(input, { ...init, headers, credentials: 'include' });

  if (res.status === 401 && _refresh) {
    try {
      const newToken = await _refresh();
      headers.set('Authorization', `Bearer ${newToken}`);
      return fetch(input, { ...init, headers, credentials: 'include' });
    } catch {
      _logout?.();
      throw new Error('Sesión expirada. Por favor ingresá de nuevo.');
    }
  }

  return res;
}