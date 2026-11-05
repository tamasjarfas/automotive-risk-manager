// components/suppliers/SupplierStatusBadge.tsx
// Kis vizuális jelzőelem a beszállító állapotához.

import type { SupplierStatus } from '../../types/supplier'

interface Props {
  status: SupplierStatus
}

export default function SupplierStatusBadge({ status }: Props) {
  const isActive = status === 'Active'

  return (
    <span className={`supplier-status-badge ${isActive ? 'badge-active' : 'badge-inactive'}`}>
      <span className={`badge-dot ${isActive ? 'dot-active' : 'dot-inactive'}`}></span>
      {isActive ? 'Aktív' : 'Inaktív'}
    </span>
  )
}

