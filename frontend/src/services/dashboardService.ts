// services/dashboardService.ts
// A vezetői Dashboard összesítő végpontját (GET /api/dashboard/summary) hívó szolgáltatás.
// A hívás a közös apiClient-en (apiFetch) keresztül történik.

import { apiFetch } from './apiClient'
import type { DashboardSummary } from '../types/dashboard'

export function fetchDashboardSummary(): Promise<DashboardSummary> {
  return apiFetch<DashboardSummary>('/api/dashboard/summary')
}

