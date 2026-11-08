// components/users/UserManagement.tsx
// Rendszergazdai felhasználókezelő felület: lista, új felhasználó, szerepkör módosítása,
// aktiválás / inaktiválás. A szerver minden műveletet ellenőriz (csak Administrator).

import { useCallback, useEffect, useState } from 'react'
import type { ManagedUser, Role } from '../../types/auth'
import { ALL_ROLES, ROLE_LABELS } from '../../types/auth'
import { fetchUsers, setUserActive, setUserRole } from '../../services/userService'
import { useAuth } from '../../context/AuthContext'
import UserForm from './UserForm'

interface Notification {
  type: 'success' | 'error'
  text: string
}

export default function UserManagement() {
  const { user: me } = useAuth()

  const [users, setUsers] = useState<ManagedUser[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notification, setNotification] = useState<Notification | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)

  const loadUsers = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      setUsers(await fetchUsers())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Nem sikerült betölteni a felhasználókat.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  useEffect(() => {
    if (!notification) return
    const timer = setTimeout(() => setNotification(null), 5000)
    return () => clearTimeout(timer)
  }, [notification])

  function replaceUser(updated: ManagedUser) {
    setUsers(prev => prev.map(u => (u.id === updated.id ? updated : u)))
  }

  async function handleRoleChange(target: ManagedUser, role: Role) {
    if (target.role === role) return
    setBusyId(target.id)
    try {
      const updated = await setUserRole(target.id, role)
      replaceUser(updated)
      setNotification({ type: 'success', text: `✅ „${updated.userName}" szerepköre: ${ROLE_LABELS[role]}.` })
    } catch (err) {
      setNotification({ type: 'error', text: err instanceof Error ? err.message : 'A szerepkör módosítása sikertelen.' })
    } finally {
      setBusyId(null)
    }
  }

  async function handleToggleActive(target: ManagedUser) {
    setBusyId(target.id)
    try {
      const updated = await setUserActive(target.id, !target.isActive)
      replaceUser(updated)
      setNotification({
        type: 'success',
        text: `✅ „${updated.userName}" állapota: ${updated.isActive ? 'Aktív' : 'Inaktív'}.`
      })
    } catch (err) {
      setNotification({ type: 'error', text: err instanceof Error ? err.message : 'Az állapotváltás sikertelen.' })
    } finally {
      setBusyId(null)
    }
  }

  function handleCreated(created: ManagedUser) {
    setUsers(prev => [...prev, created].sort((a, b) => a.userName.localeCompare(b.userName, 'hu')))
    setShowForm(false)
    setNotification({ type: 'success', text: `✅ „${created.userName}" felhasználó létrehozva.` })
  }

  function formatDate(iso: string): string {
    return new Intl.DateTimeFormat('hu-HU', {
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit'
    }).format(new Date(iso))
  }

  return (
    <div className="supplier-list-page">
      <div className="page-header">
        <div>
          <h2 className="page-title">Felhasználók</h2>
          <p className="page-subtitle">
            {loading ? 'Betöltés...' : `${users.length} felhasználó`}
          </p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>
          ➕ Új felhasználó
        </button>
      </div>

      {notification && (
        <div className={`notification notification-${notification.type}`} role="alert">
          {notification.text}
          <button className="notification-close" onClick={() => setNotification(null)}>✕</button>
        </div>
      )}

      {loading && (
        <div className="loading-state">
          <div className="spinner"></div>
          <span>Felhasználók betöltése...</span>
        </div>
      )}

      {!loading && error && (
        <div className="error-state" role="alert">
          <p>⚠️ {error}</p>
          <button className="btn btn-secondary" onClick={loadUsers}>🔄 Újrapróbálás</button>
        </div>
      )}

      {!loading && !error && users.length > 0 && (
        <div className="table-wrapper">
          <table className="supplier-table">
            <thead>
              <tr>
                <th>Név</th>
                <th>Felhasználónév / e-mail</th>
                <th>Szerepkör</th>
                <th>Állapot</th>
                <th>Létrehozva</th>
                <th className="col-actions">Műveletek</th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => {
                const isSelf = me?.id === u.id
                const busy = busyId === u.id
                return (
                  <tr key={u.id} className={u.isActive ? '' : 'row-inactive'}>
                    <td>
                      <div className="supplier-name">
                        {u.displayName}{isSelf && <span className="self-tag">te</span>}
                      </div>
                    </td>
                    <td>
                      <div>{u.userName}</div>
                      <div className="supplier-desc-preview">{u.email}</div>
                    </td>
                    <td>
                      <select
                        className="role-select"
                        value={u.role}
                        onChange={e => handleRoleChange(u, e.target.value as Role)}
                        disabled={isSelf || busy}
                        title={isSelf ? 'A saját szerepköröd nem módosíthatod' : 'Szerepkör módosítása'}
                        aria-label={`${u.userName} szerepköre`}
                      >
                        {/* Ha a szerepkör ismeretlen (pl. nincs hozzárendelve), külön opcióként látszik */}
                        {!ALL_ROLES.includes(u.role as Role) && <option value={u.role}>{u.role || '—'}</option>}
                        {ALL_ROLES.map(r => (
                          <option key={r} value={r}>{ROLE_LABELS[r]}</option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <span className={`supplier-status-badge ${u.isActive ? 'badge-active' : 'badge-inactive'}`}>
                        <span className={`badge-dot ${u.isActive ? 'dot-active' : 'dot-inactive'}`}></span>
                        {u.isActive ? 'Aktív' : 'Inaktív'}
                      </span>
                      {u.isLockedOut && <span className="lock-tag" title="Túl sok sikertelen belépés miatt ideiglenesen zárolva">🔒 zárolva</span>}
                    </td>
                    <td className="col-date">{formatDate(u.createdAt)}</td>
                    <td className="col-actions">
                      <div className="action-buttons">
                        <button
                          className={`btn btn-sm ${u.isActive ? 'btn-warning' : 'btn-success'}`}
                          onClick={() => handleToggleActive(u)}
                          disabled={isSelf || busy}
                          title={isSelf ? 'A saját fiókodat nem inaktiválhatod' : u.isActive ? 'Inaktiválás' : 'Aktiválás'}
                        >
                          {busy ? '⏳' : u.isActive ? '⏸ Inaktiválás' : '▶ Aktiválás'}
                        </button>
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {showForm && (
        <UserForm onSaved={handleCreated} onCancel={() => setShowForm(false)} />
      )}
    </div>
  )
}

