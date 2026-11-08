// main.tsx – a React alkalmazás belépési pontja.
// Ez a fájl mountolja a gyökér React komponenst a HTML-be.
// Az AuthProvider az egész alkalmazás számára elérhetővé teszi a bejelentkezett felhasználót.

import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import { AuthProvider } from './context/AuthContext'
import './index.css'

// Az 'root' id-jű div-et az index.html tartalmazza
const rootElement = document.getElementById('root')
if (!rootElement) {
  throw new Error('Nem található a #root elem az index.html-ben!')
}

createRoot(rootElement).render(
  <StrictMode>
    <AuthProvider>
      <App />
    </AuthProvider>
  </StrictMode>,
)
