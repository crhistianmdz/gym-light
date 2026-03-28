export interface AuthSession {
  userId: string;
  fullName: string;
  role: string;
  accessToken: string;
  expiresAt: string; // ISO string
}