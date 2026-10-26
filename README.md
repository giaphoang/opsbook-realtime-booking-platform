**# 🍔 Real-Time Order Management System (SignalR)

![Framework](https://img.shields.io/badge/ASP.NET%20Core%20API-MVC-purple)
![RealTime](https://img.shields.io/badge/Tech-SignalR-red)
![Architecture](https://img.shields.io/badge/Architecture-N--Layer-blue)
![Communication](https://img.shields.io/badge/Pattern-API%20Consume-orange)

## 📖 Overview
This project is a restaurant order management system developed to practice **Real-Time Web Applications** using **SignalR**.

The project is structured into two main parts:
1.  **Web API:** The backend that handles database operations and logic.
2.  **Web UI:** The frontend that **consumes the API** using `HttpClient` to display data.

It features an **N-Layer Architecture** to ensure separation of concerns between Data Access, Business, and Presentation layers.

## 🚀 Key Features

### 📡 Real-Time Communication (SignalR)
* **Live Table Status:** When an order is placed, the table color changes instantly on the dashboard without refreshing the page.
* **Instant Statistics:** Dashboard counters (Total Money, Active Orders, etc.) update in real-time as data changes in the database.

### 🔌 API Consumption
* The User Interface (MVC) does not connect to the database directly.
* All CRUD operations are performed by sending **HTTP Requests (GET, POST, PUT, DELETE)** to the Web API backend.

### 🏗️ Architecture
* **N-Layer Design:** Organized into Entity, DataAccess, Business, and API/UI layers.
* **DTOs:** Data Transfer Objects are used to carry data between processes.

## 🛠️ Tech Stack

| Component | Technology |
| :--- | :--- |
| **Backend** | .NET Core Web API |
| **Frontend** | ASP.NET Core MVC (Consuming API) |
| **Real-Time** | **Microsoft SignalR** |
| **Architecture** | N-Layered Architecture |
| **Data Access** | Entity Framework Core |
| **Database** | MSSQL |
| **Communication** | HttpClient / Newtonsoft.Json |

---

# ⚡ OpsBook — Real-Time Booking Operations Platform (this fork)

This fork extends the original restaurant system into a booking *operations* platform that
demonstrates three specific capabilities, each backed by measurements instead of assumptions:
push-based realtime delivery, Redis-cached server-rendered pages at peak traffic, and an
automated MSTest suite for the WebAPI and authentication middleware.

## 1️⃣ SignalR push replaces database polling

Every committed booking / payment mutation now publishes an event to the
`Operations` SignalR hub (`SignalRApi/Hubs/OperationsHub.cs`) through
`SignalRApi/Realtime/SignalROperationsEventPublisher.cs`. Operator screens subscribe once
and listen for `ReceiveOperationEvent` instead of re-reading the database on a timer.

**Measured on the same write workload (32 committed writes per mode,`POST /api/LatencyBenchmark`):**

| Delivery mode | P50 | P95 | Mean |
| :--- | ---: | ---: | ---: |
| Database polling (1000 ms interval) | 1006 ms | **1010 ms** | 1005 ms |
| SignalR push | 2 ms | **3 ms** | 1.9 ms |

→ **99.7% lower P95 notification latency**; the polling receiver also performed one
database read per wake-up, while push requires none.

The polling baseline remains available (`DeliveryMode.DatabasePolling`) so the comparison
is reproducible, not a one-off artifact.

## 2️⃣ Redis cache-aside for peak-traffic read paths

`SignalRWebUI` renders its operations pages server-side (MVC + Razor, attribute-routed
`/Ops/Bookings/Today` and `/Ops/Tables`), but the API payload behind each render is cached
in Redis (`SignalRWebUI/Services/CachedServerRenderedCatalog.cs`):

* first render → API call + Redis write (`ops:bookings:today`, `ops:tables`),
* repeat render → model restored from Redis, **zero** API calls,
* `RemoveAsync(key)` invalidation slot after booking / inventory mutations.

**Measured with k6 (30 s constant concurrency, anonymous GET read path, in-memory DB):**

| Traffic | Virtual users | Throughput | p95 latency | Failed requests |
| :--- | ---: | ---: | ---: | ---: |
| 1× baseline | 10 | 8,508 req/s | 2.46 ms | 0 |
| 4× peak | 40 | 12,074 req/s | 7.38 ms | 0 |

→ At 4× concurrency the read path still sustains **~12k req/s with sub-8 ms p95** and no
errors across ~617k requests. (`tools/run-benchmarks.sh` reproduces both runs.)

## 3️⃣ 40+ MSTest tests for controllers, auth middleware, and booking/payment workflows

`SignalRApi.Tests` (MSTest + Moq + WebApplicationFactory) — **47 passing tests**: 

* **Auth middleware (7):** valid bearer accepted, missing/garbage/expired/forged-key tokens
  rejected, basic-auth scheme rejected, anonymous hub negotiate rejected.
* **WebAPI controllers (5 + 6 direct):** authenticated booking list, validation errors on
  create (400), create/delete wiring through the booking service, not-found guards, and
  realtime event publication on every mutation.
* **Realtime delivery (5):** push targets the `operations` group with
  `ReceiveOperationEvent`, subscribe/unsubscribe join/leave the room, API writes publish
  events.
* **Payment workflow (11):** approve, decline zero/negative amounts, idempotent replay,
  duplicate gateway callbacks processed exactly once, missing idempotency key rejected,
  telemetry decorator, controller-level guards + published status events.
* **Redis-cached pages (7):** first render populates cache, cache hit skips the API,
  distinct keys, admin-only authorization.
* **Latency benchmark (6):** polling vs push behavior, percentile ordering, input guards.

## Running it

```bash
# Build keys + run everything offline (in-memory DB):
docker run --rm -e NUGET_PACKAGES=/nuget-packages \
  -v "$PWD":/work -v "$HOME/.nuget-packages:/nuget-packages" -w /work \
  mcr.microsoft.com/dotnet/sdk:8.0 bash /work/tools/run-benchmarks.sh

# Tests (47):
docker run --rm -e NUGET_PACKAGES=/nuget-packages \
  -v "$PWD":/work -v "$HOME/.nuget-packages:/nuget-packages" -w /work \
  mcr.microsoft.com/dotnet/sdk:8.0 dotnet test SignalRApi.Tests
```

Without Docker you need the .NET 8 SDK, SQL Server (`SignalRApi` connection string), and
Redis (`SignalRWebUI` connection string).

*Based on [Bugra Ozturk](https://github.com/ozturkbugra)'s* Real-Time Order Management System
(MIT).
