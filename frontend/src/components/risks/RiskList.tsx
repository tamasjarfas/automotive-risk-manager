// components/risks/RiskList.tsx
// Beszállítói információbiztonsági kockázati nyilvántartás és értékelő nézet.
//
// Funkciók:
//   - Kockázatok táblázatos megjelenítése (beszállító, kategória, P×I, pontszám, szint, státusz)
//   - Szűrés beszállító, kockázati szint (Low..Critical) és státusz szerint
//   - Összesítő sáv a megjelenített kockázatok súlyossági megoszlásáról
//   - Új kockázat rögzítése, szerkesztése és státuszváltása (Administrator és RiskManager részére)
//   - Viewer szerepkör esetén: csak olvasási nézet (a módosító vezérlők nem jelennek meg)

import { useState, useEffect, useCallback, useMemo } from 'react'
import type { Supplier } from '../../types/supplier'
import type { Risk, RiskLevel, RiskStatus } from '../../types/risk'
import {
  RISK_LEVEL_LABELS,
  RISK_STATUS_LABELS
} from '../../types/risk'
import { fetchSuppliers } from '../../services/supplierService'
import { fetchRisks, updateRiskStatus } from '../../services/riskService'
import { useAuth } from '../../context/AuthContext'
import RiskLevelBadge from './RiskLevelBadge'
import RiskStatusBadge from './RiskStatusBadge'
import RiskForm from './RiskForm'

interface Notification {
  type: 'success' | 'error'
  text: string
}

const ALL_LEVELS: RiskLevel[] = ['Critical', 'High', 'Medium', 'Low']
const ALL_STATUSES: RiskStatus[] = ['Open', 'UnderReview', 'Mitigated', 'Closed']

export default function RiskList() {
  const { canManageRisks } = useAuth()

  const [risks, setRisks]               = useState<Risk[]>([])
  const [suppliers, setSuppliers]       = useState<Supplier[]>([])
  const [loading, setLoading]           = useState(true)
  const [error, setError]               = useState<string | null>(null)
  const [notification, setNotification] = useState<Notification | null>(null)

  // Szűrők állapota
  const [filterSupplierId, setFilterSupplierId] = useState<string>('')
  const [filterLevel, setFilterLevel]           = useState<RiskLevel | ''>('')
  const [filterStatus, setFilterStatus]         = useState<RiskStatus | ''>('')

  // Modális űrlap állapota: null = zárva, 'new' = új rögzítése, Risk = szerkesztés
  const [editTarget, setEditTarget]             = useState<Risk | 'new' | null>(null)
  const [updatingStatusId, setUpdatingStatusId] = useState<string | null>(null)

  // Beszállítók betöltése a szűrőhöz és az űrlaphoz
  useEffect(() => {
    fetchSuppliers()
      .then(setSuppliers)
      .catch(() => {
        // Ha a beszállítók betöltése sikertelen, a kockázatok betöltése még jelezni fogja a hibát
      })
  }, [])

  // Kockázatok betöltése a kiválasztott szűrőkkel a backend /api/risks végpontjáról
  const loadRisks = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await fetchRisks({
        supplierId: filterSupplierId || undefined,
        level: filterLevel || undefined,
        status: filterStatus || undefined
      })
      setRisks(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Nem sikerült betölteni a kockázatokat.')
    } finally {
      setLoading(false)
    }
  }, [filterSupplierId, filterLevel, filterStatus])

  useEffect(() => {
    loadRisks()
  }, [loadRisks])

  // Értesítés automatikus eltüntetése
  useEffect(() => {
    if (!notification) return
    const timer = setTimeout(() => setNotification(null), 4000)
    return () => clearTimeout(timer)
  }, [notification])

  // Összesítő statisztika a jelenleg megjelenített kockázatokról
  const summaryCounts = useMemo(() => {
    return {
      Critical: risks.filter(r => r.level === 'Critical').length,
      High:     risks.filter(r => r.level === 'High').length,
      Medium:   risks.filter(r => r.level === 'Medium').length,
      Low:      risks.filter(r => r.level === 'Low').length,
    }
  }, [risks])

  const hasActiveFilter = Boolean(filterSupplierId || filterLevel || filterStatus)

  function clearFilters() {
    setFilterSupplierId('')
    setFilterLevel('')
    setFilterStatus('')
  }

  function handleSaved(saved: Risk) {
    setEditTarget(null)
    setNotification({
      type: 'success',
      text: editTarget === 'new'
        ? `✅ „${saved.title}" rögzítve (Pontszám: ${saved.score}, Szint: ${saved.level}).`
        : `✅ „${saved.title}" frissítve (Pontszám: ${saved.score}, Szint: ${saved.level}).`
    })
    loadRisks()
  }

  async function handleStatusChange(risk: Risk, newStatus: RiskStatus) {
    if (risk.status === newStatus) return
    setUpdatingStatusId(risk.id)
    try {
      const updated = await updateRiskStatus(risk.id, { status: newStatus })
      setRisks(prev => prev.map(r => (r.id === updated.id ? updated : r)))
      setNotification({
        type: 'success',
        text: `✅ „${updated.title}" új státusza: ${RISK_STATUS_LABELS[updated.status]}.`
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

  function formatDate(isoString: string): string {
    return new Intl.DateTimeFormat('hu-HU', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    }).format(new Date(isoString))
  }

  return (
    <div className="supplier-list-page">
      {/* Oldal fejléc + „Új kockázat" gomb */}
      <div className="page-header">
        <div>
          <h2 className="page-title">Beszállítói kockázatok nyilvántartása</h2>
          <p className="page-subtitle">
            {loading
              ? 'Betöltés...'
              : `${risks.length} kockázati bejegyzés${hasActiveFilter ? ' (szűrve)' : ''}`}
          </p>
        </div>
        {canManageRisks && (
          <button className="btn btn-primary" onClick={() => setEditTarget('new')}>
            ➕ Új kockázat rögzítése
          </button>
        )}
      </div>

      {/* Szűrősáv: beszállító, kockázati szint, státusz */}
      <div className="filter-bar">
        <div className="filter-group">
          <label htmlFor="filter-supplier" className="filter-label">Beszállító:</label>
          <select
            id="filter-supplier"
            className="filter-select"
            value={filterSupplierId}
            onChange={e => setFilterSupplierId(e.target.value)}
          >
            <option value="">Összes beszállító</option>
            {suppliers.map(s => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        </div>

        <div className="filter-group">
          <label htmlFor="filter-level" className="filter-label">Kockázati szint:</label>
          <select
            id="filter-level"
            className="filter-select"
            value={filterLevel}
            onChange={e => setFilterLevel(e.target.value as RiskLevel | '')}
          >
            <option value="">Összes szint</option>
            {ALL_LEVELS.map(lvl => (
              <option key={lvl} value={lvl}>{RISK_LEVEL_LABELS[lvl]}</option>
            ))}
          </select>
        </div>

        <div className="filter-group">
          <label htmlFor="filter-status" className="filter-label">Státusz:</label>
          <select
            id="filter-status"
            className="filter-select"
            value={filterStatus}
            onChange={e => setFilterStatus(e.target.value as RiskStatus | '')}
          >
            <option value="">Összes státusz</option>
            {ALL_STATUSES.map(st => (
              <option key={st} value={st}>{RISK_STATUS_LABELS[st]}</option>
            ))}
          </select>
        </div>

        {hasActiveFilter && (
          <button className="btn btn-sm btn-secondary" onClick={clearFilters}>
            ✕ Szűrők törlése
          </button>
        )}
      </div>

      {/* Gyors összesítő kártyák a kockázati szintek szerint */}
      {!loading && !error && risks.length > 0 && (
        <div className="risk-summary-strip">
          <div className="risk-summary-pill risk-level-critical">
            <span>Kritikus (17–25):</span>
            <strong>{summaryCounts.Critical}</strong>
          </div>
          <div className="risk-summary-pill risk-level-high">
            <span>Magas (10–16):</span>
            <strong>{summaryCounts.High}</strong>
          </div>
          <div className="risk-summary-pill risk-level-medium">
            <span>Közepes (5–9):</span>
            <strong>{summaryCounts.Medium}</strong>
          </div>
          <div className="risk-summary-pill risk-level-low">
            <span>Alacsony (1–4):</span>
            <strong>{summaryCounts.Low}</strong>
          </div>
        </div>
      )}

      {/* Értesítés (visszajelzés) */}
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
          <span>Kockázatok betöltése...</span>
        </div>
      )}

      {/* Hibaállapot */}
      {!loading && error && (
        <div className="error-state" role="alert">
          <p>⚠️ {error}</p>
          <button className="btn btn-secondary" onClick={loadRisks}>🔄 Újrapróbálás</button>
        </div>
      )}

      {/* Üres állapot */}
      {!loading && !error && risks.length === 0 && (
        <div className="empty-state">
          <div className="empty-icon">⚠️</div>
          <p>
            {hasActiveFilter
              ? 'Nincs a kiválasztott szűrőfeltételeknek megfelelő kockázat.'
              : 'Még nincs rögzített beszállítói kockázat a nyilvántartásban.'}
          </p>
          {hasActiveFilter ? (
            <button className="btn btn-secondary" onClick={clearFilters}>
              Szűrők visszaállítása
            </button>
          ) : (
            canManageRisks && (
              <button className="btn btn-primary" onClick={() => setEditTarget('new')}>
                Első kockázat rögzítése
              </button>
            )
          )}
        </div>
      )}

      {/* Kockázati táblázat */}
      {!loading && !error && risks.length > 0 && (
        <div className="table-wrapper">
          <table className="supplier-table">
            <thead>
              <tr>
                <th>Kockázat megnevezése</th>
                <th>Beszállító</th>
                <th>Kategória</th>
                <th>P × I</th>
                <th>Pontszám / Szint</th>
                <th>Státusz</th>
                <th>Rögzítve</th>
                {canManageRisks && <th className="col-actions">Műveletek</th>}
              </tr>
            </thead>
            <tbody>
              {risks.map(risk => (
                <tr key={risk.id} className={risk.status === 'Closed' ? 'row-inactive' : ''}>
                  <td>
                    <div className="supplier-name">{risk.title}</div>
                    {risk.description && (
                      <div className="supplier-desc-preview" title={risk.description}>
                        {risk.description.length > 90
                          ? risk.description.slice(0, 90) + '…'
                          : risk.description}
                      </div>
                    )}
                  </td>
                  <td>
                    <span className="risk-supplier-link">{risk.supplierName}</span>
                  </td>
                  <td>
                    <span className="risk-category-tag">{risk.category}</span>
                  </td>
                  <td className="col-matrix">
                    <span title={`Valószínűség: ${risk.probability}, Hatás: ${risk.impact}`}>
                      {risk.probability} × {risk.impact}
                    </span>
                  </td>
                  <td>
                    <RiskLevelBadge level={risk.level} score={risk.score} />
                  </td>
                  <td>
                    <RiskStatusBadge status={risk.status} />
                  </td>
                  <td className="col-date">{formatDate(risk.createdAt)}</td>
                  {canManageRisks && (
                    <td className="col-actions">
                      <div className="action-buttons">
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={() => setEditTarget(risk)}
                          title="Kockázat szerkesztése és újraértékelése"
                        >
                          ✏️ Szerkesztés
                        </button>
                        <select
                          aria-label={`Státusz módosítása: ${risk.title}`}
                          className="role-select"
                          value={risk.status}
                          disabled={updatingStatusId === risk.id}
                          onChange={e => handleStatusChange(risk, e.target.value as RiskStatus)}
                        >
                          {ALL_STATUSES.map(st => (
                            <option key={st} value={st}>
                              {RISK_STATUS_LABELS[st]}
                            </option>
                          ))}
                        </select>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Modális ablak (új kockázat / szerkesztés) */}
      {editTarget !== null && (
        <RiskForm
          risk={editTarget === 'new' ? null : editTarget}
          suppliers={suppliers}
          defaultSupplierId={filterSupplierId || undefined}
          onSave={handleSaved}
          onCancel={() => setEditTarget(null)}
        />
      )}
    </div>
  )
}

