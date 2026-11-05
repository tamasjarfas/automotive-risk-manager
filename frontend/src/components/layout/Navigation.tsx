// components/layout/Navigation.tsx
// Egyszerű navigációs sáv a főbb oldalak között.
// React Router nélkül: az aktív oldalt prop-ként kapja, átváltáskor callback hívódik.

type Page = 'dashboard' | 'suppliers'

interface Props {
  currentPage: Page
  onNavigate: (page: Page) => void
}

const navItems: { id: Page; label: string; icon: string }[] = [
  { id: 'dashboard',  label: 'Dashboard',    icon: '📊' },
  { id: 'suppliers',  label: 'Beszállítók',  icon: '🏭' },
]

export default function Navigation({ currentPage, onNavigate }: Props) {
  return (
    <nav className="app-nav">
      <div className="nav-content">
        {navItems.map(item => (
          <button
            key={item.id}
            className={`nav-btn ${currentPage === item.id ? 'nav-btn-active' : ''}`}
            onClick={() => onNavigate(item.id)}
            aria-current={currentPage === item.id ? 'page' : undefined}
          >
            <span className="nav-icon">{item.icon}</span>
            <span>{item.label}</span>
          </button>
        ))}
      </div>
    </nav>
  )
}

