// context/AuthContext.tsx
// A bejelentkezett felhasználó állapota az egész alkalmazás számára.
//
// FONTOS: a felületen a gombok és menüpontok elrejtése csak kényelmi funkció.
// A tényleges jogosultság-ellenőrzés a SZERVEREN történik (401/403 válaszok).

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import type { CurrentUser, Role } from '../types/auth'
import { fetchCurrentUser, login as loginRequest, logout as logoutRequest } from '../services/authService'
import { setUnauthorizedHandler } from '../services/apiClient'

interface AuthContextValue {
  user: CurrentUser | null
  loading: boolean
  /** true, ha a munkamenet egy korábban bejelentkezett felhasználónál lejárt */
  sessionExpired: boolean
  login: (identifier: string, password: string) => Promise<void>
  logout: () => Promise<void>
  hasRole: (...roles: Role[]) => boolean
  canManageSuppliers: boolean
  canManageRisks: boolean
  canManageMitigations: boolean
  canManageUsers: boolean
  canViewAuditLogs: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [loading, setLoading] = useState(true)
  const [sessionExpired, setSessionExpired] = useState(false)
  const userRef = useRef<CurrentUser | null>(null)

  const updateUser = useCallback((next: CurrentUser | null) => {
    userRef.current = next
    setUser(next)
  }, [])

  // Induláskor lekérdezzük, van-e érvényes munkamenet (a cookie-t a böngésző küldi)
  useEffect(() => {
    let cancelled = false

    // Ha bármely API-hívás 401-et kap, miközben be voltunk jelentkezve: munkamenet lejárt
    setUnauthorizedHandler(() => {
      if (userRef.current) {
        updateUser(null)
        setSessionExpired(true)
      }
    })

    fetchCurrentUser()
      .then(current => {
        if (!cancelled) updateUser(current)
      })
      .catch(() => {
        if (!cancelled) updateUser(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
      setUnauthorizedHandler(null)
    }
  }, [updateUser])

  const login = useCallback(async (identifier: string, password: string) => {
    const current = await loginRequest({ identifier, password })
    setSessionExpired(false)
    updateUser(current)
  }, [updateUser])

  const logout = useCallback(async () => {
    try {
      await logoutRequest()
    } finally {
      // Hiba esetén is a kezelőfelület kijelentkezett állapotba kerül
      updateUser(null)
      setSessionExpired(false)
    }
  }, [updateUser])

  const hasRole = useCallback(
    (...roles: Role[]) => !!user && roles.some(role => user.roles.includes(role)),
    [user]
  )

  const value = useMemo<AuthContextValue>(() => ({
    user,
    loading,
    sessionExpired,
    login,
    logout,
    hasRole,
    canManageSuppliers: hasRole('Administrator', 'RiskManager'),
    canManageRisks: hasRole('Administrator', 'RiskManager'),
    canManageMitigations: hasRole('Administrator', 'RiskManager'),
    canManageUsers: hasRole('Administrator'),
    canViewAuditLogs: hasRole('Administrator')
  }), [user, loading, sessionExpired, login, logout, hasRole])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('A useAuth csak AuthProvider-en belül használható.')
  }
  return context
}

