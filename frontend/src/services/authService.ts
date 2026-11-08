// services/authService.ts
// Bejelentkezés, kijelentkezés és az aktuális felhasználó lekérdezése.

import { apiFetch, ApiError, clearCsrfToken } from './apiClient'
import type { CurrentUser, LoginRequest } from '../types/auth'

/** A bejelentkezett felhasználó adatai, vagy null, ha nincs érvényes munkamenet. */
export async function fetchCurrentUser(): Promise<CurrentUser | null> {
  try {
    return await apiFetch<CurrentUser>('/api/auth/me')
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) return null
    throw err
  }
}

export async function login(request: LoginRequest): Promise<CurrentUser> {
  const user = await apiFetch<CurrentUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(request)
  })
  // A CSRF-token a bejelentkezett felhasználóhoz kötődik: a régi (névtelen) token már nem jó,
  // a következő állapotmódosító kérés előtt automatikusan újat kérünk.
  clearCsrfToken()
  return user
}

export async function logout(): Promise<void> {
  try {
    await apiFetch<void>('/api/auth/logout', { method: 'POST' })
  } finally {
    clearCsrfToken()
  }
}

