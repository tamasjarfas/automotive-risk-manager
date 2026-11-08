// services/riskService.ts
// A backend /api/risks végpontjait hívó szolgáltatás.
//
// Minden hívás a közös apiClient-en (apiFetch) keresztül történik:
//   - HTTP-only munkamenet cookie automatikus továbbítása
//   - Automatikus X-CSRF-TOKEN fejléc minden állapotmódosító kérésnél (POST, PUT, PATCH)
//   - Egységes hibakezelés (401 -> munkamenet lejárt, 403 -> jogosulatlan, 400 -> validáció)

import { apiFetch } from './apiClient'
import type {
  Risk,
  CreateRiskRequest,
  UpdateRiskRequest,
  UpdateRiskStatusRequest,
  RiskFilters
} from '../types/risk'

const BASE = '/api/risks'

// GET /api/risks – kockázatok listázása opcionális beszállító-, szint- és státuszszűrővel
export function fetchRisks(filters?: RiskFilters): Promise<Risk[]> {
  const params = new URLSearchParams()

  if (filters?.supplierId) {
    params.set('supplierId', filters.supplierId)
  }
  if (filters?.level) {
    params.set('level', filters.level)
  }
  if (filters?.status) {
    params.set('status', filters.status)
  }

  const queryString = params.toString()
  const url = queryString ? `${BASE}?${queryString}` : BASE
  return apiFetch<Risk[]>(url)
}

// GET /api/risks/{id} – egy adott kockázat részletes adatainak lekérése
export function fetchRiskById(id: string): Promise<Risk> {
  return apiFetch<Risk>(`${BASE}/${id}`)
}

// POST /api/risks – új kockázat rögzítése (Administrator, RiskManager)
export function createRisk(data: CreateRiskRequest): Promise<Risk> {
  return apiFetch<Risk>(BASE, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// PUT /api/risks/{id} – meglévő kockázat módosítása és újraértékelése (Administrator, RiskManager)
export function updateRisk(id: string, data: UpdateRiskRequest): Promise<Risk> {
  return apiFetch<Risk>(`${BASE}/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

// PATCH /api/risks/{id}/status – kockázat státuszának megváltoztatása (Administrator, RiskManager)
export function updateRiskStatus(
  id: string,
  data: UpdateRiskStatusRequest
): Promise<Risk> {
  return apiFetch<Risk>(`${BASE}/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify(data)
  })
}

