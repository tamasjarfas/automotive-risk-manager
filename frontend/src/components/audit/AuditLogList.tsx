// components/audit/AuditLogList.tsx
// Csak olvasható (read-only) üzleti auditnapló felület.
// Kizárólag Administrator szerepkörrel érhető el (szerveroldalon is védett: 403 Forbidden).
//
// Funkciók:
//   - Időrendben csökkenő (legfrissebb elöl) auditbejegyzések táblázata
//   - Szűrés dátumtartományra, végrehajtóra, entitástípusra, műveletre és entitásazonosítóra
//   - Lapozás (oldalszám, oldalankénti elemszám, összes találat)
//   - Megnyitható részletező nézet a biztonságos (allowlist) mezőváltozásokhoz (régi → új érték)
//   - Semmilyen szerkesztési vagy törlési művelet nem érhető el (append-only napló)

import { useCallback, useEffect, useState } from 'react'
import { fetchAuditLogs } from '../../services/auditService'
import {
  AUDIT_ACTION_LABELS,
  AUDIT_ENTITY_TYPE_LABELS,
  AUDIT_FIELD_LABELS
} from '../../types/audit'
import type {
  AuditAction,
  AuditEntityType,
  AuditLogItem,
  AuditLogPage
} from '../../types/audit'

const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export default function AuditLogList() {
  const [data, setData] = useState<AuditLogPage | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Szűrők állapota
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [entityType, setEntityType] = useState('')
  const [action, setAction] = useState('')
  const [actor, setActor] = useState('')
  const [entityId, setEntityId] = useState('')
  const [filterValidationError, setFilterValidationError] = useState<string | null>(null)

  // Lapozás állapota
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)

  // Kiválasztott bejegyzés a részletező (változások) megjelenítéséhez
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const loadLogs = useCallback(async (targetPage: number) => {
    setFilterValidationError(null)

    const trimmedEntityId = entityId.trim()
    if (trimmedEntityId.length > 0 && !GUID_REGEX.test(trimmedEntityId)) {
      setFilterValidationError('Az entitásazonosító szűrőnek érvényes UUID (GUID) formátumúnak kell lennie.')
      return
    }

    let fromUtc: string | undefined
    let toUtc: string | undefined

    if (fromDate) {
      const start = new Date(`${fromDate}T00:00:00.000Z`)
      if (!isNaN(start.getTime())) {
        fromUtc = start.toISOString()
      }
    }

    if (toDate) {
      const end = new Date(`${toDate}T23:59:59.999Z`)
      if (!isNaN(end.getTime())) {
        toUtc = end.toISOString()
      }
    }

    if (fromUtc && toUtc && fromUtc > toUtc) {
      setFilterValidationError('A kezdő dátum nem lehet későbbi a záró dátumnál.')
      return
    }

    try {
      setLoading(true)
      setError(null)
      const result = await fetchAuditLogs({
        fromUtc,
        toUtc,
        entityType: entityType || undefined,
        action: action || undefined,
        actor: actor.trim() || undefined,
        entityId: trimmedEntityId || undefined,
        page: targetPage,
        pageSize
      })
      setData(result)
      setPage(result.page)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Hiba történt az auditnapló lekérdezése közben.'
      )
    } finally {
      setLoading(false)
    }
  }, [fromDate, toDate, entityType, action, actor, entityId, pageSize])

  useEffect(() => {
    loadLogs(page)
  }, [loadLogs, page])

  function handleClearFilters() {
    setFromDate('')
    setToDate('')
    setEntityType('')
    setAction('')
    setActor('')
    setEntityId('')
    setFilterValidationError(null)
    setPage(1)
  }

  function formatTimestamp(isoUtc: string): string {
    const d = new Date(isoUtc)
    if (isNaN(d.getTime())) return isoUtc
    return d.toLocaleString('hu-HU', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    })
  }

  function formatChangeValue(val: string | null): string {
    if (val === null || val === undefined || val === '') {
      return '—'
    }
    return val
  }

  function getActionBadgeClass(act: AuditAction): string {
    switch (act) {
      case 'Created':
      case 'Activated':
        return 'audit-action-badge audit-action-created'
      case 'Completed':
        return 'audit-action-badge audit-action-completed'
      case 'Deactivated':
        return 'audit-action-badge audit-action-deactivated'
      case 'RoleChanged':
        return 'audit-action-badge audit-action-role'
      case 'StatusChanged':
      case 'Reopened':
        return 'audit-action-badge audit-action-status'
      case 'Updated':
      default:
        return 'audit-action-badge audit-action-updated'
    }
  }

  function getEntityBadgeClass(et: AuditEntityType): string {
    switch (et) {
      case 'Supplier':
        return 'audit-entity-badge audit-entity-supplier'
      case 'Risk':
        return 'audit-entity-badge audit-entity-risk'
      case 'MitigationAction':
        return 'audit-entity-badge audit-entity-mitigation'
      case 'User':
      default:
        return 'audit-entity-badge audit-entity-user'
    }
  }

  const hasActiveFilters =
    Boolean(fromDate || toDate || entityType || action || actor.trim() || entityId.trim())

  const items: AuditLogItem[] = data?.items ?? []

  return (
    <div className="page-container">
      {/* ── Oldal fejléc ── */}
      <div className="page-header">
        <div>
          <h2 className="page-title">📋 Üzleti Auditnapló</h2>
          <p className="page-subtitle">
            Beszállítói, kockázatértékelési, intézkedési és felhasználókezelési események
            utólagos visszakövethetősége (csak olvasható, kizárólag Administrator jogosultsággal)
          </p>
        </div>
        <div className="header-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => loadLogs(page)}
            disabled={loading}
          >
            🔄 Frissítés
          </button>
        </div>
      </div>

      {/* ── Biztonsági tájékoztató sáv ── */}
      <div className="audit-security-banner">
        <span>🔒</span>
        <span>
          <strong>Integritás és adatvédelem:</strong> Az auditnapló csak hozzáfűzhető (append-only).
          Jelszavakat, jelszókivonatokat, munkamenet-azonosítókat és CSRF-tokeneket a rendszer soha nem naplóz;
          a hosszú szöveges mezőknél csak a módosítás ténye kerül rögzítésre.
        </span>
      </div>

      {/* ── Szűrősáv ── */}
      <div className="card filter-card">
        <div className="audit-filter-grid">
          <div className="form-group">
            <label htmlFor="audit-from-date">Kezdő dátum (UTC)</label>
            <input
              id="audit-from-date"
              type="date"
              className="form-control"
              value={fromDate}
              onChange={e => {
                setFromDate(e.target.value)
                setPage(1)
              }}
            />
          </div>

          <div className="form-group">
            <label htmlFor="audit-to-date">Záró dátum (UTC)</label>
            <input
              id="audit-to-date"
              type="date"
              className="form-control"
              value={toDate}
              onChange={e => {
                setToDate(e.target.value)
                setPage(1)
              }}
            />
          </div>

          <div className="form-group">
            <label htmlFor="audit-entity-type">Érintett entitás típusa</label>
            <select
              id="audit-entity-type"
              className="form-control"
              value={entityType}
              onChange={e => {
                setEntityType(e.target.value)
                setPage(1)
              }}
            >
              <option value="">Összes entitástípus</option>
              <option value="Supplier">Beszállító (Supplier)</option>
              <option value="Risk">Kockázat (Risk)</option>
              <option value="MitigationAction">Intézkedés (MitigationAction)</option>
              <option value="User">Felhasználó (User)</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="audit-action">Művelet típusa</label>
            <select
              id="audit-action"
              className="form-control"
              value={action}
              onChange={e => {
                setAction(e.target.value)
                setPage(1)
              }}
            >
              <option value="">Összes művelet</option>
              <option value="Created">Létrehozás (Created)</option>
              <option value="Updated">Módosítás (Updated)</option>
              <option value="StatusChanged">Státuszváltás (StatusChanged)</option>
              <option value="Completed">Befejezés (Completed)</option>
              <option value="Reopened">Újranyitás (Reopened)</option>
              <option value="Activated">Aktiválás (Activated)</option>
              <option value="Deactivated">Inaktiválás (Deactivated)</option>
              <option value="RoleChanged">Szerepkörváltás (RoleChanged)</option>
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="audit-actor">Végrehajtó felhasználó</label>
            <input
              id="audit-actor"
              type="text"
              className="form-control"
              placeholder="pl. admin"
              value={actor}
              onChange={e => {
                setActor(e.target.value)
                setPage(1)
              }}
            />
          </div>

          <div className="form-group">
            <label htmlFor="audit-entity-id">Entitás azonosító (UUID)</label>
            <input
              id="audit-entity-id"
              type="text"
              className="form-control"
              placeholder="pl. 11111111-1111-..."
              value={entityId}
              onChange={e => {
                setEntityId(e.target.value)
                setPage(1)
              }}
            />
          </div>
        </div>

        <div className="audit-filter-footer">
          <div className="audit-page-size">
            <label htmlFor="audit-page-size-select">Oldalméret:</label>
            <select
              id="audit-page-size-select"
              className="form-control form-control-sm"
              value={pageSize}
              onChange={e => {
                setPageSize(Number(e.target.value))
                setPage(1)
              }}
            >
              <option value={10}>10 / oldal</option>
              <option value={20}>20 / oldal</option>
              <option value={50}>50 / oldal</option>
            </select>
          </div>

          {hasActiveFilters && (
            <button
              type="button"
              className="btn btn-sm btn-secondary"
              onClick={handleClearFilters}
            >
              ✕ Szűrők törlése
            </button>
          )}
        </div>

        {filterValidationError && (
          <div className="alert alert-error" style={{ marginTop: '0.75rem' }}>
            ⚠️ {filterValidationError}
          </div>
        )}
      </div>

      {/* ── Hibaállapot ── */}
      {error && (
        <div className="alert alert-error">
          <div>
            <strong>Hiba az auditnapló betöltésekor:</strong> {error}
          </div>
          <button
            type="button"
            className="btn btn-sm btn-secondary"
            onClick={() => loadLogs(page)}
          >
            Újrapróbálás
          </button>
        </div>
      )}

      {/* ── Betöltés ── */}
      {loading && (
        <div className="loading-state">
          <div className="spinner"></div>
          <span>Auditnapló bejegyzések betöltése...</span>
        </div>
      )}

      {/* ── Táblázat és lapozás ── */}
      {!loading && !error && (
        <div className="card">
          <div className="audit-summary-bar">
            <span>
              Összesen <strong>{data?.totalCount ?? 0}</strong> naplóbejegyzés
              {hasActiveFilters ? ' (szűrt találatok)' : ''}
            </span>
            {data && data.totalCount > 0 && (
              <span>
                Oldal: <strong>{data.page}</strong> / {data.totalPages}
              </span>
            )}
          </div>

          {items.length === 0 ? (
            <div className="empty-state">
              <div className="empty-icon">📋</div>
              <h3>Nincs megjeleníthető auditnapló bejegyzés</h3>
              <p>
                {hasActiveFilters
                  ? 'A megadott szűrési feltételeknek egyetlen naplóbejegyzés sem felel meg.'
                  : 'Még nem történt naplózott üzleti esemény a rendszerben.'}
              </p>
            </div>
          ) : (
            <div className="table-responsive">
              <table className="data-table audit-table">
                <thead>
                  <tr>
                    <th>Időpont</th>
                    <th>Végrehajtó</th>
                    <th>Művelet</th>
                    <th>Entitás</th>
                    <th>Entitás ID</th>
                    <th>Összefoglaló</th>
                    <th className="text-right">Változások</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map(item => {
                    const isExpanded = expandedId === item.id
                    return (
                      <AuditLogRow
                        key={item.id}
                        item={item}
                        isExpanded={isExpanded}
                        onToggleExpand={() =>
                          setExpandedId(prev => (prev === item.id ? null : item.id))
                        }
                        onFilterByEntityId={id => {
                          setEntityId(id)
                          setPage(1)
                        }}
                        formatTimestamp={formatTimestamp}
                        formatChangeValue={formatChangeValue}
                        getActionBadgeClass={getActionBadgeClass}
                        getEntityBadgeClass={getEntityBadgeClass}
                      />
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}

          {/* ── Lapozó sáv ── */}
          {data && data.totalPages > 1 && (
            <div className="audit-pagination">
              <button
                type="button"
                className="btn btn-sm btn-secondary"
                disabled={page <= 1 || loading}
                onClick={() => setPage(p => Math.max(1, p - 1))}
              >
                ← Előző oldal
              </button>
              <span className="audit-pagination-info">
                {data.page}. oldal (összesen {data.totalPages} oldal, {data.totalCount} bejegyzés)
              </span>
              <button
                type="button"
                className="btn btn-sm btn-secondary"
                disabled={page >= data.totalPages || loading}
                onClick={() => setPage(p => Math.min(data.totalPages, p + 1))}
              >
                Következő oldal →
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

interface RowProps {
  item: AuditLogItem
  isExpanded: boolean
  onToggleExpand: () => void
  onFilterByEntityId: (entityId: string) => void
  formatTimestamp: (isoUtc: string) => string
  formatChangeValue: (val: string | null) => string
  getActionBadgeClass: (action: AuditAction) => string
  getEntityBadgeClass: (entityType: AuditEntityType) => string
}

function AuditLogRow({
  item,
  isExpanded,
  onToggleExpand,
  onFilterByEntityId,
  formatTimestamp,
  formatChangeValue,
  getActionBadgeClass,
  getEntityBadgeClass
}: RowProps) {
  const shortEntityId = item.entityId.slice(0, 8)
  const changeCount = item.changeItems?.length ?? 0

  return (
    <>
      <tr className={isExpanded ? 'audit-row-expanded' : ''}>
        <td className="audit-timestamp-cell">
          {formatTimestamp(item.timestampUtc)}
        </td>
        <td>
          <span className="audit-actor-badge" title={item.actorUserId ?? 'Rendszer'}>
            👤 {item.actorUserName}
          </span>
        </td>
        <td>
          <span className={getActionBadgeClass(item.action)}>
            {AUDIT_ACTION_LABELS[item.action] ?? item.action}
          </span>
        </td>
        <td>
          <span className={getEntityBadgeClass(item.entityType)}>
            {AUDIT_ENTITY_TYPE_LABELS[item.entityType] ?? item.entityType}
          </span>
        </td>
        <td>
          <button
            type="button"
            className="audit-entity-id-btn"
            title={`Szűrés erre az entitásra: ${item.entityId}`}
            onClick={() => onFilterByEntityId(item.entityId)}
          >
            <code>{shortEntityId}…</code>
          </button>
        </td>
        <td className="audit-summary-cell">{item.summary}</td>
        <td className="text-right">
          {changeCount > 0 ? (
            <button
              type="button"
              className="btn btn-sm btn-secondary"
              onClick={onToggleExpand}
            >
              {isExpanded ? '▲ Elrejtés' : `▼ Részletek (${changeCount})`}
            </button>
          ) : (
            <span className="text-muted">Nincs mezőlista</span>
          )}
        </td>
      </tr>

      {isExpanded && changeCount > 0 && (
        <tr className="audit-details-row">
          <td colSpan={7}>
            <div className="audit-details-panel">
              <div className="audit-details-header">
                <span>
                  <strong>Bejegyzés ID:</strong> <code>{item.id}</code>
                </span>
                <span>
                  <strong>Érintett entitás ID:</strong> <code>{item.entityId}</code>
                </span>
                <span>
                  <strong>UTC időbélyeg:</strong> <code>{item.timestampUtc}</code>
                </span>
              </div>

              <table className="audit-changes-table">
                <thead>
                  <tr>
                    <th>Mező</th>
                    <th>Korábbi érték</th>
                    <th>Új érték</th>
                  </tr>
                </thead>
                <tbody>
                  {item.changeItems.map((chg, idx) => (
                    <tr key={`${item.id}-${chg.field}-${idx}`}>
                      <td>
                        <strong>{AUDIT_FIELD_LABELS[chg.field] ?? chg.field}</strong>{' '}
                        <code className="audit-field-code">({chg.field})</code>
                      </td>
                      <td className="audit-old-value">{formatChangeValue(chg.oldValue)}</td>
                      <td className="audit-new-value">{formatChangeValue(chg.newValue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </td>
        </tr>
      )}
    </>
  )
}

