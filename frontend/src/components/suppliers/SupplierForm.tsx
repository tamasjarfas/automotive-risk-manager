// components/suppliers/SupplierForm.tsx
// Modális párbeszédablak új beszállító rögzítéséhez és meglévő szerkesztéséhez.
//
// Props:
//   supplier  – ha megadott: szerkesztési mód; ha null: létrehozási mód
//   onSave    – mentés után meghívódó callback (a listát frissíti)
//   onCancel  – bezáráskor meghívódó callback

import { useState, useEffect } from 'react'
import type { Supplier } from '../../types/supplier'
import { createSupplier, updateSupplier } from '../../services/supplierService'

interface Props {
  supplier: Supplier | null   // null = új létrehozása, Supplier = szerkesztés
  onSave: (saved: Supplier) => void
  onCancel: () => void
}

export default function SupplierForm({ supplier, onSave, onCancel }: Props) {
  const isEdit = supplier !== null

  // Űrlap mezők állapota
  const [name, setName]             = useState(supplier?.name ?? '')
  const [activity, setActivity]     = useState(supplier?.activity ?? '')
  const [description, setDescription] = useState(supplier?.description ?? '')

  // Küldési állapot és hibaüzenetek
  const [submitting, setSubmitting] = useState(false)
  const [error, setError]           = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  // Ha a supplier prop megváltozik (pl. más bejegyzés szerkesztése), frissítjük a mezőket
  useEffect(() => {
    setName(supplier?.name ?? '')
    setActivity(supplier?.activity ?? '')
    setDescription(supplier?.description ?? '')
    setError(null)
    setFieldErrors({})
  }, [supplier])

  // Kliensoldalú validáció
  function validate(): boolean {
    const errors: Record<string, string> = {}
    if (!name.trim()) errors.name = 'A név megadása kötelező.'
    else if (name.trim().length > 200) errors.name = 'Legfeljebb 200 karakter.'
    if (!activity.trim()) errors.activity = 'A tevékenységi kör megadása kötelező.'
    else if (activity.trim().length > 300) errors.activity = 'Legfeljebb 300 karakter.'
    if (description.length > 2000) errors.description = 'Legfeljebb 2000 karakter.'
    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    setSubmitting(true)
    setError(null)

    try {
      const payload = {
        name: name.trim(),
        activity: activity.trim(),
        description: description.trim() || undefined
      }

      const saved = isEdit
        ? await updateSupplier(supplier!.id, payload)
        : await createSupplier(payload)

      onSave(saved)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ismeretlen hiba történt.')
    } finally {
      setSubmitting(false)
    }
  }

  // Modal háttérre kattintva bezárás
  function handleBackdropClick(e: React.MouseEvent<HTMLDivElement>) {
    if (e.target === e.currentTarget) onCancel()
  }

  return (
    <div className="modal-backdrop" onClick={handleBackdropClick} role="dialog" aria-modal="true">
      <div className="modal-box">
        <div className="modal-header">
          <h2>{isEdit ? 'Beszállító szerkesztése' : 'Új beszállító rögzítése'}</h2>
          <button className="modal-close-btn" onClick={onCancel} aria-label="Bezárás">✕</button>
        </div>

        {error && (
          <div className="form-error-banner" role="alert">
            ⚠️ {error}
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          {/* Név */}
          <div className="form-group">
            <label htmlFor="sup-name" className="form-label">
              Név <span className="required-mark">*</span>
            </label>
            <input
              id="sup-name"
              type="text"
              className={`form-input ${fieldErrors.name ? 'input-error' : ''}`}
              value={name}
              onChange={e => setName(e.target.value)}
              maxLength={200}
              placeholder="pl. Elektronikai Alkatrész Kft."
              disabled={submitting}
              autoFocus
            />
            {fieldErrors.name && <p className="field-error">{fieldErrors.name}</p>}
          </div>

          {/* Tevékenységi kör */}
          <div className="form-group">
            <label htmlFor="sup-activity" className="form-label">
              Tevékenységi kör <span className="required-mark">*</span>
            </label>
            <input
              id="sup-activity"
              type="text"
              className={`form-input ${fieldErrors.activity ? 'input-error' : ''}`}
              value={activity}
              onChange={e => setActivity(e.target.value)}
              maxLength={300}
              placeholder="pl. Elektronikai alkatrészgyártás"
              disabled={submitting}
            />
            {fieldErrors.activity && <p className="field-error">{fieldErrors.activity}</p>}
          </div>

          {/* Leírás */}
          <div className="form-group">
            <label htmlFor="sup-desc" className="form-label">
              Leírás <span className="optional-mark">(opcionális)</span>
            </label>
            <textarea
              id="sup-desc"
              className={`form-textarea ${fieldErrors.description ? 'input-error' : ''}`}
              value={description}
              onChange={e => setDescription(e.target.value)}
              maxLength={2000}
              rows={4}
              placeholder="Rövid leírás a beszállítóról, tevékenységeiről..."
              disabled={submitting}
            />
            <p className="char-count">{description.length} / 2000</p>
            {fieldErrors.description && <p className="field-error">{fieldErrors.description}</p>}
          </div>

          {/* Gombok */}
          <div className="modal-actions">
            <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={submitting}>
              Mégse
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? '⏳ Mentés...' : isEdit ? '💾 Módosítás mentése' : '➕ Létrehozás'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

