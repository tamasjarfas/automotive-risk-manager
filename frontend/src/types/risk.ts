// types/risk.ts
// TypeScript típusdefiníciók és segédkonstansok a kockázatértékelési modulhoz.
//
// FONTOS: a previewRiskEvaluation() függvény kizárólag a felhasználói felületen
// (RiskForm) nyújt azonnali vizuális előnézetet az űrlap kitöltése közben.
// A tényleges és irányadó kockázati pontszámot (Score) és szintet (Level)
// mindig a backend (RiskCalculator.cs) számítja ki és menti el.

export type RiskLevel = 'Low' | 'Medium' | 'High' | 'Critical'

export type RiskStatus = 'Open' | 'UnderReview' | 'Mitigated' | 'Closed'

export interface Risk {
  id: string
  supplierId: string
  supplierName: string
  title: string
  description: string | null
  category: string
  probability: number
  impact: number
  score: number
  level: RiskLevel
  status: RiskStatus
  createdAt: string
  updatedAt: string | null
}

export interface CreateRiskRequest {
  supplierId: string
  title: string
  description?: string
  category: string
  probability: number
  impact: number
}

export interface UpdateRiskRequest {
  supplierId: string
  title: string
  description?: string
  category: string
  probability: number
  impact: number
  status?: RiskStatus
}

export interface UpdateRiskStatusRequest {
  status: RiskStatus
}

export interface RiskFilters {
  supplierId?: string
  level?: RiskLevel | ''
  status?: RiskStatus | ''
}

export const RISK_LEVEL_LABELS: Record<RiskLevel, string> = {
  Low:      'Alacsony (1–4)',
  Medium:   'Közepes (5–9)',
  High:     'Magas (10–16)',
  Critical: 'Kritikus (17–25)',
}

export const RISK_LEVEL_SHORT_LABELS: Record<RiskLevel, string> = {
  Low:      'Alacsony',
  Medium:   'Közepes',
  High:     'Magas',
  Critical: 'Kritikus',
}

export const RISK_STATUS_LABELS: Record<RiskStatus, string> = {
  Open:        'Nyitott',
  UnderReview: 'Felülvizsgálat alatt',
  Mitigated:   'Csökkentett / Kezelt',
  Closed:      'Lezárt / Archivált',
}

export const RISK_CATEGORIES: readonly string[] = [
  'Adatvédelem',
  'Hozzáférés-kezelés',
  'Üzletmenet-folytonosság',
  'Hálózatbiztonság',
  'Beszállítói megfelelőség',
  'Fizikai biztonság',
  'Incidenskezelés',
  'Szoftver- és rendszerbiztonság',
]

export const PROBABILITY_SCALE: ReadonlyArray<{ value: number; label: string }> = [
  { value: 1, label: '1 – Nagyon ritka (évente < 1 alkalom)' },
  { value: 2, label: '2 – Ritka (eseti előfordulás)' },
  { value: 3, label: '3 – Lehetséges (időszakos kockázat)' },
  { value: 4, label: '4 – Valószínű (gyakori fenyegetés)' },
  { value: 5, label: '5 – Szinte biztos (folyamatos kitettség)' },
]

export const IMPACT_SCALE: ReadonlyArray<{ value: number; label: string }> = [
  { value: 1, label: '1 – Elhanyagolható (nincs érdemi fennakadás)' },
  { value: 2, label: '2 – Kisebb (helyi, gyorsan elhárítható zavar)' },
  { value: 3, label: '3 – Mérsékelt (észrevehető üzleti vagy IT-kár)' },
  { value: 4, label: '4 – Súlyos (jelentős adatvesztés / gyártásleállás)' },
  { value: 5, label: '5 – Kritikus (ellátásilánc-leállás / jogi/ISMS-sértés)' },
]

/**
 * Kliensoldali előnézeti számítás az űrlaphoz (5×5-ös kockázati mátrix).
 * A szerver a mentéskor ettől függetlenül, saját maga számítja ki az értéket.
 */
export function previewRiskEvaluation(
  probability: number,
  impact: number
): { score: number; level: RiskLevel } {
  const p = Math.min(5, Math.max(1, Math.round(probability)))
  const i = Math.min(5, Math.max(1, Math.round(impact)))
  const score = p * i

  let level: RiskLevel
  if (score <= 4) level = 'Low'
  else if (score <= 9) level = 'Medium'
  else if (score <= 16) level = 'High'
  else level = 'Critical'

  return { score, level }
}

