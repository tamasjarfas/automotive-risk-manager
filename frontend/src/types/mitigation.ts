// types/mitigation.ts
// TypeScript típusdefiníciók és magyar feliratok a kockázatcsökkentő
// intézkedések (MitigationAction) modulhoz.

import type { RiskLevel } from './risk'

export type MitigationStatus = 'Planned' | 'InProgress' | 'Completed' | 'Cancelled'

export type MitigationPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export type MitigationDueFilter = '' | 'overdue' | 'upcoming' | 'noduedate'

export interface MitigationAction {
  id: string
  riskId: string
  riskTitle: string
  riskScore: number
  riskLevel: RiskLevel
  supplierId: string
  supplierName: string
  title: string
  description: string | null
  responsiblePerson: string
  dueDate: string | null
  status: MitigationStatus
  priority: MitigationPriority
  /** Szerver által számított jelző: lejárt-e a határidő (nem írja felül a státuszt) */
  isOverdue: boolean
  completedAt: string | null
  resultNotes: string | null
  createdAt: string
  updatedAt: string | null
}

export interface CreateMitigationRequest {
  riskId: string
  title: string
  description?: string
  responsiblePerson: string
  dueDate?: string | null
  priority: MitigationPriority
  status?: MitigationStatus
  resultNotes?: string
}

export interface UpdateMitigationRequest {
  riskId: string
  title: string
  description?: string
  responsiblePerson: string
  dueDate?: string | null
  priority: MitigationPriority
  status?: MitigationStatus
  resultNotes?: string
}

export interface UpdateMitigationStatusRequest {
  status: MitigationStatus
  resultNotes?: string
}

export interface MitigationFilters {
  riskId?: string
  supplierId?: string
  status?: MitigationStatus | ''
  priority?: MitigationPriority | ''
  responsible?: string
  dueFilter?: MitigationDueFilter
}

export const MITIGATION_STATUS_LABELS: Record<MitigationStatus, string> = {
  Planned:    'Tervezett',
  InProgress: 'Folyamatban',
  Completed:  'Befejezett',
  Cancelled:  'Visszavont',
}

export const MITIGATION_PRIORITY_LABELS: Record<MitigationPriority, string> = {
  Low:      'Alacsony',
  Medium:   'Közepes',
  High:     'Magas',
  Critical: 'Kritikus',
}

export const MITIGATION_DUE_FILTER_LABELS: Record<Exclude<MitigationDueFilter, ''>, string> = {
  overdue:   '⏰ Lejárt határidejű (aktív)',
  upcoming:  '📅 Határidős (nem lejárt)',
  noduedate: '➖ Határidő nélküli',
}

