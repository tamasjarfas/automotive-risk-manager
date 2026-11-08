// services/apiClient.ts
// Közös API-kliens a backend hívásaihoz.
//
// Miért kell ez?
//   1. CSRF-védelem: az állapotmódosító kérések (POST/PUT/PATCH/DELETE) elé automatikusan
//      beteszi az X-CSRF-TOKEN fejlécet (a tokent a /api/auth/csrf végpontról kéri le).
//   2. Cookie-alapú munkamenet: a böngésző küldi a HTTP-only cookie-t; a JavaScript azt
//      nem látja. (credentials: 'same-origin' – a Vite proxy miatt azonos origin.)
//   3. Egységes hibakezelés: ApiError (státuszkóddal és magyar üzenettel).
//   4. Ha a szerver 401-et ad egy bejelentkezett felhasználónak (lejárt munkamenet),
//      értesíti az AuthContext-et, ami a bejelentkezési oldalra vált.

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE'])

let csrfToken: string | null = null
let csrfPromise: Promise<string> | null = null
let unauthorizedHandler: (() => void) | null = null

/** Az AuthContext ezzel iratkozik fel a "lejárt munkamenet" eseményre. */
export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler
}

/** Elfelejti a tárolt CSRF-tokent (bejelentkezés/kijelentkezés után új kell). */
export function clearCsrfToken(): void {
  csrfToken = null
}

/** Új CSRF-token lekérése. Egyidejű hívásoknál ugyanazt a kérést használja újra. */
export function refreshCsrfToken(): Promise<string> {
  const request = (async () => {
    const response = await fetch('/api/auth/csrf', {
      credentials: 'same-origin',
      cache: 'no-store'
    })
    if (!response.ok) {
      throw new ApiError(response.status, 'Nem sikerült biztonsági tokent lekérni.')
    }
    const body = (await response.json()) as { token: string }
    csrfToken = body.token
    return body.token
  })()

  csrfPromise = request
  request
    .finally(() => {
      if (csrfPromise === request) csrfPromise = null
    })
    .catch(() => undefined)

  return request
}

async function getCsrfToken(): Promise<string> {
  if (csrfToken) return csrfToken
  return csrfPromise ?? refreshCsrfToken()
}

function defaultMessage(status: number): string {
  switch (status) {
    case 400: return 'Érvénytelen kérés.'
    case 401: return 'A munkamenet lejárt, vagy nincs bejelentkezve.'
    case 403: return 'Nincs jogosultságod a művelet elvégzéséhez.'
    case 404: return 'A keresett elem nem található.'
    default:  return `Szerverhiba (HTTP ${status}).`
  }
}

// A backend többféle hibaformátumot használhat: { message }, { message, errors: [...] },
// vagy ASP.NET ProblemDetails ({ title, errors: { mező: [...] } }). Mindet kezeljük.
async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body: unknown = await response.json()
    if (body && typeof body === 'object') {
      const b = body as { message?: unknown; title?: unknown; errors?: unknown }
      const details: string[] = []

      if (Array.isArray(b.errors)) {
        for (const e of b.errors) if (typeof e === 'string') details.push(e)
      } else if (b.errors && typeof b.errors === 'object') {
        for (const value of Object.values(b.errors as Record<string, unknown>)) {
          if (Array.isArray(value)) {
            for (const e of value) if (typeof e === 'string') details.push(e)
          }
        }
      }

      if (typeof b.message === 'string') {
        return details.length > 0 ? `${b.message} ${details.join(' ')}` : b.message
      }
      if (details.length > 0) return details.join(' ')
      if (typeof b.title === 'string') return b.title
    }
  } catch {
    // A válasz nem JSON (pl. üres 403 vagy 400): az alapértelmezett üzenetet használjuk.
  }
  return defaultMessage(response.status)
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = (init.method ?? 'GET').toUpperCase()
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  if (init.body !== undefined && init.body !== null && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  if (UNSAFE_METHODS.has(method)) {
    headers.set('X-CSRF-TOKEN', await getCsrfToken())
  }

  const response = await fetch(path, {
    ...init,
    method,
    headers,
    credentials: 'same-origin'
  })

  if (response.ok) {
    if (response.status === 204) return undefined as T
    return (await response.json()) as T
  }

  const message = await readErrorMessage(response)

  // Lejárt munkamenet: a bejelentkezési hívás saját 401-ét (rossz jelszó) nem ide soroljuk.
  if (response.status === 401 && unauthorizedHandler && path !== '/api/auth/login') {
    unauthorizedHandler()
  }
  // Sikertelen állapotmódosítás után a következő próbálkozás friss CSRF-tokent kér.
  if (UNSAFE_METHODS.has(method) && response.status === 400) {
    clearCsrfToken()
  }

  throw new ApiError(response.status, message)
}

