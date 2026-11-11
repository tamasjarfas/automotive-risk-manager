// components/mitigations/MitigationForm.tsx
// Modális űrlap új kockázatcsökkentő intézkedés rögzítéséhez és meglévő szerkesztéséhez.
//
// Üzleti elvek:
//   - Minden intézkedést egy létező kockázathoz (RiskId) kell rendelni.
//   - A megnevezés (Title) és a felelős (ResponsiblePerson) kötelező.
//   - A határidő (DueDate) opcionális: határidő nélküli intézkedések is rögzíthetők.
//   - Befejezett (Completed) állapotnál rögzíthető a végrehajtás eredménye (ResultNotes),
//     és a szerver automatikusan beállítja a CompletedAt időbélyeget.
//   - Az intézkedés befejezése nem módosítja automatikusan a kockázat pontszámát (Score).

import { useState, useEffect, useMemo } from 'react'
import type { Risk } from '../../types/risk'
import { RISK_LEVEL_SHORT_LABELS } from '../../types/risk'
import type {
  MitigationAction,
  MitigationPriority,
  MitigationStatus
} from '../../types/mitigation'
import {
  MITIGATION_PRIORITY_LABELS,
  MITIGATION_STATUS_LABELS
} from '../../types/mitigation'
import { createMitigation, updateMitigation } from '../../services/mitigationService'

interface Props {
  action: MitigationAction | null // null = új intézkedés, MitigationAction = szerkesztés
  risks: Risk[]                   // választható kockázatok listája
  defaultRiskId?: string          // ha egy adott kockázathoz nyitottuk meg az űrlapot
  onSave: (saved: MitigationAction) => void
  onCancel: () => void
}

const ALL_PRIORITIES: MitigationPriority[] = ['Low', 'Medium', 'High', 'Critical']
const ALL_STATUSES: MitigationStatus[]     = ['Planned', 'InProgress', 'Completed', 'Cancelled']

function isoToDateInputValue(isoString: string | null | undefined): string {
  if (!isoString) return ''
  const d = new Date(isoString)
  if (Number.isNaN(d.getTime())) return ''
  return d.toISOString().slice(0, 10)
}

export default function MitigationForm({
  action,
  risks,
  defaultRiskId,
  onSave,
  onCancel
}: Props) {
  const isEdit = action !== null

  // Új intézkedésnél az aktív (nem Closed) kockázatokat kínáljuk fel előre,
  // de ha szerkesztünk, megtartjuk a jelenleg hozzárendelt kockázatot is.
  const selectableRisks = useMemo(() => {
    return risks.filter(
      r => r.status !== 'Closed' || (action && r.id === action.riskId) || r.id === defaultRiskId
    )
  }, [risks, action, defaultRiskId])

  const initialRiskId =
    action?.riskId ??
    defaultRiskId ??
    selectableRisks[0]?.id ??
    ''

  const [riskId, setRiskId]                       = useState<string>(initialRiskId)
  const [title, setTitle]                         = useState<string>(action?.title ?? '')
  const [responsiblePerson, setResponsiblePerson] = useState<string>(action?.responsiblePerson ?? '')
  const [dueDate, setDueDate]                     = useState<string>(isoToDateInputValue(action?.dueDate))
  const [priority, setPriority]                   = useState<MitigationPriority>(action?.priority ?? 'Medium')
  const [status, setStatus]                       = useState<MitigationStatus>(action?.status ?? 'Planned')
  const [description, setDescription]             = useState<string>(action?.description ?? '')
  const [resultNotes, setResultNotes]             = useState<string>(action?.resultNotes ?? '')

  const [submitting, setSubmitting]   = useState(false)
  const [error, setError]             = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    setRiskId(
      action?.riskId ??
      defaultRiskId ??
      selectableRisks[0]?.id ??
      ''
    )
    setTitle(action?.title ?? '')
    setResponsiblePerson(action?.responsiblePerson ?? '')
    setDueDate(isoToDateInputValue(action?.dueDate))
    setPriority(action?.priority ?? 'Medium')
    setStatus(action?.status ?? 'Planned')
    setDescription(action?.description ?? '')
    setResultNotes(action?.resultNotes ?? '')
    setError(null)
    setFieldErrors({})
  }, [action, defaultRiskId, selectableRisks])

  function validate(): boolean {
    const errors: Record<string, string> = {}

    if (!riskId) {
      errors.riskId = 'A kapcsolódó kockázat kiválasztása kötelező.'
    }
    if (!title.trim()) {
      errors.title = 'Az intézkedés megnevezése kötelező.'
    } else if (title.trim().length > 200) {
      errors.title = 'Legfeljebb 200 karakter lehet.'
    }
    if (!responsiblePerson.trim()) {
      errors.responsiblePerson = 'A felelős megadása kötelező.'
    } else if (responsiblePerson.trim().length > 150) {
      errors.responsiblePerson = 'Legfeljebb 150 karakter lehet.'
    }
    if (description.length > 2000) {
      errors.description = 'A részletes leírás legfeljebb 2000 karakter lehet.'
    }
    if (resultNotes.length > 2000) {
      errors.resultNotes = 'Az eredmény / megjegyzés legfeljebb 2000 karakter lehet.'
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
      // Ha van kiválasztott dátum (YYYY-MM-DD), UTC éjféli ISO formátumként küldjük
      const dueDateIso = dueDate ? `${dueDate}T00:00:00.000Z` : null

      const payload = {
        riskId,
        title: title.trim(),
        responsiblePerson: responsiblePerson.trim(),
        dueDate: dueDateIso,
        priority,
        status,
        description: description.trim() || undefined,
        resultNotes: resultNotes.trim() || undefined
      }

      const saved = isEdit
        ? await updateMitigation(action!.id, payload)
        : await createMitigation(payload)

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
          <h2>{isEdit ? 'Kockázatcsökkentő intézkedés szerkesztése' : 'Új kockázatcsökkentő intézkedés'}</h2>
          <button className="modal-close-btn" onClick={onCancel} aria-label="Bezárás">✕</button>
        </div>

        {error && (
          <div className="form-error-banner" role="alert">
            ⚠️ {error}
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          {/* Kapcsolódó kockázat kiválasztása */}
          <div className="form-group">
            <label htmlFor="mitigation-risk" className="form-label">
              Kapcsolódó kockázat <span className="required-mark">*</span>
            </label>
            <select
              id="mitigation-risk"
              className={`form-input ${fieldErrors.riskId ? 'input-error' : ''}`}
              value={riskId}
              onChange={e => setRiskId(e.target.value)}
              disabled={submitting || selectableRisks.length === 0}
            >
              {selectableRisks.length === 0 && (
                <option value="">-- Nincs elérhető kockázat --</option>
              )}
              {selectableRisks.map(r => (
                <option key={r.id} value={r.id}>
                  [{r.supplierName}] {r.title} ({RISK_LEVEL_SHORT_LABELS[r.level]} – {r.score} pont)
                </option>
              ))}
            </select>
            {fieldErrors.riskId && <p className="field-error">{fieldErrors.riskId}</p>}
          </div>

          {/* Intézkedés megnevezése */}
          <div className="form-group">
            <label htmlFor="mitigation-title" className="form-label">
              Intézkedés megnevezése <span className="required-mark">*</span>
            </label>
            <input
              id="mitigation-title"
              type="text"
              className={`form-input ${fieldErrors.title ? 'input-error' : ''}`}
              value={title}
              onChange={e => setTitle(e.target.value)}
              maxLength={200}
              placeholder="pl. Kötelező MFA és VPN-tanúsítvány bevezetése a beszállítói fiókokra"
              disabled={submitting}
              autoFocus
            />
            {fieldErrors.title && <p className="field-error">{fieldErrors.title}</p>}
          </div>

          {/* Felelős és Határidő egy sorban */}
          <div className="form-row-2col">
            <div className="form-group">
              <label htmlFor="mitigation-responsible" className="form-label">
                Felelős személy / egység <span className="required-mark">*</span>
              </label>
              <input
                id="mitigation-responsible"
                type="text"
                className={`form-input ${fieldErrors.responsiblePerson ? 'input-error' : ''}`}
                value={responsiblePerson}
                onChange={e => setResponsiblePerson(e.target.value)}
                maxLength={150}
                placeholder="pl. Kovács János (IT biztonság)"
                disabled={submitting}
              />
              {fieldErrors.responsiblePerson && (
                <p className="field-error">{fieldErrors.responsiblePerson}</p>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="mitigation-duedate" className="form-label">
                Teljesítési határidő <span className="optional-mark">(opcionális)</span>
              </label>
              <input
                id="mitigation-duedate"
                type="date"
                className="form-input"
                value={dueDate}
                onChange={e => setDueDate(e.target.value)}
                disabled={submitting}
              />
            </div>
          </div>

          {/* Prioritás és Státusz egy sorban */}
          <div className="form-row-2col">
            <div className="form-group">
              <label htmlFor="mitigation-priority" className="form-label">
                Prioritás <span className="required-mark">*</span>
              </label>
              <select
                id="mitigation-priority"
                className="form-input"
                value={priority}
                onChange={e => setPriority(e.target.value as MitigationPriority)}
                disabled={submitting}
              >
                {ALL_PRIORITIES.map(p => (
                  <option key={p} value={p}>{MITIGATION_PRIORITY_LABELS[p]}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="mitigation-status" className="form-label">
                Végrehajtási állapot <span className="required-mark">*</span>
              </label>
              <select
                id="mitigation-status"
                className="form-input"
                value={status}
                onChange={e => setStatus(e.target.value as MitigationStatus)}
                disabled={submitting}
              >
                {ALL_STATUSES.map(st => (
                  <option key={st} value={st}>{MITIGATION_STATUS_LABELS[st]}</option>
                ))}
              </select>
            </div>
          </div>

          {/* Részletes leírás */}
          <div className="form-group">
            <label htmlFor="mitigation-desc" className="form-label">
              Intézkedés részletes leírása <span className="optional-mark">(opcionális)</span>
            </label>
            <textarea
              id="mitigation-desc"
              className={`form-textarea ${fieldErrors.description ? 'input-error' : ''}`}
              value={description}
              onChange={e => setDescription(e.target.value)}
              maxLength={2000}
              rows={3}
              placeholder="A végrehajtandó műszaki vagy szervezési lépések leírása..."
              disabled={submitting}
            />
            <p className="char-count">{description.length} / 2000</p>
            {fieldErrors.description && <p className="field-error">{fieldErrors.description}</p>}
          </div>

          {/* Végrehajtás eredménye / megjegyzések */}
          <div className="form-group">
            <label htmlFor="mitigation-result" className="form-label">
              Végrehajtás eredménye, bizonyítékok / megjegyzések{' '}
              <span className="optional-mark">
                {status === 'Completed' ? '(befejezésnél ajánlott rögzíteni)' : '(opcionális)'}
              </span>
            </label>
            <textarea
              id="mitigation-result"
              className={`form-textarea ${fieldErrors.resultNotes ? 'input-error' : ''}`}
              value={resultNotes}
              onChange={e => setResultNotes(e.target.value)}
              maxLength={2000}
              rows={3}
              placeholder="pl. Auditjegyzőkönyv száma, bevezetett kontroll eredménye, ellenőrzési megállapítások..."
              disabled={submitting}
            />
            <p className="char-count">{resultNotes.length} / 2000</p>
            {fieldErrors.resultNotes && <p className="field-error">{fieldErrors.resultNotes}</p>}
          </div>

          {/* Szakmai tájékoztató a maradványkockázat külön újraértékeléséről */}
          <div className="risk-preview-box">
            <div className="risk-preview-hint">
              ℹ️ <strong>Szakmai megjegyzés:</strong> Az intézkedés befejezése (<em>Befejezett</em> állapot)
              automatikusan rögzíti a teljesítés időpontját, de <strong>nem írja felül automatikusan</strong> a
              kapcsolódó kockázat valószínűségét, hatását és pontszámát. A maradványkockázat újraértékelése
              a <em>Kockázatok</em> modulban külön, tudatos RiskManager-döntésként végezhető el.
            </div>
          </div>

          {/* Gombok */}
          <div className="modal-actions">
            <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={submitting}>
              Mégse
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={submitting || selectableRisks.length === 0}
            >
              {submitting ? '⏳ Mentés...' : isEdit ? '💾 Módosítás mentése' : '➕ Intézkedés rögzítése'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

