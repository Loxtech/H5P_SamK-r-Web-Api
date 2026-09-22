export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  isLockedOut: boolean;
}
