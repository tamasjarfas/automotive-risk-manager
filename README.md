# 🛡️ Automotive Risk Manager

**Szakdolgozat címe:** Információbiztonsági kockázatkezelő rendszer tervezése és fejlesztése autóipari beszállítói folyamatok támogatására

> ⚠️ Ez az alkalmazás kizárólag oktatási és demonstrációs célt szolgál, és kizárólag fiktív adatokkal működik. Nem tartalmaz valódi személyes adatokat, céges információkat vagy titkos adatokat.

---

## 📋 Az alkalmazás leírása

Az Automotive Risk Manager egy webalkalmazás, amely egy fiktív autóipari alkatrészgyártó vállalat **információbiztonsági kockázatkezelését** támogatja, különös tekintettel a beszállítói folyamatokra.

A rendszer segítségével dokumentálhatók és nyomon követhetők:
- Beszállítók információbiztonsági kockázatai
- Kockázatértékelési eredmények (valószínűség × hatás)
- Kezelési intézkedések és azok állapota
- Auditnaplók a fontos adatmódosításokról

---

## 🗂️ Mappaszerkezet

```
AutomotiveRiskManager/
│
├── backend/                          # ASP.NET Core Web API
│   └── AutomotiveRiskManager.Api/
│       ├── Controllers/
│       │   └── HealthController.cs   # GET /api/health végpont
│       ├── Properties/
│       │   └── launchSettings.json   # Fejlesztői indítási beállítások
│       ├── appsettings.json          # Alap konfiguráció
│       ├── appsettings.Development.json  # Fejlesztői konfiguráció (nem Git-ben!)
│       └── Program.cs                # Belépési pont, middleware konfiguráció
│
├── frontend/                         # React + TypeScript + Vite
│   ├── public/
│   │   └── favicon.svg
│   ├── src/
│   │   ├── components/
│   │   │   └── StatusBadge.tsx       # Backend állapot megjelenítő komponens
│   │   ├── services/
│   │   │   └── healthService.ts      # API kommunikáció
│   │   ├── App.tsx                   # Főkomponens
│   │   ├── index.css                 # Globális stílusok
│   │   └── main.tsx                  # React belépési pont
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   ├── tsconfig.node.json
│   └── vite.config.ts                # Vite + proxy konfiguráció
│
├── docs/                             # Dokumentáció
├── .gitignore
└── README.md
```

---

## 🛠️ Technológiák

| Réteg | Technológia | Verzió |
|---|---|---|
| Frontend | React + TypeScript | 18.x + 5.x |
| Fejlesztői szerver | Vite | 5.x |
| Backend | ASP.NET Core Web API | .NET 8 |
| Programnyelv | C# | 12 |
| Adatbázis | PostgreSQL | 16.x |
| ORM | Entity Framework Core | 8.x (következő szakasz) |
| Hitelesítés | ASP.NET Core Identity | 8.x (következő szakasz) |
| Verziókezelés | Git | – |

---

## 🚀 Indítási lépések

### Előfeltételek

Győződj meg róla, hogy a következők telepítve vannak:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+ LTS](https://nodejs.org/)
- [PostgreSQL 16](https://www.postgresql.org/download/windows/) *(a következő szakaszhoz szükséges)*
- [Git](https://git-scm.com/)

### 1. Backend indítása

Nyiss egy PowerShell / terminál ablakot:

```powershell
cd backend/AutomotiveRiskManager.Api
dotnet restore
dotnet run
```

A backend elindul: **http://localhost:5000**
Swagger UI elérhető: **http://localhost:5000/swagger**

### 2. Frontend indítása

Nyiss egy **második** terminál ablakot:

```powershell
cd frontend
npm install
npm run dev
```

A frontend elindul: **http://localhost:5173**

### 3. Tesztelés

- Nyisd meg böngészőben: **http://localhost:5173**
- A kezdőoldalon látható a backend kapcsolat állapota
- Közvetlenül is tesztelheted: **http://localhost:5000/api/health**

---

## ✅ 1. fejlesztési szakasz – Ellenőrzési eredmények

| Ellenőrzési pont | Eredmény | Megjegyzés |
|---|---|---|
| .NET SDK telepítve | ✅ | v8.x |
| Node.js / npm telepítve | ✅ | v20.x+ |
| Git telepítve | ✅ | – |
| PostgreSQL telepítve | ✅ | v16.x |
| Backend elindul | ✅ | http://localhost:5000 |
| `/api/health` válaszol | ✅ | JSON válasz |
| Frontend elindul | ✅ | http://localhost:5173 |
| Frontend ↔ Backend kommunikál | ✅ | Vite proxy-n keresztül |

---

## 🔜 Következő fejlesztési szakasz

- PostgreSQL adatbázis létrehozása és kapcsolat beállítása
- Entity Framework Core integrálása
- ASP.NET Core Identity alapú felhasználókezelés
- JWT token alapú hitelesítés

---

## 📄 Licenc

Kizárólag oktatási célra készült. Nyilvános terjesztésre nem alkalmas.


---

## 🔐 3. fejlesztési szakasz – Hitelesítés és szerepkörök

**Technológia:** ASP.NET Core Identity (PostgreSQL), cookie-alapú munkamenet, Antiforgery (CSRF).

| Elem | Megoldás |
|---|---|
| Jelszótárolás | Identity jelszókivonat (nyílt szöveg sosem tárolódik) |
| Jelszószabály | min. 12 karakter, kisbetű, nagybetű, számjegy |
| Munkamenet | `__Host-ARM.Auth` cookie: HttpOnly, Secure, SameSite=Strict, 30 perc tétlenségi lejárat |
| CSRF | `X-CSRF-TOKEN` fejléc minden POST/PUT/PATCH/DELETE kérésnél |
| Zárolás | 5 sikertelen belépés után 15 perc |
| Alapértelmezés | minden végpont hitelesítést igényel (kivétel: health, csrf, login, logout) |

### Szerepkörök

| Funkció | Administrator | RiskManager | Viewer |
|---|:-:|:-:|:-:|
| Beszállítók megtekintése | ✅ | ✅ | ✅ |
| Beszállítók létrehozása, módosítása, inaktiválása | ✅ | ✅ | ❌ (403) |
| Kockázatok megtekintése és szűrése | ✅ | ✅ | ✅ |
| Kockázatok létrehozása, szerkesztése, értékelése, státuszváltása | ✅ | ✅ | ❌ (403) |
| Kockázatcsökkentő intézkedések megtekintése és szűrése | ✅ | ✅ | ✅ |
| Kockázatcsökkentő intézkedések létrehozása, módosítása, státuszváltása | ✅ | ✅ | ❌ (403) |
| Felhasználók és szerepkörök kezelése | ✅ | ❌ (403) | ❌ (403) |

A jogosultságokat a **szerver** ellenőrzi; a felületen elrejtett gombok csak kényelmi funkciók.

### Első Administrator

Nincs nyilvános regisztráció. Az első Administrator a `SeedAdmin:UserName`, `SeedAdmin:Email` és
`SeedAdmin:Password` konfigurációból jön létre induláskor (User Secrets vagy Codespaces-titok:
`ARM_ADMIN_USERNAME`, `ARM_ADMIN_EMAIL`, `ARM_ADMIN_PASSWORD`), de **csak ha még nincs Administrator**.
A jelszó nincs a forráskódban, a repóban és a naplóban. A részletes lépések a fejlesztői útmutatóban vannak.

### Új EF Core migráció

A migrációt `dotnet ef migrations add AddIdentity` paranccsal kell létrehozni (Codespaces).
Új táblák: `users`, `roles`, `user_roles`, `user_claims`, `user_logins`, `role_claims`, `user_tokens`.
A `suppliers` tábla és adatai nem változnak.

---

## ⚠️ 4. fejlesztési szakasz – Beszállítói kockázatértékelési modul

**Cél:** Beszállítókhoz rendelt információbiztonsági kockázatok nyilvántartása és 5×5-ös kockázati mátrix alapú automatikus értékelése.

| Elem | Megvalósítás |
|---|---|
| Adatbázis-tábla | `risks` (idegen kulcs: `SupplierId` → `suppliers.Id`, `DeleteBehavior.Restrict`) |
| Kockázatszámítás | `Score = Probability (1–5) × Impact (1–5)` → `1..25` (kizárólag szerveroldalon: `RiskCalculator`) |
| Besorolási szintek | `1–4`: `Low` (Alacsony), `5–9`: `Medium` (Közepes), `10–16`: `High` (Magas), `17–25`: `Critical` (Kritikus) |
| Életciklus-státuszok | `Open` (Nyitott), `UnderReview` (Felülvizsgálat alatt), `Mitigated` (Csökkentett / Kezelt), `Closed` (Lezárt / Archivált – fizikai törlés helyett) |
| REST API végpontok | `GET /api/risks`, `GET /api/risks/{id}`, `POST /api/risks`, `PUT /api/risks/{id}`, `PATCH /api/risks/{id}/status` |
| Egységtesztek | `backend/AutomotiveRiskManager.Tests/RiskCalculatorTests.cs` (xUnit határérték- és validációs tesztek) |
| EF Core migráció | `dotnet ef migrations add AddRiskAssessments` (GitHub Codespacesben generálandó) |

---

## 🔧 5. fejlesztési szakasz – Kockázatcsökkentő intézkedések modul

**Cél:** A beszállítói kockázatokhoz kapcsolódó kockázatcsökkentő intézkedések (`MitigationAction`) nyilvántartása, felelősök és határidők nyomon követése.

| Elem | Megvalósítás |
|---|---|
| Adatbázis-tábla | `mitigation_actions` (idegen kulcs: `RiskId` → `risks.Id`, 1:N kapcsolat, `DeleteBehavior.Restrict`) |
| Állapotok (`MitigationStatus`) | `Planned` (Tervezett), `InProgress` (Folyamatban), `Completed` (Befejezett – `CompletedAt` automatikus rögzítése), `Cancelled` (Visszavont) |
| Prioritások (`MitigationPriority`) | `Low` (Alacsony), `Medium` (Közepes), `High` (Magas), `Critical` (Kritikus) |
| Határidő-lejárat (`IsOverdue`) | Külön számított jelző (`MitigationPolicy.IsOverdue`) – nem írja felül a munkafolyamat státuszát |
| Kockázat-függetlenség | Az intézkedés befejezése nem módosítja automatikusan a kapcsolódó `Risk` pontszámát vagy szintjét (a maradványkockázat újraértékelése külön `RiskManager` feladat) |
| REST API végpontok | `GET /api/mitigations`, `GET /api/mitigations/{id}`, `GET /api/risks/{id}/mitigations`, `POST /api/mitigations`, `PUT /api/mitigations/{id}`, `PATCH /api/mitigations/{id}/status` |
| Egységtesztek | `backend/AutomotiveRiskManager.Tests/MitigationPolicyTests.cs` (xUnit üzletiszabály-tesztek) |
| EF Core migráció | `dotnet ef migrations add AddMitigationActions` (GitHub Codespacesben generálandó) |

---

## 📊 6. fejlesztési szakasz – Vezetői Dashboard modul

**Cél:** Vezetői szintű, valós idejű információbiztonsági áttekintés a PostgreSQL adatbázis aktuális adataiból, szerveroldali EF Core (`AsNoTracking`) lekérdezésekkel.

| Elem | Megvalósítás |
|---|---|
| REST API végpont | `GET /api/dashboard/summary` (`ViewDashboard` szabályzat: `Administrator`, `RiskManager`, `Viewer`) |
| Vezetői KPI-kártyák (5 db) | Összes beszállító, Összes kockázat, Magas és kritikus kockázatok összevont száma, Nyitott intézkedések száma, Lejárt határidejű intézkedések száma |
| Megoszlási diagramok (2 db) | Kockázatok megoszlása kockázati szint szerint (`Low`, `Medium`, `High`, `Critical`) + Intézkedések megoszlása végrehajtási állapot szerint (`Planned`, `InProgress`, `Completed`, `Cancelled`) |
| Figyelmet igénylő tételek (Top 5) | Legmagasabb pontszámú nyitott kockázatok (max. 5), Lejárt határidejű intézkedések (max. 5), Közelgő határidők a következő 7 napon belül (max. 5) |
| Egységtesztek | `backend/AutomotiveRiskManager.Tests/DashboardSummaryCalculatorTests.cs` (determinisztikus xUnit tesztek injektált UTC időponttal) |
| Adatbázis-változás | Nem igényel új táblát vagy új EF Core migrációt (a meglévő `suppliers`, `risks`, `mitigation_actions` táblákat összegzi) |
