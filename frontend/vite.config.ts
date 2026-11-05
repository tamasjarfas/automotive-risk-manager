import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// vite.config.ts – a Vite fejlesztői szerver konfigurációja.
//
// A proxy beállítás kulcsfontosságú:
// A frontend a /api/ kezdetű kéréseket automatikusan
// továbbítja a backendnek (localhost:5000),
// így elkerülhetők a CORS-problémák fejlesztés közben.

// GitHub Codespaces: a böngésző a továbbított porton, egyedi címen éri el a Vite-ot
// (https://<CODESPACE_NAME>-5173.<továbbítási domain>). A Vite host-ellenőrzése
// ezt alapból elutasítja, ezért PONTOSAN ezt az egy címet engedélyezzük.
// A két környezeti változót a Codespaces automatikusan beállítja; helyi (Windows)
// futtatásnál nincsenek definiálva, ilyenkor semmilyen extra host nem engedélyezett.
const env =
  (globalThis as unknown as { process?: { env?: Record<string, string | undefined> } }).process?.env ?? {}
const codespaceName = env.CODESPACE_NAME
const forwardingDomain = env.GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN
const codespacesHosts =
  codespaceName && forwardingDomain ? [`${codespaceName}-5173.${forwardingDomain}`] : []

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,  // A frontend ezen a porton fut
    allowedHosts: codespacesHosts,  // csak a saját Codespaces-cím (lásd fent)
    proxy: {
      // Minden /api/ kezdetű kérés a backendre megy
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

