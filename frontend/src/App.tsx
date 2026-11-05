// App.tsx – a főkomponens, az alkalmazás „gyökere".
//
// Felelősségei:
//   1. Navigációs állapot kezelése (melyik oldal van megnyitva)
//   2. Fejléc és lábléc megjelenítése
//   3. Az aktív oldal komponensének renderelése

import { useState, useEffect } from 'react'
import { fetchHealthStatus, HealthResponse } from './services/healthService'
import StatusBadge from './components/StatusBadge'
import Navigation from './components/layout/Navigation'
import SupplierList from './components/suppliers/SupplierList'

type Page = 'dashboard' | 'suppliers'

export default function App() {
  const [currentPage, setCurrentPage] = useState<Page>('dashboard')

  // Backend kapcsolat állapota (dashboard-on jelenik meg)
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
          <div className="header-badge">v0.2.0 – Fejlesztői verzió</div>
        </div>
      </header>

      {/* ── Navigáció ── */}
      <Navigation currentPage={currentPage} onNavigate={setCurrentPage} />

      {/* ── Fő tartalom ── */}
      <main className="app-main">

        {/* Dashboard oldal */}
        {currentPage === 'dashboard' && (
          <>
            <section className="hero">
              <h2>Üdvözöljük a rendszerben</h2>
              <p>
                Ez az alkalmazás egy fiktív autóipari alkatrészgyártó vállalat
                beszállítói információbiztonsági kockázatkezelését támogatja.
                A rendszer BSc szakdolgozat keretében, oktatási célra készül.
              </p>
            </section>

            <section className="status-section">
              <h3>Rendszer állapot</h3>
              <StatusBadge loading={loading} error={error} health={health} />
              {!loading && (
                <button
                  className="retry-btn"
                  onClick={() => {
                    setLoading(true)
                    setHealth(null)
                    setError(null)
                    fetchHealthStatus()
                      .then(setHealth)
                      .catch(err => setError(err instanceof Error ? err.message : 'Hiba'))
                      .finally(() => setLoading(false))
                  }}
                >
                  🔄 Újraellenőrzés
                </button>
              )}
            </section>

            <section className="features">
              <h3>Modulok állapota</h3>
              <div className="features-grid">
                {[
                  { icon: '👤', title: 'Felhasználókezelés',    desc: 'Bejelentkezés, szerepkörök',              status: 'Tervezett' },
                  { icon: '🏭', title: 'Beszállítókezelés',     desc: 'Beszállítók nyilvántartása',              status: 'Elkészült ✅' },
                  { icon: '⚠️', title: 'Kockázatkezelés',      desc: 'Kockázatok rögzítése és felülvizsgálata', status: 'Tervezett' },
                  { icon: '📊', title: 'Kockázatértékelés',     desc: 'Valószínűség × Hatás pontszám',           status: 'Tervezett' },
                  { icon: '🔧', title: 'Kezelési intézkedések', desc: 'Feladatok és felelősök',                  status: 'Tervezett' },
                  { icon: '📈', title: 'Dashboard',             desc: 'Összesített mutatók, auditnapló',         status: 'Tervezett' },
                ].map(feature => (
                  <div key={feature.title} className="feature-card">
                    <div className="feature-icon">{feature.icon}</div>
                    <h4>{feature.title}</h4>
                    <p>{feature.desc}</p>
                    <span className={`feature-status ${feature.status.includes('✅') ? 'status-done' : ''}`}>
                      {feature.status}
                    </span>
                  </div>
                ))}
              </div>
            </section>
          </>
        )}

        {/* Beszállítók oldal */}
        {currentPage === 'suppliers' && <SupplierList />}

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

