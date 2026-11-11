// services/auditService.ts
// Csak olvasható API-hívások az üzleti auditnapló (/api/audit-logs) lekérdezéséhez.
// Kizárólag Administrator szerepkörrel érhető el.

import { apiFetch } from './apiClient'
import type { AuditLogFilters, AuditLogPage } from '../types/audit'

export async function fetchAuditLogs(filters: AuditLogFilters = {}): Promise<AuditLogPage> {
  const params = new URLSearchParams()

  if (filters.fromUtc) {
    params.set('fromUtc', filters.fromUtc)
  }
  if (filters.toUtc) {
    params.set('toUtc', filters.toUtc)
  }
  if (filters.entityType) {
    params.set('entityType', filters.entityType)
  }
  if (filters.action) {
    params.set('action', filters.action)
  }
  if (filters.actor && filters.actor.trim().length > 0) {
    params.set('actor', filters.actor.trim())
  }
  if (filters.entityId && filters.entityId.trim().length > 0) {
    params.set('entityId', filters.entityId.trim())
  }
  if (filters.page) {
    params.set('page', String(filters.page))
  }
  if (filters.pageSize) {
    params.set('pageSize', String(filters.pageSize))
  }

  const query = params.toString()
  const path = query ? `/api/audit-logs?${query}` : '/api/audit-logs'
  return apiFetch<AuditLogPage>(path)
}

