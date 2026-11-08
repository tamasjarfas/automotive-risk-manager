// services/supplierService.ts
// A backend /api/suppliers végpontjait hívó szolgáltatás.
//
// A hívások a közös apiClient-en keresztül mennek: cookie-s munkamenet, automatikus
// CSRF-token az állapotmódosító kéréseknél, egységes hibakezelés (401/403/400...).
// A függvények nevei és visszatérési típusai változatlanok.

import { apiFetch } from './apiClient'
import type {
  Supplier,
  CreateSupplierRequest,
  UpdateSupplierRequest,
  UpdateSupplierStatusRequest
} from '../types/supplier'

const BASE = '/api/suppliers'

// GET /api/suppliers – összes beszállító lekérése
export function fetchSuppliers(): Promise<Supplier[]> {
  return apiFetch<Supplier[]>(BASE)
}

// GET /api/suppliers/{id} – egy adott beszállító lekérése
export function fetchSupplierById(id: string): Promise<Supplier> {
  return apiFetch<Supplier>(`${BASE}/${id}`)
}

// POST /api/suppliers – új beszállító létrehozása (Administrator, RiskManager)
export function createSupplier(data: CreateSupplierRequest): Promise<Supplier> {
  return apiFetch<Supplier>(BASE, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// PUT /api/suppliers/{id} – meglévő beszállító módosítása (Administrator, RiskManager)
export function updateSupplier(id: string, data: UpdateSupplierRequest): Promise<Supplier> {
  return apiFetch<Supplier>(`${BASE}/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

// PATCH /api/suppliers/{id}/status – állapot megváltoztatása (Administrator, RiskManager)
export function updateSupplierStatus(
  id: string,
  data: UpdateSupplierStatusRequest
): Promise<Supplier> {
  return apiFetch<Supplier>(`${BASE}/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify(data)
  })
}
