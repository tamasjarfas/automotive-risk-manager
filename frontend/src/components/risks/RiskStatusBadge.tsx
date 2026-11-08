// components/risks/RiskStatusBadge.tsx
// Jelvény a kockázat életciklus-státuszának (Open, UnderReview, Mitigated, Closed)
// megjelenítésére.

import type { RiskStatus } from '../../types/risk'
import { RISK_STATUS_LABELS } from '../../types/risk'

interface Props {
  status: RiskStatus
}

export default function RiskStatusBadge({ status }: Props) {
  const statusClass = `risk-status-${status.toLowerCase()}`
  const label = RISK_STATUS_LABELS[status] ?? status

  return (
    <span className={`risk-status-badge ${statusClass}`}>
      {label}
    </span>
  )
}

