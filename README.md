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

