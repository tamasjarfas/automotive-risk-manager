// components/mitigations/MitigationPriorityBadge.tsx
// Vizuális jelvény a kockázatcsökkentő intézkedés prioritásának
// (Low, Medium, High, Critical) megjelenítésére.

import type { MitigationPriority } from '../../types/mitigation'
import { MITIGATION_PRIORITY_LABELS } from '../../types/mitigation'

interface Props {
  priority: MitigationPriority
}

export default function MitigationPriorityBadge({ priority }: Props) {
  const priorityClass = `risk-level-${priority.toLowerCase()}`
  const label = MITIGATION_PRIORITY_LABELS[priority] ?? priority

  return (
    <span className={`risk-level-badge ${priorityClass}`}>
      <span className="risk-level-dot" />
      <span>{label}</span>
    </span>
  )
}

