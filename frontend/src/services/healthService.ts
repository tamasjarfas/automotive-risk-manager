// healthService.ts – a backend /api/health végpontot hívó szolgáltatás.
//
// Ez a fájl tartalmazza a backend API-val való kommunikációt.
// Miért kell ez?
//   - Az API-hívások egy helyen vannak, nem szétszórva a komponensek között.
//   - Ha a backend URL megváltozik, csak itt kell módosítani.
//   - TypeScript interfészekkel biztonságosan típusos az adat.

// A backend által visszaadott adat típusa
export interface HealthResponse {
  status: string
  application: string
  version: string
  timestamp: string
  environment: string
  message: string
}

// A health végpont lekérdezése
// A Vite proxy miatt /api/health → http://localhost:5000/api/health
export async function fetchHealthStatus(): Promise<HealthResponse> {
  const response = await fetch('/api/health')

  if (!response.ok) {
    throw new Error(`API hiba: ${response.status} ${response.statusText}`)
  }

  const data: HealthResponse = await response.json()
  return data
}

