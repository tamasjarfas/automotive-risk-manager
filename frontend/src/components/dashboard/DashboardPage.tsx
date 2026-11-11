// components/dashboard/DashboardPage.tsx
// Vezetői Dashboard modul (6. fejlesztési szakasz).
//
// Felépítése:
//   1. Üdvözlő fejléc + utolsó frissítés időpontja + „🔄 Frissítés" gomb
//   2. 5 db vezetői KPI-kártya (Összes beszállító, Összes kockázat, Magas és kritikus
//      kockázatok, Nyitott intézkedések, Lejárt határidejű intézkedések)
//   3. 2 db SVG + szöveges jelmagyarázattal ellátott megoszlási diagram:
//      - Kockázatok megoszlása kockázati szint szerint (Low, Medium, High, Critical)
//      - Intézkedések megoszlása végrehajtási állapot szerint (Planned, InProgress, Completed, Cancelled)
//   4. 3 db figyelmet igénylő lista (max. 5-5 elem, kattintható navigációval):
//      - Legmagasabb pontszámú nyitott / felülvizsgálat alatt álló kockázatok
//      - Lejárt határidejű, még nem befejezett intézkedések
//      - Közelgő határidők a következő 7 napon belül
//   5. Rendszer- és adatbázis-kapcsolat állapotjelző

import { useState, useEffect, useCallback } from 'react'
import type { DashboardSummary, DashboardMitigationItem } from '../../types/dashboard'
import { fetchDashboardSummary } from '../../services/dashboardService'
import type { HealthResponse } from '../../services/healthService'
import StatusBadge from '../StatusBadge'
import RiskLevelBadge from '../risks/RiskLevelBadge'
import RiskStatusBadge from '../risks/RiskStatusBadge'
import MitigationStatusBadge from '../mitigations/MitigationStatusBadge'
import MitigationPriorityBadge from '../mitigations/MitigationPriorityBadge'
import type { Page } from '../layout/Navigation'

interface ChartSegment {
  key: string
  label: string
  count: number
  color: string
  bgTint: string
}

interface Props {
  userDisplayName: string
  health: HealthResponse | null
  healthLoading: boolean
  healthError: string | null
  onRetryHealth: () => void
  onNavigate: (page: Page) => void
  onOpenRiskMitigations: (riskId: string) => void
}

/**
 * Akadálymentes SVG fánkdiagram (donut chart) + vízszintes aránysáv + szöveges jelmagyarázat.
 * Üres adathalmaz (total === 0) esetén is tisztán, hibamentesen jelenik meg.
 */
function DistributionChartCard({
  title,
  subtitle,
  totalLabel,
  totalCount,
  segments,
  footerNote
}: {
  title: string
  subtitle: string
  totalLabel: string
  totalCount: number
  segments: ChartSegment[]
  footerNote?: string
}) {
  const radius = 54
  const circumference = 2 * Math.PI * radius
  let cumulativeFraction = 0

  return (
    <div className="dashboard-chart-card">
      <div className="dashboard-card-header">
        <div>
          <h3 className="dashboard-card-title">{title}</h3>
          <p className="dashboard-card-subtitle">{subtitle}</p>
        </div>
      </div>

      <div className="chart-body">
        {/* SVG fánkdiagram */}
        <div className="donut-wrapper" role="img" aria-label={`${title} – összesen ${totalCount}`}>
          <svg viewBox="0 0 140 140" className="donut-svg">
            {/* Alapgyűrű (üres állapotban is látszik) */}
            <circle
              cx="70"
              cy="70"
              r={radius}
              fill="transparent"
              stroke="#e2e8f0"
              strokeWidth="18"
            />
            {totalCount > 0 &&
              segments.map(seg => {
                if (seg.count <= 0) return null
                const fraction = seg.count / totalCount
                const dashLength = fraction * circumference
                const dashOffset = -cumulativeFraction * circumference
                cumulativeFraction += fraction

                return (
                  <circle
                    key={seg.key}
                    cx="70"
                    cy="70"
                    r={radius}
                    fill="transparent"
                    stroke={seg.color}
                    strokeWidth="18"
                    strokeDasharray={`${dashLength} ${circumference}`}
                    strokeDashoffset={dashOffset}
                    transform="rotate(-90 70 70)"
                  >
                    <title>{`${seg.label}: ${seg.count} db (${Math.round(fraction * 100)}%)`}</title>
                  </circle>
                )
              })}
          </svg>
          <div className="donut-center">
            <span className="donut-center-value">{totalCount}</span>
            <span className="donut-center-label">{totalLabel}</span>
          </div>
        </div>

        {/* Szöveges jelmagyarázat és aránysávok */}
        <div className="chart-legend">
          {segments.map(seg => {
            const pct = totalCount > 0 ? Math.round((seg.count / totalCount) * 100) : 0
            return (
              <div key={seg.key} className="chart-legend-row">
                <div className="chart-legend-top">
                  <span className="chart-legend-label">
                    <span
                      className="chart-legend-dot"
                      style={{ backgroundColor: seg.color }}
                    />
                    {seg.label}
                  </span>
                  <span className="chart-legend-values">
                    <strong>{seg.count} db</strong>
                    <span className="chart-legend-pct">({pct}%)</span>
                  </span>
                </div>
                <div className="chart-bar-track" aria-hidden="true">
                  <div
                    className="chart-bar-fill"
                    style={{
                      width: `${pct}%`,
                      backgroundColor: seg.color
                    }}
                  />
                </div>
              </div>
            )
          })}
        </div>
      </div>

      {footerNote && <div className="chart-footer-note">{footerNote}</div>}
    </div>
  )
}

export default function DashboardPage({
  userDisplayName,
  health,
  healthLoading,
  healthError,
  onRetryHealth,
  onNavigate,
  onOpenRiskMitigations
}: Props) {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState<boolean>(true)
  const [error, setError]     = useState<string | null>(null)

  const loadSummary = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await fetchDashboardSummary()
      setSummary(data)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Nem sikerült betölteni a vezetői Dashboard összesítő adatait.'
      )
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadSummary()
  }, [loadSummary])

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
      minute: '2-digit',
      second: '2-digit'
    }).format(new Date(isoString))
  }

  const riskSegments: ChartSegment[] = summary
    ? [
        {
          key: 'Critical',
          label: 'Kritikus (17–25 pont)',
          count: summary.risks.criticalCount,
          color: '#dc2626',
          bgTint: '#fef2f2'
        },
        {
          key: 'High',
          label: 'Magas (10–16 pont)',
          count: summary.risks.highCount,
          color: '#ea580c',
          bgTint: '#fff7ed'
        },
        {
          key: 'Medium',
          label: 'Közepes (5–9 pont)',
          count: summary.risks.mediumCount,
          color: '#ca8a04',
          bgTint: '#fefce8'
        },
        {
          key: 'Low',
          label: 'Alacsony (1–4 pont)',
          count: summary.risks.lowCount,
          color: '#16a34a',
          bgTint: '#f0fdf4'
        }
      ]
    : []

  const mitigationSegments: ChartSegment[] = summary
    ? [
        {
          key: 'Planned',
          label: 'Tervezett',
          count: summary.mitigations.plannedCount,
          color: '#2563eb',
          bgTint: '#eff6ff'
        },
        {
          key: 'InProgress',
          label: 'Folyamatban',
          count: summary.mitigations.inProgressCount,
          color: '#ca8a04',
          bgTint: '#fefce8'
        },
        {
          key: 'Completed',
          label: 'Befejezett',
          count: summary.mitigations.completedCount,
          color: '#16a34a',
          bgTint: '#f0fdf4'
        },
        {
          key: 'Cancelled',
          label: 'Visszavont',
          count: summary.mitigations.cancelledCount,
          color: '#64748b',
          bgTint: '#f1f5f9'
        }
      ]
    : []

  function renderMitigationAttentionRow(item: DashboardMitigationItem) {
    return (
      <div
        key={item.id}
        className={`attention-item ${item.isOverdue ? 'attention-item-overdue' : ''}`}
      >
        <div className="attention-item-main">
          <div className="attention-item-title-row">
            <span className="attention-item-title">{item.title}</span>
            <MitigationPriorityBadge priority={item.priority} />
            <MitigationStatusBadge status={item.status} />
          </div>
          <div className="attention-item-meta">
            <span>🏭 <strong>{item.supplierName}</strong></span>
            <span>•</span>
            <span>⚠️ {item.riskTitle} ({item.riskScore} pont)</span>
            <span>•</span>
            <span>👤 Felelős: <strong>{item.responsiblePerson}</strong></span>
          </div>
        </div>
        <div className="attention-item-side">
          <div className="attention-item-due">
            <span className="attention-due-label">Határidő:</span>
            <strong>{formatDateOnly(item.dueDate)}</strong>
            {item.isOverdue && <span className="overdue-badge">⏰ Lejárt!</span>}
          </div>
          <button
            type="button"
            className="btn btn-sm btn-secondary"
            onClick={() => onOpenRiskMitigations(item.riskId)}
            title="Ugrás az intézkedéshez"
          >
            Megnyitás →
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="dashboard-page">
      {/* ── 1. Vezetői fejléc + Frissítés gomb ── */}
      <section className="hero dashboard-hero">
        <div className="dashboard-hero-top">
          <div>
            <h2>Vezetői áttekintés – Üdvözöljük, {userDisplayName}!</h2>
            <p>
              Valós idejű beszállítói információbiztonsági mutatók, kockázati megoszlások
              és kiemelt figyelmet igénylő intézkedések.
            </p>
          </div>
          <div className="dashboard-hero-actions">
            {summary && !loading && (
              <span className="dashboard-timestamp">
                Frissítve: {formatDateTime(summary.generatedAtUtc)}
              </span>
            )}
            <button
              type="button"
              className="btn btn-secondary dashboard-refresh-btn"
              onClick={loadSummary}
              disabled={loading}
            >
              {loading ? '⏳ Frissítés...' : '🔄 Adatok frissítése'}
            </button>
          </div>
        </div>
      </section>

      {/* ── Betöltési állapot ── */}
      {loading && (
        <div className="loading-state">
          <div className="spinner"></div>
          <span>Vezetői mutatók és összesítések betöltése...</span>
        </div>
      )}

      {/* ── Hibaállapot ── */}
      {!loading && error && (
        <div className="error-state" role="alert">
          <p>⚠️ {error}</p>
          <button className="btn btn-secondary" onClick={loadSummary}>
            🔄 Újrapróbálás
          </button>
        </div>
      )}

      {/* ── Fő Dashboard tartalom ── */}
      {!loading && !error && summary && (
        <>
          {/* ── 2. Vezetői KPI-kártyák (5 db) ── */}
          <section aria-label="Vezetői fő mutatók (KPI)">
            <div className="kpi-grid">
              {/* KPI 1: Összes beszállító */}
              <div
                className="kpi-card kpi-card-clickable"
                onClick={() => onNavigate('suppliers')}
                role="button"
                tabIndex={0}
                onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && onNavigate('suppliers')}
              >
                <div className="kpi-header">
                  <span className="kpi-label">Összes beszállító</span>
                  <span className="kpi-icon">🏭</span>
                </div>
                <div className="kpi-value">{summary.suppliers.totalCount}</div>
                <div className="kpi-breakdown">
                  <span className="kpi-tag kpi-tag-success">
                    Aktív: {summary.suppliers.activeCount}
                  </span>
                  <span className="kpi-tag kpi-tag-muted">
                    Inaktív: {summary.suppliers.inactiveCount}
                  </span>
                </div>
              </div>

              {/* KPI 2: Összes kockázat */}
              <div
                className="kpi-card kpi-card-clickable"
                onClick={() => onNavigate('risks')}
                role="button"
                tabIndex={0}
                onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && onNavigate('risks')}
              >
                <div className="kpi-header">
                  <span className="kpi-label">Összes kockázat</span>
                  <span className="kpi-icon">⚠️</span>
                </div>
                <div className="kpi-value">{summary.risks.totalCount}</div>
                <div className="kpi-breakdown">
                  <span className="kpi-tag kpi-tag-info">
                    Nyitott: {summary.risks.openCount + summary.risks.underReviewCount}
                  </span>
                  <span className="kpi-tag kpi-tag-muted">
                    Lezárt: {summary.risks.closedCount}
                  </span>
                </div>
              </div>

              {/* KPI 3: Magas és kritikus kockázatok összevont száma */}
              <div
                className={`kpi-card kpi-card-clickable ${
                  summary.risks.highAndCriticalCount > 0 ? 'kpi-card-warning' : ''
                }`}
                onClick={() => onNavigate('risks')}
                role="button"
                tabIndex={0}
                onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && onNavigate('risks')}
              >
                <div className="kpi-header">
                  <span className="kpi-label">Magas és kritikus kockázatok</span>
                  <span className="kpi-icon">🔥</span>
                </div>
                <div className="kpi-value kpi-value-highlight">
                  {summary.risks.highAndCriticalCount}
                </div>
                <div className="kpi-breakdown">
                  <span className="kpi-tag kpi-tag-danger">
                    Kritikus: {summary.risks.criticalCount}
                  </span>
                  <span className="kpi-tag kpi-tag-warning">
                    Magas: {summary.risks.highCount}
                  </span>
                </div>
              </div>

              {/* KPI 4: Nyitott intézkedések száma */}
              <div
                className="kpi-card kpi-card-clickable"
                onClick={() => onNavigate('mitigations')}
                role="button"
                tabIndex={0}
                onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && onNavigate('mitigations')}
              >
                <div className="kpi-header">
                  <span className="kpi-label">Nyitott intézkedések</span>
                  <span className="kpi-icon">🔧</span>
                </div>
                <div className="kpi-value">{summary.mitigations.openActionsCount}</div>
                <div className="kpi-breakdown">
                  <span className="kpi-tag kpi-tag-info">
                    Tervezett: {summary.mitigations.plannedCount}
                  </span>
                  <span className="kpi-tag kpi-tag-warning">
                    Folyamatban: {summary.mitigations.inProgressCount}
                  </span>
                </div>
              </div>

              {/* KPI 5: Lejárt határidejű intézkedések száma */}
              <div
                className={`kpi-card kpi-card-clickable ${
                  summary.mitigations.overdueCount > 0 ? 'kpi-card-danger' : ''
                }`}
                onClick={() => onNavigate('mitigations')}
                role="button"
                tabIndex={0}
                onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && onNavigate('mitigations')}
              >
                <div className="kpi-header">
                  <span className="kpi-label">Lejárt határidejű intézkedések</span>
                  <span className="kpi-icon">⏰</span>
                </div>
                <div className="kpi-value kpi-value-danger">
                  {summary.mitigations.overdueCount}
                </div>
                <div className="kpi-breakdown">
                  <span className="kpi-tag kpi-tag-success">
                    Befejezett: {summary.mitigations.completedCount}
                  </span>
                  <span className="kpi-tag kpi-tag-muted">
                    Határidő nélküli: {summary.mitigations.noDueDateCount}
                  </span>
                </div>
              </div>
            </div>
          </section>

          {/* ── 3. Kockázati és Intézkedési megoszlási diagramok (2 db) ── */}
          <section className="dashboard-charts-grid" aria-label="Megoszlási diagramok">
            <DistributionChartCard
              title="Kockázatok megoszlása kockázati szint szerint"
              subtitle="Az 5×5-ös kockázati mátrix (Valószínűség × Hatás) besorolása alapján"
              totalLabel="kockázat"
              totalCount={summary.risks.totalCount}
              segments={riskSegments}
              footerNote={`Státusz szerint: ${summary.risks.openCount} nyitott, ${summary.risks.underReviewCount} felülvizsgálat alatt, ${summary.risks.mitigatedCount} csökkentett/kezelt, ${summary.risks.closedCount} lezárt.`}
            />

            <DistributionChartCard
              title="Intézkedések megoszlása végrehajtási állapot szerint"
              subtitle="A kockázatcsökkentő intézkedések életciklus-állapotai"
              totalLabel="intézkedés"
              totalCount={summary.mitigations.totalCount}
              segments={mitigationSegments}
              footerNote={`Határidők: ${summary.mitigations.overdueCount} lejárt határidejű (aktív), ${summary.mitigations.noDueDateCount} határidő nélküli.`}
            />
          </section>

          {/* ── 4. Figyelmet igénylő tételek (Top 5 listák) ── */}
          <section className="attention-section">
            {/* Lista #1: Legmagasabb pontszámú nyitott vagy felülvizsgálat alatt álló kockázatok */}
            <div className="attention-card">
              <div className="dashboard-card-header">
                <div>
                  <h3 className="dashboard-card-title">
                    ⚠️ Legmagasabb pontszámú nyitott kockázatok (Top 5)
                  </h3>
                  <p className="dashboard-card-subtitle">
                    Nyitott és felülvizsgálat alatt álló kockázatok csökkenő kockázati pontszám szerint
                  </p>
                </div>
                <button
                  type="button"
                  className="btn btn-sm btn-secondary"
                  onClick={() => onNavigate('risks')}
                >
                  Összes kockázat →
                </button>
              </div>

              {summary.topOpenRisks.length === 0 ? (
                <div className="attention-empty">
                  ✅ Jelenleg nincs nyitott vagy felülvizsgálat alatt álló kockázat a rendszerben.
                </div>
              ) : (
                <div className="attention-list">
                  {summary.topOpenRisks.map(risk => (
                    <div key={risk.id} className="attention-item">
                      <div className="attention-item-main">
                        <div className="attention-item-title-row">
                          <span className="attention-item-title">{risk.title}</span>
                          <RiskLevelBadge level={risk.level} score={risk.score} />
                          <RiskStatusBadge status={risk.status} />
                        </div>
                        <div className="attention-item-meta">
                          <span>🏭 <strong>{risk.supplierName}</strong></span>
                          <span>•</span>
                          <span>📂 {risk.category}</span>
                          <span>•</span>
                          <span>Mátrix: <strong>{risk.probability} × {risk.impact} = {risk.score} pont</strong></span>
                        </div>
                      </div>
                      <div className="attention-item-side">
                        <button
                          type="button"
                          className="btn btn-sm btn-secondary"
                          onClick={() => onOpenRiskMitigations(risk.id)}
                          title="A kockázathoz tartozó intézkedések megnyitása"
                        >
                          🔧 Intézkedések →
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Kétoszlopos sor a Lejárt és a Közelgő határidejű intézkedéseknek */}
            <div className="attention-grid-2col">
              {/* Lista #2: Lejárt határidejű intézkedések (max. 5) */}
              <div className="attention-card">
                <div className="dashboard-card-header">
                  <div>
                    <h3 className="dashboard-card-title">
                      ⏰ Lejárt határidejű intézkedések (Top 5)
                    </h3>
                    <p className="dashboard-card-subtitle">
                      Még nem befejezett intézkedések, amelyek határideje már elmúlt
                    </p>
                  </div>
                  <button
                    type="button"
                    className="btn btn-sm btn-secondary"
                    onClick={() => onNavigate('mitigations')}
                  >
                    Összes intézkedés →
                  </button>
                </div>

                {summary.overdueMitigations.length === 0 ? (
                  <div className="attention-empty">
                    ✅ Nincs lejárt határidejű, befejezetlen intézkedés.
                  </div>
                ) : (
                  <div className="attention-list">
                    {summary.overdueMitigations.map(renderMitigationAttentionRow)}
                  </div>
                )}
              </div>

              {/* Lista #3: Közelgő határidők a következő 7 napon belül (max. 5) */}
              <div className="attention-card">
                <div className="dashboard-card-header">
                  <div>
                    <h3 className="dashboard-card-title">
                      📅 Közelgő határidők a következő 7 napban (Top 5)
                    </h3>
                    <p className="dashboard-card-subtitle">
                      A mai napon vagy a következő 7 napon belül esedékes nyitott intézkedések
                    </p>
                  </div>
                  <button
                    type="button"
                    className="btn btn-sm btn-secondary"
                    onClick={() => onNavigate('mitigations')}
                  >
                    Összes intézkedés →
                  </button>
                </div>

                {summary.upcomingMitigations.length === 0 ? (
                  <div className="attention-empty">
                    ℹ️ Nincs a következő 7 napon belül lejáró nyitott intézkedés.
                  </div>
                ) : (
                  <div className="attention-list">
                    {summary.upcomingMitigations.map(renderMitigationAttentionRow)}
                  </div>
                )}
              </div>
            </div>
          </section>
        </>
      )}

      {/* ── 5. Rendszer állapot (Backend health check) ── */}
      <section className="status-section">
        <div className="dashboard-card-header">
          <h3>Rendszer és adatbázis-kapcsolat állapota</h3>
          {!healthLoading && (
            <button className="retry-btn" onClick={onRetryHealth}>
              🔄 Kapcsolat újraellenőrzése
            </button>
          )}
        </div>
        <StatusBadge loading={healthLoading} error={healthError} health={health} />
      </section>
    </div>
  )
}

