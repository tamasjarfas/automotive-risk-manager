// App.tsx – a főkomponens, az alkalmazás „gyökere".
//
// Felelősségei:
//   1. Hitelesítési állapot: nincs bejelentkezés → bejelentkezési oldal
//   2. Navigációs állapot kezelése (melyik oldal van megnyitva)
//   3. Fejléc (bejelentkezett felhasználó, kijelentkezés) és lábléc megjelenítése
//   4. Az aktív oldal komponensének renderelése (jogosultságtól függően)

import { useState, useEffect } from 'react'
import { fetchHealthStatus, HealthResponse } from './services/healthService'
import Navigation from './components/layout/Navigation'
import type { Page } from './components/layout/Navigation'
import DashboardPage from './components/dashboard/DashboardPage'
import SupplierList from './components/suppliers/SupplierList'
import RiskList from './components/risks/RiskList'
import MitigationList from './components/mitigations/MitigationList'
import LoginPage from './components/auth/LoginPage'
import AccessDenied from './components/auth/AccessDenied'
import UserManagement from './components/users/UserManagement'
import { useAuth } from './context/AuthContext'
import { ROLE_LABELS } from './types/auth'

export default function App() {
  const { user, loading: authLoading, logout, canManageUsers } = useAuth()
  const [currentPage, setCurrentPage] = useState<Page>('dashboard')
  const [selectedRiskIdForMitigations, setSelectedRiskIdForMitigations] = useState<string | null>(null)

  // Backend kapcsolat állapota (dashboard-on jelenik meg; a /api/health nyilvános)
  const [health, setHealth]   = useState<HealthResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState<string | null>(null)

  useEffect(() => {
    const checkHealth = async () => {
      try {
        setLoading(true)
        setError(null)
        const data = await fetchHealthStatus()
        setHealth(data)
      } catch (err) {
        setError(
          err instanceof Error
            ? err.message
            : 'Ismeretlen hiba történt a backend elérése közben.'
        )
      } finally {
        setLoading(false)
      }
    }
    checkHealth()
  }, [])

  async function handleLogout() {
    await logout()
    setCurrentPage('dashboard')   // újabb bejelentkezésnél ne a korábbi (esetleg tiltott) oldal nyíljon meg
  }

  // A munkamenet ellenőrzése közben (oldalbetöltéskor) rövid várakozó képernyő
  if (authLoading) {
    return (
      <div className="login-page">
        <div className="loading-state">
          <div className="spinner"></div>
          <span>Munkamenet ellenőrzése...</span>
        </div>
      </div>
    )
  }

  // Nincs bejelentkezve: csak a bejelentkezési oldal érhető el
  if (!user) {
    return <LoginPage />
  }

  return (
    <div className="app">
      {/* ── Fejléc ── */}
      <header className="app-header">
        <div className="header-content">
          <div className="logo">
            <div className="logo-icon">🛡️</div>
            <div className="logo-text">
              <h1>Automotive Risk Manager</h1>
              <p>Információbiztonsági kockázatkezelő rendszer</p>
            </div>
          </div>
          <div className="header-right">
            <div className="header-user">
              <span className="header-user-name">{user.displayName}</span>
              <span className="header-user-role">
                {user.roles.map(r => ROLE_LABELS[r] ?? r).join(', ')}
              </span>
            </div>
            <button className="btn btn-sm btn-logout" onClick={handleLogout}>
              Kijelentkezés
            </button>
            <div className="header-badge">v0.6.0 – Fejlesztői verzió</div>
          </div>
        </div>
      </header>

      {/* ── Navigáció ── */}
      <Navigation currentPage={currentPage} onNavigate={setCurrentPage} />

      {/* ── Fő tartalom ── */}
      <main className="app-main">

        {/* Vezetői Dashboard oldal (minden bejelentkezett szerepkör számára, csak olvasási nézet) */}
        {currentPage === 'dashboard' && (
          <DashboardPage
            userDisplayName={user.displayName}
            health={health}
            healthLoading={loading}
            healthError={error}
            onRetryHealth={() => {
              setLoading(true)
              setHealth(null)
              setError(null)
              fetchHealthStatus()
                .then(setHealth)
                .catch(err => setError(err instanceof Error ? err.message : 'Hiba'))
                .finally(() => setLoading(false))
            }}
            onNavigate={setCurrentPage}
            onOpenRiskMitigations={(riskId) => {
              setSelectedRiskIdForMitigations(riskId)
              setCurrentPage('mitigations')
            }}
          />
        )}

        {/* Beszállítók oldal (minden bejelentkezett szerepkör látja; a módosító gombok jogosultságfüggők) */}
        {currentPage === 'suppliers' && <SupplierList />}

        {/* Kockázatok oldal (minden bejelentkezett szerepkör látja; a módosítás csak Administrator/RiskManager) */}
        {currentPage === 'risks' && (
          <RiskList
            onOpenMitigations={(riskId) => {
              setSelectedRiskIdForMitigations(riskId)
              setCurrentPage('mitigations')
            }}
          />
        )}

        {/* Kockázatcsökkentő intézkedések oldal (minden bejelentkezett szerepkör látja; módosítás: Admin/RiskManager) */}
        {currentPage === 'mitigations' && (
          <MitigationList
            initialRiskId={selectedRiskIdForMitigations}
            onClearInitialRisk={() => setSelectedRiskIdForMitigations(null)}
          />
        )}

        {/* Felhasználók oldal: csak Administrator; másoknak hibaoldal (a szerver is 403-at ad) */}
        {currentPage === 'users' && (
          canManageUsers
            ? <UserManagement />
            : <AccessDenied onBack={() => setCurrentPage('dashboard')} />
        )}

      </main>

      {/* ── Lábléc ── */}
      <footer className="app-footer">
        <p>
          Automotive Risk Manager &copy; 2025 &nbsp;|&nbsp;
          BSc szakdolgozat – mérnökinformatikus szak &nbsp;|&nbsp;
          Kizárólag fiktív adatokkal működik
        </p>
      </footer>
    </div>
  )
}
