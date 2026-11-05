// components/suppliers/SupplierList.tsx
// Teljes beszállítókezelő nézet: lista, szerkesztés, állapotváltás.

import { useState, useEffect, useCallback } from 'react'
import type { Supplier } from '../../types/supplier'
import {
  fetchSuppliers,
  updateSupplierStatus
} from '../../services/supplierService'
import SupplierStatusBadge from './SupplierStatusBadge'
import SupplierForm from './SupplierForm'

// Visszajelzési üzenet típusa
interface Notification {
  type: 'success' | 'error'
  text: string
}

export default function SupplierList() {
  const [suppliers, setSuppliers]       = useState<Supplier[]>([])
  const [loading, setLoading]           = useState(true)
  const [error, setError]               = useState<string | null>(null)
  const [notification, setNotification] = useState<Notification | null>(null)

  // Szerkesztési állapot: null = form zárva, Supplier = szerkesztés, 'new' = új létrehozás
  const [editTarget, setEditTarget]     = useState<Supplier | 'new' | null>(null)

  // Átmeneti tiltás az állapotváltó gombhoz (dupla kattintás ellen)
  const [togglingId, setTogglingId]     = useState<string | null>(null)

  // ─── Adatok betöltése ──────────────────────────────────────────────────────
  const loadSuppliers = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await fetchSuppliers()
      setSuppliers(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Nem sikerült betölteni a beszállítókat.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadSuppliers()
  }, [loadSuppliers])

  // ─── Értesítés automatikus eltüntetése ─────────────────────────────────────
  useEffect(() => {
    if (!notification) return
    const timer = setTimeout(() => setNotification(null), 4000)
    return () => clearTimeout(timer)
  }, [notification])

  // ─── Form mentés callback ──────────────────────────────────────────────────
  function handleSaved(saved: Supplier) {
    setSuppliers(prev => {
      const idx = prev.findIndex(s => s.id === saved.id)
      if (idx === -1) {
        // Új elem: lista elejére rakjuk, majd újra sorba rendezzük
        return [saved, ...prev].sort((a, b) => a.name.localeCompare(b.name, 'hu'))
      }
      // Meglévő elem frissítése
      const updated = [...prev]
      updated[idx] = saved
      return updated
    })
    setEditTarget(null)
    setNotification({
      type: 'success',
      text: editTarget === 'new'
        ? `✅ „${saved.name}" sikeresen rögzítve.`
        : `✅ „${saved.name}" adatai frissítve.`
    })
  }

  // ─── Állapotváltás (aktív ↔ inaktív) ─────────────────────────────────────
  async function handleToggleStatus(supplier: Supplier) {
    const newStatus = supplier.status === 'Active' ? 'Inactive' : 'Active'
    setTogglingId(supplier.id)
    try {
      const updated = await updateSupplierStatus(supplier.id, { status: newStatus })
      setSuppliers(prev => prev.map(s => s.id === updated.id ? updated : s))
      setNotification({
        type: 'success',
        text: `✅ „${updated.name}" állapota: ${newStatus === 'Active' ? 'Aktív' : 'Inaktív'}.`
      })
    } catch (err) {
      setNotification({
        type: 'error',
        text: err instanceof Error ? err.message : 'Az állapotváltás sikertelen.'
      })
    } finally {
      setTogglingId(null)
    }
  }

  // ─── Dátum formázás ────────────────────────────────────────────────────────
  function formatDate(isoString: string): string {
    return new Intl.DateTimeFormat('hu-HU', {
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit'
    }).format(new Date(isoString))
  }

  // ─── Renderelés ───────────────────────────────────────────────────────────
  return (
    <div className="supplier-list-page">

      {/* Oldal fejléc + „Új" gomb */}
      <div className="page-header">
        <div>
          <h2 className="page-title">Beszállítók</h2>
          <p className="page-subtitle">
            {loading ? 'Betöltés...' : `${suppliers.length} bejegyzés`}
          </p>
        </div>
        <button className="btn btn-primary" onClick={() => setEditTarget('new')}>
          ➕ Új beszállító
        </button>
      </div>

      {/* Értesítés (visszajelzés) */}
      {notification && (
        <div className={`notification notification-${notification.type}`} role="alert">
          {notification.text}
          <button className="notification-close" onClick={() => setNotification(null)}>✕</button>
        </div>
      )}

      {/* Betöltési és hiba állapot */}
      {loading && (
        <div className="loading-state">
          <div className="spinner"></div>
          <span>Beszállítók betöltése...</span>
        </div>
      )}

      {!loading && error && (
        <div className="error-state" role="alert">
          <p>⚠️ {error}</p>
          <button className="btn btn-secondary" onClick={loadSuppliers}>🔄 Újrapróbálás</button>
        </div>
      )}

      {/* Üres állapot */}
      {!loading && !error && suppliers.length === 0 && (
        <div className="empty-state">
          <div className="empty-icon">🏭</div>
          <p>Még nincs rögzített beszállító.</p>
          <button className="btn btn-primary" onClick={() => setEditTarget('new')}>
            Első beszállító rögzítése
          </button>
        </div>
      )}

      {/* Táblázat */}
      {!loading && !error && suppliers.length > 0 && (
        <div className="table-wrapper">
          <table className="supplier-table">
            <thead>
              <tr>
                <th>Név</th>
                <th>Tevékenységi kör</th>
                <th>Állapot</th>
                <th>Rögzítve</th>
                <th className="col-actions">Műveletek</th>
              </tr>
            </thead>
            <tbody>
              {suppliers.map(supplier => (
                <tr key={supplier.id} className={supplier.status === 'Inactive' ? 'row-inactive' : ''}>
                  <td>
                    <div className="supplier-name">{supplier.name}</div>
                    {supplier.description && (
                      <div className="supplier-desc-preview" title={supplier.description}>
                        {supplier.description.length > 80
                          ? supplier.description.slice(0, 80) + '…'
                          : supplier.description}
                      </div>
                    )}
                  </td>
                  <td>{supplier.activity}</td>
                  <td><SupplierStatusBadge status={supplier.status} /></td>
                  <td className="col-date">{formatDate(supplier.createdAt)}</td>
                  <td className="col-actions">
                    <div className="action-buttons">
                      <button
                        className="btn btn-sm btn-secondary"
                        onClick={() => setEditTarget(supplier)}
                        title="Szerkesztés"
                      >
                        ✏️ Szerkesztés
                      </button>
                      <button
                        className={`btn btn-sm ${supplier.status === 'Active' ? 'btn-warning' : 'btn-success'}`}
                        onClick={() => handleToggleStatus(supplier)}
                        disabled={togglingId === supplier.id}
                        title={supplier.status === 'Active' ? 'Inaktiválás' : 'Aktiválás'}
                      >
                        {togglingId === supplier.id
                          ? '⏳'
                          : supplier.status === 'Active' ? '⏸ Inaktiválás' : '▶ Aktiválás'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Modális ablak (szerkesztés / létrehozás) */}
      {editTarget !== null && (
        <SupplierForm
          supplier={editTarget === 'new' ? null : editTarget}
          onSave={handleSaved}
          onCancel={() => setEditTarget(null)}
        />
      )}
    </div>
  )
}

