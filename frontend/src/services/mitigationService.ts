// services/mitigationService.ts
// A backend /api/mitigations (és /api/risks/{id}/mitigations) végpontjait hívó szolgáltatás.
//
// Minden hívás a közös apiClient-en (apiFetch) keresztül történik:
//   - HTTP-only munkamenet cookie automatikus továbbítása
//   - Automatikus X-CSRF-TOKEN fejléc minden állapotmódosító kérésnél (POST, PUT, PATCH)
//   - Egységes hibakezelés (401 -> munkamenet lejárt, 403 -> jogosulatlan, 400 -> validáció)

import { apiFetch } from './apiClient'
import type {
  MitigationAction,
  CreateMitigationRequest,
  UpdateMitigationRequest,
  UpdateMitigationStatusRequest,
  MitigationFilters
} from '../types/mitigation'

const BASE = '/api/mitigations'

// GET /api/mitigations – intézkedések listázása opcionális szűrőkkel
export function fetchMitigations(filters?: MitigationFilters): Promise<MitigationAction[]> {
  const params = new URLSearchParams()

  if (filters?.riskId) {
    params.set('riskId', filters.riskId)
  }
  if (filters?.supplierId) {
    params.set('supplierId', filters.supplierId)
  }
  if (filters?.status) {
    params.set('status', filters.status)
  }
  if (filters?.priority) {
    params.set('priority', filters.priority)
  }
  if (filters?.responsible && filters.responsible.trim()) {
    params.set('responsible', filters.responsible.trim())
  }
  if (filters?.dueFilter) {
    params.set('dueFilter', filters.dueFilter)
  }

  const queryString = params.toString()
  const url = queryString ? `${BASE}?${queryString}` : BASE
  return apiFetch<MitigationAction[]>(url)
}

// GET /api/risks/{riskId}/mitigations – egy adott kockázathoz tartozó intézkedések lekérése
export function fetchMitigationsForRisk(riskId: string): Promise<MitigationAction[]> {
  return apiFetch<MitigationAction[]>(`/api/risks/${riskId}/mitigations`)
}

// GET /api/mitigations/{id} – egy adott intézkedés részletes adatainak lekérése
export function fetchMitigationById(id: string): Promise<MitigationAction> {
  return apiFetch<MitigationAction>(`${BASE}/${id}`)
}

// POST /api/mitigations – új intézkedés rögzítése (Administrator, RiskManager)
export function createMitigation(data: CreateMitigationRequest): Promise<MitigationAction> {
  return apiFetch<MitigationAction>(BASE, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// PUT /api/mitigations/{id} – meglévő intézkedés módosítása (Administrator, RiskManager)
export function updateMitigation(
  id: string,
  data: UpdateMitigationRequest
): Promise<MitigationAction> {
  return apiFetch<MitigationAction>(`${BASE}/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

// PATCH /api/mitigations/{id}/status – intézkedés státuszának frissítése (Administrator, RiskManager)
export function updateMitigationStatus(
  id: string,
  data: UpdateMitigationStatusRequest
): Promise<MitigationAction> {
  return apiFetch<MitigationAction>(`${BASE}/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify(data)
  })
}

