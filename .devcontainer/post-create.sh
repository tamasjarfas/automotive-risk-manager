#!/usr/bin/env bash
# .devcontainer/post-create.sh
# A Codespace első létrehozásakor fut le (postCreateCommand).
#
# Mit csinál?
#   1. Ellenőrzi az ARM_DB_PASSWORD Codespaces-titkot.
#   2. Visszaállítja a .NET eszközöket (dotnet-ef) és a NuGet-csomagokat.
#   3. Telepíti a frontend függőségeit (npm ci).
#   4. A kapcsolati sztringet a .NET User Secrets-be írja (a repón kívülre).
#   5. Opcionálisan telepíti a psql klienst az adatbázis kézi ellenőrzéséhez.
#
# A fájl NEM tartalmaz jelszót. A parancs többször is futtatható.

set -euo pipefail

# A repó gyökerébe lépünk (a script a .devcontainer mappában van)
cd "$(dirname "$0")/.."

BACKEND_PROJECT="backend/AutomotiveRiskManager.Api"

# 1) Titok ellenőrzése
if [[ -z "${ARM_DB_PASSWORD:-}" ]]; then
  echo "HIBA: Az ARM_DB_PASSWORD nincs beállítva." >&2
  echo "Állítsd be: GitHub -> Settings -> Codespaces -> Secrets, majd indítsd újra a Codespace-t." >&2
  exit 1
fi

# A jelszó csak biztonságos karaktereket tartalmazhat, így a kapcsolati sztring
# nem törhet el (nincs ; = " karakter), és a shell sem értelmezi félre.
if [[ ! "${ARM_DB_PASSWORD}" =~ ^[A-Za-z0-9._~-]{12,}$ ]]; then
  echo "HIBA: Az ARM_DB_PASSWORD legalább 12 karakter legyen, és csak betűt, számot, '.', '_', '~', '-' tartalmazhat." >&2
  exit 1
fi

# 2) .NET eszközök és csomagok
echo ">> dotnet tool restore"
dotnet tool restore

echo ">> dotnet restore"
dotnet restore "${BACKEND_PROJECT}"

# 3) Frontend függőségek (a package-lock.json alapján, reprodukálható telepítés)
echo ">> npm ci"
(cd frontend && npm ci)

# 4) Kapcsolati sztring a User Secrets-be (a repón kívül tárolódik: ~/.microsoft/usersecrets)
echo ">> User Secrets beállítása"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=127.0.0.1;Port=5432;Database=automotive_risk_manager;Username=arm_app;Password=${ARM_DB_PASSWORD}" \
  --project "${BACKEND_PROJECT}" > /dev/null
echo "   Kész (az érték nincs kiírva)."

# 5) psql kliens (nem kötelező; hiba esetén a környezet attól még működik)
if ! command -v psql > /dev/null 2>&1; then
  echo ">> psql kliens telepítése (opcionális)"
  (sudo apt-get update -y > /dev/null && sudo apt-get install -y postgresql-client > /dev/null) \
    || echo "   A psql telepítése nem sikerült, kihagyva (nem kritikus)."
fi

echo "Kész. Következő lépés: lásd a README / útmutató (migráció, dotnet run, npm run dev)."

