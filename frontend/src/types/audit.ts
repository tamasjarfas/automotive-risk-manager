// types/audit.ts
// Az üzleti auditnapló (AuditLog) TypeScript típusai és magyar nyelvű feliratai.

export type AuditAction =
  | 'Created'
  | 'Updated'
  | 'StatusChanged'
  | 'Deactivated'
  | 'Activated'
  | 'RoleChanged'
  | 'Completed'
  | 'Reopened'

export type AuditEntityType =
  | 'Supplier'
  | 'Risk'
  | 'MitigationAction'
  | 'User'

export interface AuditFieldChange {
  field: string
  oldValue: string | null
  newValue: string | null
}

export interface AuditLogItem {
  id: string
  timestampUtc: string
  actorUserId: string | null
  actorUserName: string
  action: AuditAction
  entityType: AuditEntityType
  entityId: string
  summary: string
  changes: string | null
  changeItems: AuditFieldChange[]
}

export interface AuditLogPage {
  items: AuditLogItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface AuditLogFilters {
  fromUtc?: string
  toUtc?: string
  entityType?: string
  action?: string
  actor?: string
  entityId?: string
  page?: number
  pageSize?: number
}

export const AUDIT_ACTION_LABELS: Record<AuditAction, string> = {
  Created:       'Létrehozás',
  Updated:       'Módosítás',
  StatusChanged: 'Státuszváltás',
  Deactivated:   'Inaktiválás',
  Activated:     'Aktiválás',
  RoleChanged:   'Szerepkörváltás',
  Completed:     'Befejezés',
  Reopened:      'Újranyitás'
}

export const AUDIT_ENTITY_TYPE_LABELS: Record<AuditEntityType, string> = {
  Supplier:         'Beszállító',
  Risk:             'Kockázat',
  MitigationAction: 'Intézkedés',
  User:             'Felhasználó'
}

export const AUDIT_FIELD_LABELS: Record<string, string> = {
  Name:              'Megnevezés',
  Activity:          'Tevékenység',
  Description:       'Leírás',
  Status:            'Státusz',
  SupplierId:        'Beszállító azonosító',
  Title:             'Cím / Megnevezés',
  Category:          'Kategória',
  Probability:       'Valószínűség (1–5)',
  Impact:            'Hatás (1–5)',
  Score:             'Kockázati pontszám (1–25)',
  Level:             'Kockázati szint',
  RiskId:            'Kockázat azonosító',
  ResponsiblePerson: 'Felelős',
  DueDate:           'Határidő',
  Priority:          'Prioritás',
  CompletedAt:       'Befejezés időpontja',
  ResultNotes:       'Eredmény / Megjegyzés',
  UserName:          'Felhasználónév',
  DisplayName:       'Megjelenített név',
  Role:              'Szerepkör',
  IsActive:          'Aktív fiók'
}

