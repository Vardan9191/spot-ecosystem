# Project "Spot" Local Setup & Verification Guide

## 1. Prerequisites

- **.NET SDK**: .NET 10.0 (or .NET 9.0)
- **Docker & Docker Compose**: For PostgreSQL 16 + PostGIS 3.4 and Redis
- **Flutter SDK**: 3.2.0+ (for mobile client)

---

## 2. Docker & Database Setup

Start PostgreSQL with PostGIS extension and Redis:

```bash
cd spot-ecosystem/docker
docker compose up -d
```

The database container automatically initializes:
- `CREATE EXTENSION IF NOT EXISTS postgis;`
- `CREATE EXTENSION IF NOT EXISTS "uuid-ossp";`
- Creates all spatial tables with GIST indexes.
- Seeds test fixtures around NYC Downtown Civic Center.

---

## 3. Backend Service

### Build the Solution
```bash
cd spot-ecosystem/backend
dotnet build Spot.slnx
```

### Run Integration & Spatial Tests
```bash
cd spot-ecosystem/backend
dotnet test tests/Spot.Tests/Spot.Tests.csproj --verbosity normal
```

All 11 automated spatial boundary and integration tests verify:
- Accurate geodesic distance computation using Haversine formula.
- Stores within 1.5km (250m, 650m, 1250m) are returned and ordered by proximity.
- Stores outside 1.5km (2600m) are excluded.
- Category filtering (`?category=coffee`).
- TaskList & TaskItem CRUD workflows.
- Store multi-metric reviews.

### Run the Web API
```bash
cd spot-ecosystem/backend/src/Api
dotnet run
```
Access Swagger UI at `http://localhost:5000/swagger`.

---

## 4. Mobile Client (Flutter)

```bash
cd spot-ecosystem/mobile
flutter pub get
flutter run
```

Features included:
1. **Map & Zones**: Interactive geofence visualizer with circular overlays and distance indicators.
2. **Stories & Feed**: Vertical merchant media presentation with flash deals and badges.
3. **Task Lists**: Offline-first shopping lists with checkable items and geofence association.
4. **Reviews**: 3-dimensional ratings (Service, Quality, Overall) and customer reviews.
