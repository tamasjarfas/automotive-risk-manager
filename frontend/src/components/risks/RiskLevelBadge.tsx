// components/risks/RiskLevelBadge.tsx
// Vizuális jelvény a kockázati szint (Low, Medium, High, Critical) és
// opcionálisan a pontszám (1–25) jól látható megkülönböztetésére.

import type { RiskLevel } from '../../types/risk'
import { RISK_LEVEL_SHORT_LABELS } from '../../types/risk'

interface Props {
  level: RiskLevel
  score?: number
}

export default function RiskLevelBadge({ level, score }: Props) {
  const levelClass = `risk-level-${level.toLowerCase()}`
  const label = RISK_LEVEL_SHORT_LABELS[level] ?? level

  return (
    <span className={`risk-level-badge ${levelClass}`}>
      <span className="risk-level-dot" />
      <span>{label}</span>
      {typeof score === 'number' && (
        <span className="risk-level-score">({score})</span>
      )}
    </span>
  )
}

