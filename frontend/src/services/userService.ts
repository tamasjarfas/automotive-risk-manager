// services/userService.ts
// A felhasználókezelő végpontok (/api/users) hívása. Csak Administrator használhatja.

import { apiFetch } from './apiClient'
import type { CreateUserRequest, ManagedUser, Role } from '../types/auth'

export function fetchUsers(): Promise<ManagedUser[]> {
  return apiFetch<ManagedUser[]>('/api/users')
}

export function createUser(data: CreateUserRequest): Promise<ManagedUser> {
  return apiFetch<ManagedUser>('/api/users', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

export function setUserRole(id: string, role: Role): Promise<ManagedUser> {
  return apiFetch<ManagedUser>(`/api/users/${id}/role`, {
    method: 'PUT',
    body: JSON.stringify({ role })
  })
}

export function setUserActive(id: string, isActive: boolean): Promise<ManagedUser> {
  return apiFetch<ManagedUser>(`/api/users/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ isActive })
  })
}

