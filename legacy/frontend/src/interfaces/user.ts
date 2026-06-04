export interface User {
  id: number;
  username: string;
  role: string;
  createdAt: string;
  lastLoginAt: string | null;
  deletedAt: string | null;
  mfaEnabled: boolean;
}
