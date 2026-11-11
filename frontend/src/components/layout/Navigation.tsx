// components/layout/Navigation.tsx
// Navigációs sáv. A menüpontok a felhasználó szerepköre szerint jelennek meg.
// FIGYELEM: ez csak kényelmi funkció; a jogosultságot a szerver ellenőrzi.
// React Router nélkül: az aktív oldalt prop-ként kapja, átváltáskor callback hívódik.

import { useAuth } from '../../context/AuthContext'

export type Page = 'dashboard' | 'suppliers' | 'risks' | 'mitigations' | 'users' | 'audit'

interface Props {
  currentPage: Page
  onNavigate: (page: Page) => void
}

interface NavItem {
  id: Page
  label: string
  icon: string
  adminOnly?: boolean
}

const navItems: NavItem[] = [
  { id: 'dashboard',   label: 'Dashboard',    icon: '📊' },
  { id: 'suppliers',   label: 'Beszállítók',  icon: '🏭' },
  { id: 'risks',       label: 'Kockázatok',   icon: '⚠️' },
  { id: 'mitigations', label: 'Intézkedések', icon: '🔧' },
  { id: 'users',       label: 'Felhasználók', icon: '👤', adminOnly: true },
  { id: 'audit',       label: 'Auditnapló',   icon: '📋', adminOnly: true },
]

export default function Navigation({ currentPage, onNavigate }: Props) {
  const { canManageUsers } = useAuth()
  const visibleItems = navItems.filter(item => !item.adminOnly || canManageUsers)

  return (
    <nav className="app-nav">
      <div className="nav-content">
        {visibleItems.map(item => (
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
