// types/dashboard.ts
// TypeScript típusdefiníciók a vezetői Dashboard (/api/dashboard/summary) válaszához.

import type { RiskLevel, RiskStatus } from './risk'
import type { MitigationPriority, MitigationStatus } from './mitigation'

export interface DashboardSuppliersSummary {
  totalCount: number
  activeCount: number
  inactiveCount: number
}

export interface DashboardRisksSummary {
  totalCount: number
  lowCount: number
  mediumCount: number
  highCount: number
  criticalCount: number
  highAndCriticalCount: number
  openCount: number
  underReviewCount: number
  mitigatedCount: number
  closedCount: number
}

export interface DashboardMitigationsSummary {
  totalCount: number
  plannedCount: number
  inProgressCount: number
  completedCount: number
  cancelledCount: number
  openActionsCount: number
  overdueCount: number
  noDueDateCount: number
}

export interface DashboardTopRiskItem {
  id: string
  title: string
  category: string
  supplierId: string
  supplierName: string
  probability: number
  impact: number
  score: number
  level: RiskLevel
  status: RiskStatus
  createdAt: string
}

export interface DashboardMitigationItem {
  id: string
  title: string
  responsiblePerson: string
  riskId: string
  riskTitle: string
  riskScore: number
  riskLevel: RiskLevel
  supplierId: string
  supplierName: string
  dueDate: string | null
  status: MitigationStatus
  priority: MitigationPriority
  isOverdue: boolean
}

export interface DashboardSummary {
  generatedAtUtc: string
  suppliers: DashboardSuppliersSummary
  risks: DashboardRisksSummary
  mitigations: DashboardMitigationsSummary
  topOpenRisks: DashboardTopRiskItem[]
  overdueMitigations: DashboardMitigationItem[]
  upcomingMitigations: DashboardMitigationItem[]
}

