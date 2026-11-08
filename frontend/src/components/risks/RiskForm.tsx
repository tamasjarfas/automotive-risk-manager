// components/risks/RiskForm.tsx
// Modális űrlap új beszállítói kockázat rögzítéséhez és meglévő szerkesztéséhez.
//
// Kiemelt funkció:
//   A Valószínűség (1–5) és Hatás (1–5) kiválasztásakor az űrlap valós időben
//   megjeleníti a számított pontszámot (P × I) és a kockázati szintet (Low..Critical).
//   Mentéskor a kliens csak a bemeneti adatokat küldi el; a végső pontszámot és
//   besorolást a backend (RiskCalculator) számítja ki és érvényesíti.

import { useState, useEffect, useMemo } from 'react'
import type { Supplier } from '../../types/supplier'
import type { Risk, RiskStatus } from '../../types/risk'
import {
  RISK_CATEGORIES,
  PROBABILITY_SCALE,
  IMPACT_SCALE,
  RISK_STATUS_LABELS,
  previewRiskEvaluation
} from '../../types/risk'
import { createRisk, updateRisk } from '../../services/riskService'
import RiskLevelBadge from './RiskLevelBadge'

interface Props {
  risk: Risk | null          // null = új kockázat, Risk = meglévő szerkesztése
  suppliers: Supplier[]      // választható beszállítók listája
  defaultSupplierId?: string // ha a listában szűrve volt egy beszállítóra, alapértelmezetten felkínáljuk
  onSave: (saved: Risk) => void
  onCancel: () => void
}

const ALL_STATUSES: RiskStatus[] = ['Open', 'UnderReview', 'Mitigated', 'Closed']

export default function RiskForm({
  risk,
  suppliers,
  defaultSupplierId,
  onSave,
  onCancel
}: Props) {
  const isEdit = risk !== null

  // Csak az aktív beszállítókat kínáljuk fel új kockázatnál,
  // de szerkesztéskor megtartjuk a kockázathoz jelenleg rendelt (esetleg inaktív) beszállítót is.
  const selectableSuppliers = useMemo(() => {
    return suppliers.filter(
      s => s.status === 'Active' || (risk && s.id === risk.supplierId)
    )
  }, [suppliers, risk])

  const initialSupplierId =
    risk?.supplierId ??
    defaultSupplierId ??
    selectableSuppliers[0]?.id ??
    ''

  const [supplierId, setSupplierId]   = useState<string>(initialSupplierId)
  const [title, setTitle]             = useState<string>(risk?.title ?? '')
  const [category, setCategory]       = useState<string>(risk?.category ?? RISK_CATEGORIES[0])
  const [probability, setProbability] = useState<number>(risk?.probability ?? 3)
  const [impact, setImpact]           = useState<number>(risk?.impact ?? 3)
  const [status, setStatus]           = useState<RiskStatus>(risk?.status ?? 'Open')
  const [description, setDescription] = useState<string>(risk?.description ?? '')

  const [submitting, setSubmitting]   = useState(false)
  const [error, setError]             = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    setSupplierId(
      risk?.supplierId ??
      defaultSupplierId ??
      selectableSuppliers[0]?.id ??
      ''
    )
    setTitle(risk?.title ?? '')
    setCategory(risk?.category ?? RISK_CATEGORIES[0])
    setProbability(risk?.probability ?? 3)
    setImpact(risk?.impact ?? 3)
    setStatus(risk?.status ?? 'Open')
    setDescription(risk?.description ?? '')
    setError(null)
    setFieldErrors({})
  }, [risk, defaultSupplierId, selectableSuppliers])

  // Valós idejű kliensoldali előnézet (Valószínűség × Hatás)
  const preview = useMemo(
    () => previewRiskEvaluation(probability, impact),
    [probability, impact]
  )

  function validate(): boolean {
    const errors: Record<string, string> = {}

    if (!supplierId) {
      errors.supplierId = 'A beszállító kiválasztása kötelező.'
    }
    if (!title.trim()) {
      errors.title = 'A kockázat megnevezése kötelező.'
    } else if (title.trim().length > 200) {
      errors.title = 'Legfeljebb 200 karakter lehet.'
    }
    if (!category.trim()) {
      errors.category = 'A kockázati kategória kiválasztása kötelező.'
    }
    if (probability < 1 || probability > 5) {
      errors.probability = 'A valószínűség 1 és 5 közötti egész szám lehet.'
    }
    if (impact < 1 || impact > 5) {
      errors.impact = 'A hatás súlyossága 1 és 5 közötti egész szám lehet.'
    }
    if (description.length > 2000) {
      errors.description = 'A részletes leírás legfeljebb 2000 karakter lehet.'
    }

    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    setSubmitting(true)
    setError(null)

    try {
      const basePayload = {
        supplierId,
        title: title.trim(),
        category: category.trim(),
        probability,
        impact,
        description: description.trim() || undefined
      }

      const saved = isEdit
        ? await updateRisk(risk!.id, { ...basePayload, status })
        : await createRisk(basePayload)

      onSave(saved)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ismeretlen hiba történt a mentés során.')
    } finally {
      setSubmitting(false)
    }
  }

  function handleBackdropClick(e: React.MouseEvent<HTMLDivElement>) {
    if (e.target === e.currentTarget) onCancel()
  }

  return (
    <div className="modal-backdrop" onClick={handleBackdropClick} role="dialog" aria-modal="true">
      <div className="modal-box modal-box-wide">
        <div className="modal-header">
          <h2>{isEdit ? 'Kockázat szerkesztése és újraértékelése' : 'Új beszállítói kockázat rögzítése'}</h2>
          <button className="modal-close-btn" onClick={onCancel} aria-label="Bezárás">✕</button>
        </div>

        {error && (
          <div className="form-error-banner" role="alert">
            ⚠️ {error}
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          {/* Beszállító kiválasztása */}
          <div className="form-group">
            <label htmlFor="risk-supplier" className="form-label">
              Érintett beszállító <span className="required-mark">*</span>
            </label>
            <select
              id="risk-supplier"
              className={`form-input ${fieldErrors.supplierId ? 'input-error' : ''}`}
              value={supplierId}
              onChange={e => setSupplierId(e.target.value)}
              disabled={submitting || selectableSuppliers.length === 0}
            >
              {selectableSuppliers.length === 0 && (
                <option value="">-- Nincs elérhető aktív beszállító --</option>
              )}
              {selectableSuppliers.map(s => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.activity}){s.status === 'Inactive' ? ' [Inaktív]' : ''}
                </option>
              ))}
            </select>
            {fieldErrors.supplierId && <p className="field-error">{fieldErrors.supplierId}</p>}
          </div>

          {/* Kockázat megnevezése */}
          <div className="form-group">
            <label htmlFor="risk-title" className="form-label">
              Kockázat megnevezése <span className="required-mark">*</span>
            </label>
            <input
              id="risk-title"
              type="text"
              className={`form-input ${fieldErrors.title ? 'input-error' : ''}`}
              value={title}
              onChange={e => setTitle(e.target.value)}
              maxLength={200}
              placeholder="pl. Nem titkosított távoli VPN-hozzáférés a beszállítói hálózaton"
              disabled={submitting}
              autoFocus
            />
            {fieldErrors.title && <p className="field-error">{fieldErrors.title}</p>}
          </div>

          {/* Kategória és (szerkesztésnél) Státusz egy sorban */}
          <div className="form-row-2col">
            <div className="form-group">
              <label htmlFor="risk-category" className="form-label">
                Kockázati kategória <span className="required-mark">*</span>
              </label>
              <select
                id="risk-category"
                className={`form-input ${fieldErrors.category ? 'input-error' : ''}`}
                value={category}
                onChange={e => setCategory(e.target.value)}
                disabled={submitting}
              >
                {RISK_CATEGORIES.map(cat => (
                  <option key={cat} value={cat}>{cat}</option>
                ))}
                {!RISK_CATEGORIES.includes(category) && category && (
                  <option value={category}>{category}</option>
                )}
              </select>
              {fieldErrors.category && <p className="field-error">{fieldErrors.category}</p>}
            </div>

            {isEdit && (
              <div className="form-group">
                <label htmlFor="risk-status" className="form-label">
                  Kockázat státusza
                </label>
                <select
                  id="risk-status"
                  className="form-input"
                  value={status}
                  onChange={e => setStatus(e.target.value as RiskStatus)}
                  disabled={submitting}
                >
                  {ALL_STATUSES.map(st => (
                    <option key={st} value={st}>
                      {RISK_STATUS_LABELS[st]}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>

          {/* Valószínűség (1–5) és Hatás (1–5) */}
          <div className="form-row-2col">
            <div className="form-group">
              <label htmlFor="risk-probability" className="form-label">
                Bekövetkezési valószínűség (1–5) <span className="required-mark">*</span>
              </label>
              <select
                id="risk-probability"
                className={`form-input ${fieldErrors.probability ? 'input-error' : ''}`}
                value={probability}
                onChange={e => setProbability(Number(e.target.value))}
                disabled={submitting}
              >
                {PROBABILITY_SCALE.map(item => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>
              {fieldErrors.probability && <p className="field-error">{fieldErrors.probability}</p>}
            </div>

            <div className="form-group">
              <label htmlFor="risk-impact" className="form-label">
                Hatás súlyossága (1–5) <span className="required-mark">*</span>
              </label>
              <select
                id="risk-impact"
                className={`form-input ${fieldErrors.impact ? 'input-error' : ''}`}
                value={impact}
                onChange={e => setImpact(Number(e.target.value))}
                disabled={submitting}
              >
                {IMPACT_SCALE.map(item => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>
              {fieldErrors.impact && <p className="field-error">{fieldErrors.impact}</p>}
            </div>
          </div>

          {/* Élő kockázatértékelési előnézet (5×5 mátrix) */}
          <div className="risk-preview-box" aria-live="polite">
            <div className="risk-preview-header">
              <span className="risk-preview-title">📊 Kockázatértékelés előnézete (5×5 mátrix)</span>
              <span className="risk-preview-formula">
                Valószínűség ({probability}) × Hatás ({impact}) = <strong>{preview.score} pont</strong>
              </span>
            </div>
            <div className="risk-preview-body">
              <RiskLevelBadge level={preview.level} score={preview.score} />
              <span className="risk-preview-hint">
                1–4: Alacsony &nbsp;|&nbsp; 5–9: Közepes &nbsp;|&nbsp; 10–16: Magas &nbsp;|&nbsp; 17–25: Kritikus (szerveroldalon ellenőrizve)
              </span>
            </div>
          </div>

          {/* Részletes leírás */}
          <div className="form-group">
            <label htmlFor="risk-desc" className="form-label">
              Részletes szakmai leírás <span className="optional-mark">(opcionális)</span>
            </label>
            <textarea
              id="risk-desc"
              className={`form-textarea ${fieldErrors.description ? 'input-error' : ''}`}
              value={description}
              onChange={e => setDescription(e.target.value)}
              maxLength={2000}
              rows={4}
              placeholder="Írja le a fenyegetést, az érintett beszállítói rendszert vagy folyamatot, és a lehetséges biztonsági következményeket..."
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
            <button
              type="submit"
              className="btn btn-primary"
              disabled={submitting || selectableSuppliers.length === 0}
            >
              {submitting ? '⏳ Mentés...' : isEdit ? '💾 Módosítás mentése' : '➕ Kockázat rögzítése'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

