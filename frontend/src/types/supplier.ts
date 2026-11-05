// types/supplier.ts
// A Supplier entitás TypeScript interfészei.
//
// Ezek a típusok pontosan tükrözik a backend SupplierDto C# osztályát.
// Ha a backend API-kontraktus változik, itt kell frissíteni – és a TypeScript
// fordító azonnal jelzi, hol kell javítani a frontend kódban.

// A backend által visszaadott állapotértékek
export type SupplierStatus = 'Active' | 'Inactive'

// A backend SupplierDto.cs osztályának TypeScript megfelelője
export interface Supplier {
  id: string                        // Guid (UUID v4), string-ként érkezik JSON-ból
  name: string
  activity: string
  description: string | null
  status: SupplierStatus
  createdAt: string                 // ISO 8601 formátumú dátum-string
  updatedAt: string | null
}

// POST /api/suppliers kérés törzse
export interface CreateSupplierRequest {
  name: string
  activity: string
  description?: string
}

// PUT /api/suppliers/{id} kérés törzse
export interface UpdateSupplierRequest {
  name: string
  activity: string
  description?: string
}

// PATCH /api/suppliers/{id}/status kérés törzse
export interface UpdateSupplierStatusRequest {
  status: SupplierStatus
}

// Az API hibaválaszának általános alakja
export interface ApiError {
  message?: string
  errors?: Record<string, string[]>
}

