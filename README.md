# 📍 Project "Spot" (Hyper-Local Geofenced Ecosystem)

[![CI](https://github.com/Vardan9191/spot-ecosystem/actions/workflows/ci.yml/badge.svg)](https://github.com/Vardan9191/spot-ecosystem/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![PostGIS](https://img.shields.io/badge/PostGIS-16--3.4-blue.svg)](https://postgis.net/)
[![Redis](https://img.shields.io/badge/Redis-7--Alpine-red.svg)](https://redis.io/)
[![Flutter](https://img.shields.io/badge/Flutter-3.24+-cyan.svg)](https://flutter.dev/)
[![Tests](https://img.shields.io/badge/Tests-33%2F33%20Passing-brightgreen.svg)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> **Հիպեր-լոկալ գեոլոկացիոն էկոհամակարգ** (Hyper-Local Geofenced Shopping & Merchant Media Ecosystem).  
> Օպտիմիզացված է մարտկոցի խնայողության, PostGIS SRID 4326 տարածական (spatial) GIST ինդեքսների, Redis GEO <1ms քեշավորման, SignalR իրական ժամանակի հոսքերի, օֆլայն աշխատանքի (offline-first) և Gemini AI-ով կեղծ ռեվյուների հայտնաբերման համար:

---

## 🏗️ Ճարտարապետություն և Մոնոռեպո Կառուցվածք

```text
spot-ecosystem/
├── backend/                  # .NET 10 Web API + Clean Architecture + PostGIS / NTS
│   ├── src/
│   │   ├── Domain/          # Entity-ներ, WGS 84 Haversine հաշվարկներ, Spatial Point մոդելներ
│   │   ├── Infrastructure/  # EF Core, PostGIS DbContext, GIST ինդեքսներ, Redis GEO Cache, AI Fraud Detector
│   │   └── Api/             # Controllers, SignalR Hubs, Swagger, DTOs, WebSockets
│   └── tests/
│       └── Spot.Tests/      # 33 Unit & Integration թեստեր (100% Pass)
├── mobile/                   # Flutter Հավելված (iOS & Android)
│   ├── lib/
│   │   ├── core/            # Location service, Geofence manager, Battery optimizer, SignalR client
│   │   └── features/        # Map, Feed (Shorts/Stories), Task Lists, Reviews
│   └── pubspec.yaml
├── web/                      # Web Պորտալներ
│   └── merchant-dashboard/  # Merchant Dashboard & Ինտերակտիվ Geofence Canvas Builder
├── docker/
│   ├── docker-compose.yml       # Dev Environment (PostGIS 16 + Redis 7)
│   ├── docker-compose.prod.yml  # Production Full Stack (PostGIS + Redis + .NET 10 + Nginx)
│   ├── nginx/nginx.conf         # Production Reverse Proxy + WebSockets
│   └── initdb/                  # SQL միգրացիաներ և GIST ինդեքսներ
└── docs/
    ├── architecture.md                   # Տեխնիկական և ալգորիթմական նկարագրություն
    ├── setup.md                          # Տեղադրման և գործարկման ուղեցույց
    └── production-server-architecture.md # Production Սերվերների Ճարտարապետություն և Ծախսեր
```

---

## ⚡ Իրականացված Էտապներ (Completed Milestones)

- [x] **Sprint 1: Core Foundation**
  - .NET 10 Clean Architecture API + PostGIS 16-3.4 SRID 4326 GIST ինդեքսներով:
  - 1.5կմ շառավղով մոտակա խանութների որոնում և Haversine գեոդեզիական հաշվարկ:
  - Offline-first գնումների ցուցակներ և ռեվյուների մոդուլ:
  - Flutter բջջային հաճախորդ՝ 4-ժամյա push notification cooldown մեխանիզմով:
- [x] **Sprint 2: Real-Time & Media Ingestion**
  - **Issue #1**: SignalR Real-Time Spatial LocationHub (`/hubs/location`) ուղիղ կոորդինատների հոսքի և ակցիաների բրոդքասթի համար:
  - **Issue #2**: Background Geofencing & 3-Zone Battery Optimization (Dormant >2.5km, Proximity, Entry):
  - **Issue #3**: Merchant Shorts & Video Ingestion API (S3/MinIO presigned URLs, MP4/WebM validation, 24h expiration):
- [x] **Sprint 3: Web Portal & Redis Spatial Caching**
  - **Issue #4**: Merchant Dashboard Web Portal (`web/merchant-dashboard/`) ինտերակտիվ Canvas Geofence Builder-ով:
  - **Issue #5**: Redis Spatial Cache Engine (`GEOADD` / `GEORADIUS`) ենթամիլիվայրկյանային (<1ms) որոնումներով և PostGIS fallback-ով:
- [x] **Sprint 4: AI & Security**
  - **Issue #6**: Review Fraud Detection & AI Sentiment Analysis:
    - Sliding-window velocity rate limiting (>5 ռեվյու/րոպե մեկ IP-ից կամ User ID-ից արգելափակում / ֆլագավորում):
    - Google Gemini AI REST ինտեգրացիա սպամի և կեղծ ռեվյուների հայտնաբերման համար:
    - Սենտիմենտի վերլուծություն Service, Quality և Atmosphere 3 առանձին ուղղություններով:

---

## 🧪 Թեստավորում (Automated Test Suite)

Համակարգում ներառված են 33 ավտոմատացված թեստեր.
```bash
dotnet test backend/tests/Spot.Tests/Spot.Tests.csproj
```
**Արդյունք**: `Пройден! : не пройдено 0, пройдено 33, пропущено 0, всего 33`

---

## 🖥️ Production Սերվերներ և Տեղակայում

Սերվերների ընտրության, չափսերի (Sizing) և տեղակայման մանրամասն ուղեցույցը հասանելի է այստեղ՝  
👉 **[docs/production-server-architecture.md](docs/production-server-architecture.md)**

### Հակիրճ՝ Առաջարկվող Սերվեր
* **Մատակարար**: **Hetzner Cloud (Գերմանիա)**
* **Սերվերի Պլան**: **CPX41** (8 vCPU AMD EPYC, 16 GB RAM, 240 GB NVMe SSD)
* **Ամսական Ծախս**: **~€23.80 (~$26 / ամսական)**
* **Վիդեոների Պահպանում**: **Cloudflare R2** ($0 egress fee, ~$1.50/mo)

---

## 🚀 Տեղակայումը Սերվերում (1 Հրամանով)

```bash
# 1. Կլոնավորել ռեպոզիտորիան
git clone https://github.com/Vardan9191/spot-ecosystem.git
cd spot-ecosystem

# 2. Կարգավորել գաղտնաբառերը
cp docker/.env.example .env

# 3. Գործարկել Production Stack-ը
docker compose -f docker/docker-compose.prod.yml up -d --build
```
