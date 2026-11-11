// components/mitigations/MitigationList.tsx
// Kockázatcsökkentő intézkedések (MitigationAction) nyilvántartása és nyomon követése.
//
// Funkciók:
//   - Intézkedések táblázatos megjelenítése (megnevezés, felelős, kapcsolódó kockázat
//     és beszállító, határidő + lejárt jelzés, prioritás, állapot, eredmény)
//   - Lejárt és még nem befejezett intézkedések vizuális kiemelése (IsOverdue)
//   - Szűrés: kapcsolódó kockázat, beszállító, státusz, határidő és felelős szerint
//   - Új intézkedés rögzítése, szerkesztése és státuszváltása (Administrator, RiskManager)
//   - Viewer szerepkör esetén: csak megtekintési lehetőség

import { useState, useEffect, useCallback, useMemo } from 'react'
import type { Supplier } from '../../types/supplier'
import type { Risk } from '../../types/risk'
import type {
  MitigationAction,
  MitigationStatus,
  MitigationDueFilter
} from '../../types/mitigation'
import {
  MITIGATION_STATUS_LABELS,
  MITIGATION_DUE_FILTER_LABELS
} from '../../types/mitigation'
import { fetchSuppliers } from '../../services/supplierService'
import { fetchRisks } from '../../services/riskService'
import {
  fetchMitigations,
  updateMitigationStatus
} from '../../services/mitigationService'
import { useAuth } from '../../context/AuthContext'
import RiskLevelBadge from '../risks/RiskLevelBadge'
import MitigationStatusBadge from './MitigationStatusBadge'
import MitigationPriorityBadge from './MitigationPriorityBadge'
import MitigationForm from './MitigationForm'

interface Notification {
  type: 'success' | 'error'
  text: string
}

interface Props {
  /** Ha a Kockázatok oldalról egy adott kockázathoz navigáltunk ide */
  initialRiskId?: string | null
  onClearInitialRisk?: () => void
}

const ALL_STATUSES: MitigationStatus[] = ['Planned', 'InProgress', 'Completed', 'Cancelled']
const ALL_DUE_FILTERS: Array<Exclude<MitigationDueFilter, ''>> = ['overdue', 'upcoming', 'noduedate']

export default function MitigationList({ initialRiskId, onClearInitialRisk }: Props) {
  const { canManageMitigations } = useAuth()

  const [actions, setActions]           = useState<MitigationAction[]>([])
  const [risks, setRisks]               = useState<Risk[]>([])
  const [suppliers, setSuppliers]       = useState<Supplier[]>([])
  const [loading, setLoading]           = useState(true)
  const [error, setError]               = useState<string | null>(null)
  const [notification, setNotification] = useState<Notification | null>(null)

  // Szűrők állapota
  const [filterRiskId, setFilterRiskId]         = useState<string>(initialRiskId ?? '')
  const [filterSupplierId, setFilterSupplierId] = useState<string>('')
  const [filterStatus, setFilterStatus]         = useState<MitigationStatus | ''>('')
  const [filterDue, setFilterDue]               = useState<MitigationDueFilter>('')
  const [filterResponsible, setFilterResponsible] = useState<string>('')

  // Ha a Kockázatok oldalról új riskId érkezik, állítsuk be a szűrőt
  useEffect(() => {
    if (initialRiskId) {
      setFilterRiskId(initialRiskId)
    }
  }, [initialRiskId])

  // Modális űrlap állapota: null = zárva, 'new' = új rögzítése, MitigationAction = szerkesztés
  const [editTarget, setEditTarget]             = useState<MitigationAction | 'new' | null>(null)
  const [updatingStatusId, setUpdatingStatusId] = useState<string | null>(null)

  // Beszállítók és kockázatok betöltése a szűrőkhöz és a létrehozó űrlaphoz
  useEffect(() => {
    fetchSuppliers().then(setSuppliers).catch(() => undefined)
    fetchRisks().then(setRisks).catch(() => undefined)
  }, [])

  // Intézkedések betöltése a kiválasztott szűrőkkel
  const loadActions = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await fetchMitigations({
        riskId:      filterRiskId || undefined,
        supplierId:  filterSupplierId || undefined,
        status:      filterStatus || undefined,
        dueFilter:   filterDue || undefined,
        responsible: filterResponsible.trim() || undefined
      })
      setActions(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Nem sikerült betölteni az intézkedéseket.')
    } finally {
      setLoading(false)
    }
  }, [filterRiskId, filterSupplierId, filterStatus, filterDue, filterResponsible])

  useEffect(() => {
    loadActions()
  }, [loadActions])

  // Értesítés automatikus eltüntetése
  useEffect(() => {
    if (!notification) return
    const timer = setTimeout(() => setNotification(null), 4000)
    return () => clearTimeout(timer)
  }, [notification])

  // Összesítő mutatók a megjelenített intézkedésekről
  const summaryCounts = useMemo(() => {
    return {
      Overdue:    actions.filter(a => a.isOverdue).length,
      InProgress: actions.filter(a => a.status === 'InProgress').length,
      Planned:    actions.filter(a => a.status === 'Planned').length,
      Completed:  actions.filter(a => a.status === 'Completed').length,
    }
  }, [actions])

  const hasActiveFilter = Boolean(
    filterRiskId ||
    filterSupplierId ||
    filterStatus ||
    filterDue ||
    filterResponsible.trim()
  )

  function clearFilters() {
    setFilterRiskId('')
    setFilterSupplierId('')
    setFilterStatus('')
    setFilterDue('')
    setFilterResponsible('')
    onClearInitialRisk?.()
  }

  function handleSaved(saved: MitigationAction) {
    setEditTarget(null)
    setNotification({
      type: 'success',
      text: editTarget === 'new'
        ? `✅ „${saved.title}" intézkedés sikeresen rögzítve.`
        : `✅ „${saved.title}" intézkedés adatai frissítve.`
    })
    loadActions()
  }

  async function handleStatusChange(action: MitigationAction, newStatus: MitigationStatus) {
    if (action.status === newStatus) return
    setUpdatingStatusId(action.id)
    try {
      const updated = await updateMitigationStatus(action.id, { status: newStatus })
      setActions(prev => prev.map(a => (a.id === updated.id ? updated : a)))
      setNotification({
        type: 'success',
        text: `✅ „${updated.title}" új állapota: ${MITIGATION_STATUS_LABELS[updated.status]}.`
      })
    } catch (err) {
      setNotification({
        type: 'error',
        text: err instanceof Error ? err.message : 'A státusz módosítása sikertelen.'
      })
    } finally {
      setUpdatingStatusId(null)
    }
  }

  function formatDateOnly(isoString: string | null): string {
    if (!isoString) return 'Nincs határidő'
    return new Intl.DateTimeFormat('hu-HU', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).format(new Date(isoString))
  }

  function formatDateTime(isoString: string): string {
    return new Intl.DateTimeFormat('hu-HU', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    }).format(new Date(isoString))
  }

  // Ha beszállítóra szűrünk, a kockázat-legördülőben csak az adott beszállító kockázatait kínáljuk fel
  const filterableRisks = useMemo(() => {
    if (!filterSupplierId) return risks
    return risks.filter(r => r.supplierId === filterSupplierId)
  }, [risks, filterSupplierId])

  return (
    <div className="supplier-list-page">
      {/* Oldal fejléc + „Új intézkedés" gomb */}
      <div className="page-header">
        <div>
          <h2 className="page-title">Kockázatcsökkentő intézkedések</h2>
          <p className="page-subtitle">
            {loading
              ? 'Betöltés...'
              : `${actions.length} intézkedés${hasActiveFilter ? ' (szűrve)' : ''}`}
          </p>
        </div>
        {canManageMitigations && (
          <button className="btn btn-primary" onClick={() => setEditTarget('new')}>
            ➕ Új intézkedés rögzítése
          </button>
        )}
      </div>

      {/* Szűrősáv: beszállító, kockázat, státusz, határidő, felelős */}
      <div className="filter-bar">
        <div className="filter-group">
          <label htmlFor="mit-filter-supplier" className="filter-label">Beszállító:</label>
          <select
            id="mit-filter-supplier"
            className="filter-select"
            value={filterSupplierId}
            onChange={e => {
              setFilterSupplierId(e.target.value)
              setFilterRiskId('')
              onClearInitialRisk?.()
            }}
          >
            <option value="">Összes beszállító</option>
            {suppliers.map(s => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        </div>

        <div className="filter-group">
          <label htmlFor="mit-filter-risk" className="filter-label">Kockázat:</label>
          <select
            id="mit-filter-risk"
            className="filter-select"
            value={filterRiskId}
            onChange={e => {
              setFilterRiskId(e.target.value)
              if (!e.target.value) onClearInitialRisk?.()
            }}
          >
            <option value="">Összes kockázat</option>
            {filterableRisks.map(r => (
              <option key={r.id} value={r.id}>
                {r.title.length > 42 ? r.title.slice(0, 42) + '…' : r.title} ({r.supplierName})
              </option>
            ))}
          </select>
        </div>

        <div className="filter-group">
          <label htmlFor="mit-filter-status" className="filter-label">Állapot:</label>
          <select
            id="mit-filter-status"
            className="filter-select"
            value={filterStatus}
            onChange={e => setFilterStatus(e.target.value as MitigationStatus | '')}
          >
            <option value="">Összes állapot</option>
            {ALL_STATUSES.map(st => (
              <option key={st} value={st}>{MITIGATION_STATUS_LABELS[st]}</option>
            ))}
          </select>
        </div>

        <div className="filter-group">
          <label htmlFor="mit-filter-due" className="filter-label">Határidő:</label>
          <select
            id="mit-filter-due"
            className="filter-select"
            value={filterDue}
            onChange={e => setFilterDue(e.target.value as MitigationDueFilter)}
          >
            <option value="">Összes határidő</option>
            {ALL_DUE_FILTERS.map(df => (
              <option key={df} value={df}>{MITIGATION_DUE_FILTER_LABELS[df]}</option>
            ))}
          </select>
        </div>

        <div className="filter-group">
          <label htmlFor="mit-filter-resp" className="filter-label">Felelős:</label>
          <input
            id="mit-filter-resp"
            type="text"
            className="filter-select"
            placeholder="Név vagy egység..."
            value={filterResponsible}
            onChange={e => setFilterResponsible(e.target.value)}
          />
        </div>

        {hasActiveFilter && (
          <button className="btn btn-sm btn-secondary" onClick={clearFilters}>
            ✕ Szűrők törlése
          </button>
        )}
      </div>

      {/* Gyors összesítő sáv */}
      {!loading && !error && actions.length > 0 && (
        <div className="risk-summary-strip">
          <div className="risk-summary-pill risk-level-critical">
            <span>⏰ Lejárt határidejű:</span>
            <strong>{summaryCounts.Overdue}</strong>
          </div>
          <div className="risk-summary-pill risk-level-medium">
            <span>🔄 Folyamatban:</span>
            <strong>{summaryCounts.InProgress}</strong>
          </div>
          <div className="risk-summary-pill mitigation-pill-planned">
            <span>📋 Tervezett:</span>
            <strong>{summaryCounts.Planned}</strong>
          </div>
          <div className="risk-summary-pill risk-level-low">
            <span>✅ Befejezett:</span>
            <strong>{summaryCounts.Completed}</strong>
          </div>
        </div>
      )}

      {/* Értesítés */}
      {notification && (
        <div className={`notification notification-${notification.type}`} role="alert">
          {notification.text}
          <button className="notification-close" onClick={() => setNotification(null)}>✕</button>
        </div>
      )}

      {/* Betöltés */}
      {loading && (
        <div className="loading-state">
          <div className="spinner"></div>
          <span>Intézkedések betöltése...</span>
        </div>
      )}

      {/* Hibaállapot */}
      {!loading && error && (
        <div className="error-state" role="alert">
          <p>⚠️ {error}</p>
          <button className="btn btn-secondary" onClick={loadActions}>🔄 Újrapróbálás</button>
        </div>
      )}

      {/* Üres állapot */}
      {!loading && !error && actions.length === 0 && (
        <div className="empty-state">
          <div className="empty-icon">🔧</div>
          <p>
            {hasActiveFilter
              ? 'Nincs a kiválasztott szűrőfeltételeknek megfelelő kockázatcsökkentő intézkedés.'
              : 'Még nincs rögzített kockázatcsökkentő intézkedés a rendszerben.'}
          </p>
          {hasActiveFilter ? (
            <button className="btn btn-secondary" onClick={clearFilters}>
              Szűrők visszaállítása
            </button>
          ) : (
            canManageMitigations && (
              <button className="btn btn-primary" onClick={() => setEditTarget('new')}>
                Első intézkedés rögzítése
              </button>
            )
          )}
        </div>
      )}

      {/* Intézkedések táblázata */}
      {!loading && !error && actions.length > 0 && (
        <div className="table-wrapper">
          <table className="supplier-table">
            <thead>
              <tr>
                <th>Intézkedés megnevezése</th>
                <th>Kapcsolódó kockázat / Beszállító</th>
                <th>Felelős</th>
                <th>Határidő</th>
                <th>Prioritás</th>
                <th>Állapot</th>
                {canManageMitigations && <th className="col-actions">Műveletek</th>}
              </tr>
            </thead>
            <tbody>
              {actions.map(action => {
                const rowClass = action.isOverdue
                  ? 'row-overdue'
                  : action.status === 'Cancelled'
                    ? 'row-inactive'
                    : ''

                return (
                  <tr key={action.id} className={rowClass}>
                    <td>
                      <div className="supplier-name">{action.title}</div>
                      {action.description && (
                        <div className="supplier-desc-preview" title={action.description}>
                          {action.description.length > 85
                            ? action.description.slice(0, 85) + '…'
                            : action.description}
                        </div>
                      )}
                      {action.resultNotes && (
                        <div className="mitigation-result-preview" title={action.resultNotes}>
                          <strong>Eredmény:</strong>{' '}
                          {action.resultNotes.length > 85
                            ? action.resultNotes.slice(0, 85) + '…'
                            : action.resultNotes}
                        </div>
                      )}
                    </td>
                    <td>
                      <div className="mitigation-risk-cell">
                        <span className="supplier-name">{action.riskTitle}</span>
                        <div className="mitigation-risk-meta">
                          <span className="risk-supplier-link">{action.supplierName}</span>
                          {action.riskLevel && (
                            <RiskLevelBadge
                              level={action.riskLevel}
                              score={action.riskScore}
                            />
                          )}
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className="mitigation-responsible">{action.responsiblePerson}</span>
                    </td>
                    <td className="col-date">
                      <div className="mitigation-due-cell">
                        <span className={!action.dueDate ? 'due-none' : ''}>
                          {formatDateOnly(action.dueDate)}
                        </span>
                        {action.isOverdue && (
                          <span className="overdue-badge" title="A teljesítési határidő lejárt, és az intézkedés még nincs befejezve!">
                            ⏰ Lejárt!
                          </span>
                        )}
                        {action.status === 'Completed' && action.completedAt && (
                          <span className="completed-at-note">
                            Teljesítve: {formatDateTime(action.completedAt)}
                          </span>
                        )}
                      </div>
                    </td>
                    <td>
                      <MitigationPriorityBadge priority={action.priority} />
                    </td>
                    <td>
                      <MitigationStatusBadge status={action.status} />
                    </td>
                    {canManageMitigations && (
                      <td className="col-actions">
                        <div className="action-buttons">
                          <button
                            className="btn btn-sm btn-secondary"
                            onClick={() => setEditTarget(action)}
                            title="Intézkedés szerkesztése"
                          >
                            ✏️ Szerkesztés
                          </button>
                          <select
                            aria-label={`Státusz módosítása: ${action.title}`}
                            className="role-select"
                            value={action.status}
                            disabled={updatingStatusId === action.id}
                            onChange={e =>
                              handleStatusChange(action, e.target.value as MitigationStatus)
                            }
                          >
                            {ALL_STATUSES.map(st => (
                              <option key={st} value={st}>
                                {MITIGATION_STATUS_LABELS[st]}
                              </option>
                            ))}
                          </select>
                        </div>
                      </td>
                    )}
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Modális ablak (új intézkedés / szerkesztés) */}
      {editTarget !== null && (
        <MitigationForm
          action={editTarget === 'new' ? null : editTarget}
          risks={risks}
          defaultRiskId={filterRiskId || undefined}
          onSave={handleSaved}
          onCancel={() => setEditTarget(null)}
        />
      )}
    </div>
  )
}

