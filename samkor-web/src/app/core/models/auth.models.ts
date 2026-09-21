export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: string;
  token: string;
  expiresAt: string;
  fullName: string;
  email: string;
  roles: string[];
}
