# 📍 Project "Spot" (Hyper-Local Geofenced Ecosystem)

[![CI](https://github.com/Vardan9191/spot-ecosystem/actions/workflows/ci.yml/badge.svg)](https://github.com/Vardan9191/spot-ecosystem/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![PostGIS](https://img.shields.io/badge/PostGIS-16--3.4-blue.svg)](https://postgis.net/)
[![Flutter](https://img.shields.io/badge/Flutter-3.24+-cyan.svg)](https://flutter.dev/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> **Հիպեր-լոկալ գեոլոկացիոն էկոհամակարգ** (Hyper-Local Geofenced Shopping & Merchant Media Ecosystem).  
> Օպտիմիզացված է մարտկոցի խնայողության, PostGIS SRID 4326 տարածական (spatial) արագագործ ինդեքսների և օֆլայն աշխատանքի (offline-first) համար:

---

## 🏗️ Ճարտարապետություն և Կառուցվածք (Architecture)

```text
spot-ecosystem/
├── backend/                  # .NET 10 Web API + Clean Architecture + PostGIS / NTS
│   ├── src/
│   │   ├── Domain/          # Entity-ներ, WGS 84 Haversine հաշվարկներ, Spatial Point մոդելներ
│   │   ├── Infrastructure/  # EF Core, PostGIS DbContext, GIST ինդեքսներ, Repositories
│   │   └── Api/             # Controllers, Swagger, Dependency Injection
│   └── tests/
│       └── Spot.Tests/      # 1.5կմ սահմանի ավտոմատ ինտեգրացիոն թեստեր (100% Pass)
├── mobile/                   # Flutter Հավելված (iOS & Android)
│   ├── lib/
│   │   ├── core/            # Location service, Geofence manager, Local notifications, 4h cooldown
│   │   └── features/        # Map, Feed (Shorts/Stories), Task Lists, Reviews
│   └── pubspec.yaml
├── docker/
│   ├── docker-compose.yml   # PostgreSQL 16 + PostGIS 3.4 + Redis
│   └── initdb/              # SQL միգրացիաներ և GIST ինդեքսներ
└── docs/
    ├── architecture.md      # Տեխնիկական և ալգորիթմական նկարագրություն
    └── setup.md             # Տեղադրման և գործարկման ուղեցույց
```

---

## ⚡ Տեխնիկական Հիմնական Հատկանիշներ

1. **Տարածական Շարժիչ (Spatial Engine)**:
   - **SRID 4326** (WGS 84): NetTopologySuite `Point(X, Y)` (որտեղ X = Longitude, Y = Latitude):
   - **GIST Ինդեքսներ**: Բոլոր գեոլոկացիոն սյուների վրա (`stores.location`, `task_lists.custom_location`):
   - **Hyper-Local Հարցումներ**: `GET /api/v1/stores/nearby?latitude={lat}&longitude={lng}&radiusMeters={radius}&category={cat}`: Ֆիլտրում է ըստ տրված շառավղի և դասավորում ըստ մոտիկության:

2. **Մարտկոցի Խնայողություն & Geofencing (Mobile)**:
   - **Adaptive Distance Filter**: 20-մետրանոց ֆիլտր և բալանսավորված ճշգրտություն՝ պրոցեսորը անընդհատ չարթնացնելու համար:
   - **4-Ժամյա Հակասպամ Կուլդաուն (4-Hour Cooldown)**: Կանխում է push ծանուցումների սպամը նույն խանութի/գոտու համար:

3. **Offline-First Task & Shopping Lists**:
   - Գնումների ցուցակները հասանելի են և խմբագրելի անգամ առանց ինտերնետի:

---

## 🚀 Արագ Գործարկում (Quickstart)

### 1. Տվյալների Բազա (PostgreSQL + PostGIS + Redis)
```bash
cd docker
docker compose up -d
```

### 2. Backend API և Թեստեր
```bash
cd backend
dotnet build Spot.slnx
dotnet test tests/Spot.Tests/Spot.Tests.csproj --verbosity normal
```

API-ի գործարկում:
```bash
cd src/Api
dotnet run
```
Swagger UI: `http://localhost:5000/swagger`
SignalR Endpoint: `http://localhost:5000/hubs/location`

### 3. Mobile Հավելված (Flutter)
```bash
cd mobile
flutter pub get
flutter run
```

---

## 📋 Պրոֆեսիոնալ Ճանապարհային Քարտեզ (Roadmap & Sprints)

- [x] **Sprint 1: Core Foundation & Spatial Proof-of-Concept**
  - [x] Docker + PostGIS 16 + Redis կոնֆիգուրացիա
  - [x] PostgreSQL սխեմա և GIST տարածական ինդեքսներ
  - [x] .NET 10 Clean Architecture API & NetTopologySuite
  - [x] 1.5կմ շառավղով մոտակա խանութների ավտոմատ թեստավորում
  - [x] Flutter Core (Location, Geofencing, 4h Cooldown, Offline Store)
- [ ] **Sprint 2: Real-Time Sync & Media Streaming**
  - [x] WebSockets / SignalR իրական ժամանակում գտնվելու վայրի սինխրոնիզացիա (`LocationHub`)
  - [ ] Cloud Storage ինտեգրացիա խանութների Shorts/Stories վիդեոների համար
- [ ] **Sprint 3: Merchant Dashboard & Geofence Campaign Analytics**
  - [ ] Վաճառողների կառավարման վահանակ (Web Dashboard)
  - [ ] Գովազդային գեո-գոտիների ստեղծում և կոնվերսիայի վիճակագրություն
- [ ] **Sprint 4: Production Hardening & CI/CD**
  - [ ] Kubernetes / Helm Charts դեփլոյմենթ
  - [ ] Store Review Sentiment Analysis & Fraud Detection
