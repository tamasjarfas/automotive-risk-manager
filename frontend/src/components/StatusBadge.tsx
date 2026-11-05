// StatusBadge.tsx – a backend kapcsolat állapotát jelző komponens.
// Három állapotot jelenít meg: töltés, sikeres kapcsolat, hiba.

import { HealthResponse } from '../services/healthService'

interface StatusBadgeProps {
  loading: boolean
  error: string | null
  health: HealthResponse | null
}

export default function StatusBadge({ loading, error, health }: StatusBadgeProps) {
  if (loading) {
    return (
      <div className="status-badge status-loading">
        <span className="status-dot status-dot-loading"></span>
        <span>Backend kapcsolat ellenőrzése...</span>
      </div>
    )
  }

  if (error) {
    return (
      <div className="status-badge status-error">
        <span className="status-dot status-dot-error"></span>
        <div>
          <strong>Backend nem elérhető</strong>
          <p>{error}</p>
          <small>Győződj meg róla, hogy a backend szerver fut (dotnet run)</small>
        </div>
      </div>
    )
  }

  if (health) {
    return (
      <div className="status-badge status-healthy">
        <span className="status-dot status-dot-healthy"></span>
        <div>
          <strong>{health.status} – Backend elérhető ✓</strong>
          <p>{health.message}</p>
          <small>
            {health.application} v{health.version} &nbsp;|&nbsp;
            Környezet: {health.environment} &nbsp;|&nbsp;
            {health.timestamp}
          </small>
        </div>
      </div>
    )
  }

  return null
}

