// main.tsx – a React alkalmazás belépési pontja.
// Ez a fájl mountolja a gyökér React komponenst a HTML-be.

import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import './index.css'

// Az 'root' id-jű div-et az index.html tartalmazza
const rootElement = document.getElementById('root')
if (!rootElement) {
  throw new Error('Nem található a #root elem az index.html-ben!')
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

