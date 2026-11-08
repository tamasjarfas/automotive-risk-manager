// components/auth/LoginPage.tsx
// Bejelentkezési oldal. Felhasználónévvel VAGY e-mail-címmel és jelszóval lehet belépni.
// A hibaüzenet szándékosan általános: nem árulja el, hogy a felhasználó létezik-e.

import { useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from '../../context/AuthContext'

export default function LoginPage() {
  const { login, sessionExpired } = useAuth()
  const [identifier, setIdentifier] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!identifier.trim() || !password) {
      setError('Add meg a felhasználónevet (vagy e-mail-címet) és a jelszót.')
      return
    }

    setSubmitting(true)
    setError(null)
    try {
      await login(identifier.trim(), password)
      // Siker esetén az AuthContext frissül, az App automatikusan a fő felületet mutatja.
    } catch (err) {
      setPassword('')   // hibás belépés után a jelszómező ürítése
      setError(err instanceof Error ? err.message : 'A bejelentkezés sikertelen.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-logo">🛡️</div>
        <h1 className="login-title">Automotive Risk Manager</h1>
        <p className="login-subtitle">Információbiztonsági kockázatkezelő rendszer</p>

        {sessionExpired && (
          <div className="notification notification-error" role="alert">
            A munkamenet lejárt, kérjük, jelentkezz be újra.
          </div>
        )}

        {error && (
          <div className="form-error-banner" role="alert">
            ⚠️ {error}
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label htmlFor="login-identifier" className="form-label">
              Felhasználónév vagy e-mail-cím
            </label>
            <input
              id="login-identifier"
              type="text"
              className="form-input"
              value={identifier}
              onChange={e => setIdentifier(e.target.value)}
              autoComplete="username"
              autoFocus
              maxLength={256}
              disabled={submitting}
            />
          </div>

          <div className="form-group">
            <label htmlFor="login-password" className="form-label">Jelszó</label>
            <input
              id="login-password"
              type="password"
              className="form-input"
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="current-password"
              maxLength={128}
              disabled={submitting}
            />
          </div>

          <button type="submit" className="btn btn-primary login-submit" disabled={submitting}>
            {submitting ? '⏳ Bejelentkezés...' : 'Bejelentkezés'}
          </button>
        </form>

        <p className="login-note">
          Fiókot csak rendszergazda hozhat létre. Kizárólag fiktív adatokkal működő oktatási rendszer.
        </p>
      </div>
    </div>
  )
}

