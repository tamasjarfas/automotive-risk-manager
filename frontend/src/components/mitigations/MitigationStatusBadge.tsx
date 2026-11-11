// components/mitigations/MitigationStatusBadge.tsx
// Jelvény a kockázatcsökkentő intézkedés végrehajtási állapotának
// (Planned, InProgress, Completed, Cancelled) megjelenítésére.

import type { MitigationStatus } from '../../types/mitigation'
import { MITIGATION_STATUS_LABELS } from '../../types/mitigation'

interface Props {
  status: MitigationStatus
}

export default function MitigationStatusBadge({ status }: Props) {
  const statusClass = `mitigation-status-${status.toLowerCase()}`
  const label = MITIGATION_STATUS_LABELS[status] ?? status

  return (
    <span className={`mitigation-status-badge ${statusClass}`}>
      {label}
    </span>
  )
}

