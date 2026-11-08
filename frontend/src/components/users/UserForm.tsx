// components/users/UserForm.tsx
// Modális ablak új felhasználó létrehozásához (csak Administrator használhatja).
// A kezdeti jelszót a rendszergazda adja meg; a jelszó sehol nem tárolódik a böngészőben.

import { useState } from 'react'
import type { FormEvent, MouseEvent } from 'react'
import type { ManagedUser, Role } from '../../types/auth'
import { ALL_ROLES, ROLE_LABELS } from '../../types/auth'
import { createUser } from '../../services/userService'

interface Props {
  onSaved: (user: ManagedUser) => void
  onCancel: () => void
}

export default function UserForm({ onSaved, onCancel }: Props) {
  const [userName, setUserName] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<Role>('Viewer')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  function validate(): boolean {
    const errors: Record<string, string> = {}
    if (!/^[A-Za-z0-9._-]{3,64}$/.test(userName.trim())) {
      errors.userName = '3–64 karakter: betű, szám, ".", "_" vagy "-".'
    }
    if (!displayName.trim()) errors.displayName = 'A megjelenített név kötelező.'
    if (!/^\S+@\S+\.\S+$/.test(email.trim())) errors.email = 'Érvénytelen e-mail-cím.'
    if (password.length < 12) errors.password = 'A jelszó legalább 12 karakter legyen.'
    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!validate()) return

    setSubmitting(true)
    setError(null)
    try {
      const created = await createUser({
        userName: userName.trim(),
        displayName: displayName.trim(),
        email: email.trim(),
        password,
        role
      })
      onSaved(created)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'A felhasználó létrehozása sikertelen.')
    } finally {
      setSubmitting(false)
    }
  }

  function handleBackdropClick(e: MouseEvent<HTMLDivElement>) {
    if (e.target === e.currentTarget && !submitting) onCancel()
  }

  return (
    <div className="modal-backdrop" onClick={handleBackdropClick} role="dialog" aria-modal="true">
      <div className="modal-box">
        <div className="modal-header">
          <h2>Új felhasználó létrehozása</h2>
          <button className="modal-close-btn" onClick={onCancel} aria-label="Bezárás">✕</button>
        </div>

        {error && (
          <div className="form-error-banner" role="alert">⚠️ {error}</div>
        )}

        <form onSubmit={handleSubmit} noValidate autoComplete="off">
          <div className="form-group">
            <label htmlFor="usr-name" className="form-label">
              Felhasználónév <span className="required-mark">*</span>
            </label>
            <input
              id="usr-name"
              type="text"
              className={`form-input ${fieldErrors.userName ? 'input-error' : ''}`}
              value={userName}
              onChange={e => setUserName(e.target.value)}
              maxLength={64}
              disabled={submitting}
              autoFocus
            />
            {fieldErrors.userName && <p className="field-error">{fieldErrors.userName}</p>}
          </div>

          <div className="form-group">
            <label htmlFor="usr-display" className="form-label">
              Megjelenített név <span className="required-mark">*</span>
            </label>
            <input
              id="usr-display"
              type="text"
              className={`form-input ${fieldErrors.displayName ? 'input-error' : ''}`}
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              maxLength={150}
              disabled={submitting}
            />
            {fieldErrors.displayName && <p className="field-error">{fieldErrors.displayName}</p>}
          </div>

          <div className="form-group">
            <label htmlFor="usr-email" className="form-label">
              E-mail-cím <span className="required-mark">*</span>
            </label>
            <input
              id="usr-email"
              type="email"
              className={`form-input ${fieldErrors.email ? 'input-error' : ''}`}
              value={email}
              onChange={e => setEmail(e.target.value)}
              maxLength={256}
              placeholder="pl. felhasznalo@example.test"
              disabled={submitting}
            />
            {fieldErrors.email && <p className="field-error">{fieldErrors.email}</p>}
          </div>

          <div className="form-group">
            <label htmlFor="usr-password" className="form-label">
              Kezdeti jelszó <span className="required-mark">*</span>
            </label>
            <input
              id="usr-password"
              type="password"
              className={`form-input ${fieldErrors.password ? 'input-error' : ''}`}
              value={password}
              onChange={e => setPassword(e.target.value)}
              maxLength={128}
              autoComplete="new-password"
              disabled={submitting}
            />
            <p className="char-count">Legalább 12 karakter, kisbetű, nagybetű és számjegy.</p>
            {fieldErrors.password && <p className="field-error">{fieldErrors.password}</p>}
          </div>

          <div className="form-group">
            <label htmlFor="usr-role" className="form-label">
              Szerepkör <span className="required-mark">*</span>
            </label>
            <select
              id="usr-role"
              className="form-input"
              value={role}
              onChange={e => setRole(e.target.value as Role)}
              disabled={submitting}
            >
              {ALL_ROLES.map(r => (
                <option key={r} value={r}>{ROLE_LABELS[r]} ({r})</option>
              ))}
            </select>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={submitting}>
              Mégse
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? '⏳ Mentés...' : '➕ Létrehozás'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

