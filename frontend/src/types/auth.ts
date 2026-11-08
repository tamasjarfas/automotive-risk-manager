// types/auth.ts
// A hitelesítéshez és felhasználókezeléshez tartozó típusok.
// A backend AuthDtos.cs / UserDtos.cs osztályait tükrözik.

export type Role = 'Administrator' | 'RiskManager' | 'Viewer'

export const ALL_ROLES: Role[] = ['Administrator', 'RiskManager', 'Viewer']

// Megjelenített (magyar) szerepkörnevek
export const ROLE_LABELS: Record<Role, string> = {
  Administrator: 'Rendszergazda',
  RiskManager: 'Kockázatkezelő',
  Viewer: 'Megtekintő'
}

export interface CurrentUser {
  id: string
  userName: string
  email: string
  displayName: string
  roles: Role[]
}

export interface LoginRequest {
  identifier: string
  password: string
}

export interface ManagedUser {
  id: string
  userName: string
  email: string
  displayName: string
  isActive: boolean
  isLockedOut: boolean
  role: string
  createdAt: string
}

export interface CreateUserRequest {
  userName: string
  email: string
  displayName: string
  password: string
  role: Role
}

