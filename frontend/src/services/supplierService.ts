// services/supplierService.ts
// A backend /api/suppliers végpontjait hívó szolgáltatás.
//
// Minden API-hívás egy helyen koncentrálódik:
//   - Ha a backend URL megváltozik, csak itt kell módosítani.
//   - Az összes fetch-hívás egységes hibakezelést kap.
//   - A visszatérési típusok TypeScript-interfészekhez kötöttek.

import type {
  Supplier,
  CreateSupplierRequest,
  UpdateSupplierRequest,
  UpdateSupplierStatusRequest
} from '../types/supplier'

const BASE = '/api/suppliers'

// Egységes hibakezelő: ha a válasz nem OK, kiolvassa a hibaüzenetet és dob egy Error-t
async function handleResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.json() as Promise<T>
  }

  // Próbáljuk kiolvasni a backend hibaüzenetét
  let errorMessage = `HTTP ${response.status} ${response.statusText}`
  try {
    const body = await response.json()
    if (body?.message) errorMessage = body.message
    else if (body?.title) errorMessage = body.title
  } catch {
    // Ha a válasz nem JSON, maradjon az alapértelmezett üzenet
  }

  throw new Error(errorMessage)
}

// GET /api/suppliers – összes beszállító lekérése
export async function fetchSuppliers(): Promise<Supplier[]> {
  const response = await fetch(BASE)
  return handleResponse<Supplier[]>(response)
}

// GET /api/suppliers/{id} – egy adott beszállító lekérése
export async function fetchSupplierById(id: string): Promise<Supplier> {
  const response = await fetch(`${BASE}/${id}`)
  return handleResponse<Supplier>(response)
}

// POST /api/suppliers – új beszállító létrehozása
export async function createSupplier(data: CreateSupplierRequest): Promise<Supplier> {
  const response = await fetch(BASE, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  })
  return handleResponse<Supplier>(response)
}

// PUT /api/suppliers/{id} – meglévő beszállító módosítása
export async function updateSupplier(id: string, data: UpdateSupplierRequest): Promise<Supplier> {
  const response = await fetch(`${BASE}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  })
  return handleResponse<Supplier>(response)
}

// PATCH /api/suppliers/{id}/status – állapot megváltoztatása
export async function updateSupplierStatus(
  id: string,
  data: UpdateSupplierStatusRequest
): Promise<Supplier> {
  const response = await fetch(`${BASE}/${id}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  })
  return handleResponse<Supplier>(response)
}

