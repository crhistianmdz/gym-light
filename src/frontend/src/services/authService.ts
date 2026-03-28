const API_BASE = import.meta.env.VITE_API_URL ?? '/api';

export interface LoginPayload {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  userId: string;
  fullName: string;
  role: string;
  expiresAt: string;
}

export async function login(payload: LoginPayload): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error ?? 'Error al iniciar sesión');
  }

  return res.json() as Promise<AuthResponse>;
}

export async function refreshToken(): Promise<AuthResponse> {
  const res = await fetch(`${API_BASE}/auth/refresh`, {
    method: 'POST',
    credentials: 'include',
  });

  if (!res.ok) throw new Error('Sesión expirada');
  return res.json() as Promise<AuthResponse>;
}

export async function logout(): Promise<void> {
  await fetch(`${API_BASE}/auth/logout`, {
    method: 'POST',
    credentials: 'include',
  }).catch(() => {
    // best-effort: clear cookie server-side, ignore network errors
  });
}