# Keyloop Technical Assessment: The Unified Service Scheduler
### Scenario A (Domain: Ownership) — Automotive Workshop Appointment Platform

---

## 🌟 Executive Overview

In automotive retail and aftersales service, legacy or disconnected scheduling systems lead to chronic workshop friction:
- **Double-booked service bays**, causing physical garage bottlenecks and customer delays.
- **Unqualified technician assignments** (e.g., assigning an internal combustion engine tech to a 400V high-voltage EV battery diagnosis), posing critical safety and warranty liabilities.
- **Customer frustration** caused by inaccurate turnaround estimates and lost bookings.

**The Unified Service Scheduler** is an enterprise multi-resource scheduling engine built on **C# and .NET 10 (LTS)** that replaces manual booking workflows. It enforces **dual-resource constraint satisfaction** (simultaneous availability of an appropriately equipped **Service Bay** and a **Certified Technician** for the entire service duration) backed by **Entity Framework Core 10 and ACID transactional concurrency controls** to completely eliminate double bookings.

---

## 🏗️ Architecture & Core Design Patterns

The solution is architected according to **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS (Command Query Responsibility Segregation)** principles, with strict adherence to **Single Responsibility (SRP)** and **Interface Segregation (ISP)**:

- **Keyloop.Scheduler.Domain**: Pure enterprise domain models, enums, and typed domain exceptions. Every type resides in its own dedicated file with zero third-party dependencies.
- **Keyloop.Scheduler.Core**: Focused CQRS services (`IAvailabilityService`, `IBookingService`, `ISlotRecommender`, `IAppointmentQueryService`, `SchedulerEngine` Facade), immutable DTOs, and segregated repository contracts.
- **Keyloop.Scheduler.Infrastructure**: Persistence layer using Entity Framework Core 10 and SQLite with WAL mode, modular `IEntityTypeConfiguration<T>` classes, segregated repositories, and transactional locking (`BeginTransactionAsync`).
- **Keyloop.Scheduler.Api**: ASP.NET Core 10 REST Web API with Swagger/Swashbuckle OpenAPI documentation, CORS, and RFC 7807 Global Exception Handling Middleware.
- **Frontend & Test Harness**: Modern, responsive workshop dispatch board and customer booking portal with live concurrency conflict simulation.

### 📁 Codebase Directory Structure (One Type Per File)

```
Keyloop.Scheduler/
├── src/
│   ├── Keyloop.Scheduler.Domain/                # Pure Domain Layer
│   │   ├── Entities/
│   │   │   ├── Dealership.cs
│   │   │   ├── ServiceBay.cs
│   │   │   ├── Technician.cs
│   │   │   ├── TechnicianSkill.cs
│   │   │   ├── ServiceType.cs
│   │   │   ├── ServiceRequiredSkill.cs
│   │   │   ├── Vehicle.cs
│   │   │   ├── Customer.cs
│   │   │   └── Appointment.cs
│   │   ├── Enums/
│   │   │   ├── BayType.cs
│   │   │   ├── SkillCode.cs
│   │   │   ├── AppointmentStatus.cs
│   │   │   └── PowertrainType.cs
│   │   └── Exceptions/
│   │       ├── ResourceConflictException.cs
│   │       ├── BusinessRuleValidationException.cs
│   │       ├── EntityNotFoundException.cs
│   │       └── SchedulingException.cs
│   │
│   ├── Keyloop.Scheduler.Core/                  # Application CQRS Layer
│   │   ├── DTOs/
│   │   │   ├── AvailabilitySlotDto.cs
│   │   │   ├── CreateAppointmentRequest.cs
│   │   │   ├── AppointmentResponse.cs
│   │   │   ├── ConcurrencyConflictResponse.cs
│   │   │   ├── ServiceTypeDto.cs
│   │   │   └── AppointmentMappingExtensions.cs
│   │   ├── Interfaces/
│   │   │   ├── IAvailabilityService.cs          # Pure query for available slots
│   │   │   ├── IBookingService.cs               # Transactional command handler
│   │   │   ├── ISlotRecommender.cs              # Alternate slot discovery
│   │   │   ├── IAppointmentQueryService.cs      # Read queries for dispatch board
│   │   │   ├── ISchedulerEngine.cs              # Unified high-level facade
│   │   │   ├── IAppointmentRepository.cs        # Appointment persistence & locking
│   │   │   ├── IWorkshopResourceRepository.cs   # Dealership, bay & tech queries
│   │   │   ├── ICustomerVehicleRepository.cs    # Customer & vehicle lookup
│   │   │   └── IDatabaseMaintenance.cs          # Test harness reset
│   │   ├── Services/
│   │   │   ├── AvailabilityService.cs
│   │   │   ├── BookingService.cs
│   │   │   ├── SlotRecommender.cs
│   │   │   ├── AppointmentQueryService.cs
│   │   │   └── SchedulerEngine.cs               # Facade pattern
│   │   └── DependencyInjection.cs
│   │
│   ├── Keyloop.Scheduler.Infrastructure/        # Persistence & EF Core Layer
│   │   ├── Data/
│   │   │   ├── SchedulerDbContext.cs
│   │   │   └── Configurations/                  # Modular IEntityTypeConfiguration<T>
│   │   │       ├── DealershipConfiguration.cs
│   │   │       ├── ServiceBayConfiguration.cs
│   │   │       ├── TechnicianConfiguration.cs
│   │   │       ├── TechnicianSkillConfiguration.cs
│   │   │       ├── ServiceTypeConfiguration.cs
│   │   │       ├── CustomerConfiguration.cs
│   │   │       ├── VehicleConfiguration.cs
│   │   │       └── AppointmentConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── AppointmentRepository.cs
│   │   │   ├── WorkshopResourceRepository.cs
│   │   │   ├── CustomerVehicleRepository.cs
│   │   │   └── DatabaseMaintenance.cs
│   │   └── DependencyInjection.cs
│   │
│   └── Keyloop.Scheduler.Api/                   # Web API & UI Layer
│       ├── Controllers/
│       │   ├── AvailabilityController.cs
│       │   ├── AppointmentsController.cs
│       │   ├── MetaController.cs
│       │   └── DemoController.cs
│       ├── Middleware/ErrorHandlingMiddleware.cs
│       ├── wwwroot/                             # Responsive Web UI & Test Harness
│       └── Program.cs
│
└── tests/
    └── Keyloop.Scheduler.Tests/                 # Unit & Concurrency Tests
        ├── SchedulerEngineTests.cs
        └── AppointmentConcurrencyIntegrationTests.cs
```

---

## 🚀 Quick Start Guide

### Option 1: Run with Docker & Docker Compose (Recommended — Zero Host Setup)

Requires only [Docker](https://www.docker.com/). The multi-stage Docker build automatically restores dependencies, builds the C# solution, executes the xUnit test suite, and starts the ASP.NET Core API with SQLite persistence:

```bash
# 1. Navigate to the project root
cd /Users/riturajrajput/Documents/Keyloop

# 2. Build image, execute tests, and run container
docker compose up --build
```

Once running:
- **Interactive Web App & Dispatch Board**: Open [http://localhost:5001](http://localhost:5001)
- **REST API Base**: `http://localhost:5001/api/v1`
- **Database**: Automatically seeds persistent SQLite storage in `./data/scheduler.db`

To run tests independently via Docker:
```bash
docker build --target build -t keyloop-tests .
```

---

### Option 2: Run via Local .NET 10 CLI (If .NET 10 SDK is installed)

```bash
# 1. Restore & Build
dotnet restore
dotnet build

# 2. Run xUnit automated test suite
dotnet test

# 3. Launch Web API
dotnet run --project src/Keyloop.Scheduler.Api
```

## 🧪 Testing Suite & Business Logic Validation

The automated test suite in `tests/Keyloop.Scheduler.Tests` is built with **xUnit**, **Moq**, and **Microsoft.Data.Sqlite**, split into domain unit tests and physical SQLite integration tests:

### 1. Domain Constraint Solver Tests (`SchedulerEngineTests.cs`)
- `GetAvailability_ShouldReturnAvailableSlots_WhenBayAndQualifiedTechAreFree`: Verifies that availability queries only return slots where both an eligible bay and a certified technician are simultaneously free.
- `GetAvailability_ShouldReturnEmpty_WhenNoTechnicianHoldsRequiredCertification`: Ensures that services requiring specialized certifications (e.g. `EvPowertrain` for High-Voltage Battery diagnostics) strictly reject unqualified technicians.
- `GetAvailability_ShouldFilterOutUnmatchedBays_WhenSpecializedBayTypeRequired`: Confirms that EV or heavy hoist services are strictly restricted to properly equipped service bays.
- `GetAvailability_ShouldRespectTemporalNonOverlap_AcrossMultiSlotServiceDuration`: Guarantees continuous availability across the entire multi-interval service duration (e.g. 90-minute EV service).
- `BookAppointment_ShouldThrowConflictException_WhenBayOrTechIsDoubleBooked`: Validates domain-level conflict detection when a bay or technician is already committed.
- `BookAppointment_ShouldThrowConflictException_WhenPreferredBayIsAlreadyOccupied`: Validates that requesting a preferred service bay that is already occupied throws `ResourceConflictException` (HTTP 409) rather than a generic validation error.
- `BookAppointment_ShouldThrowValidationException_WhenTimeOutsideOperatingHours`: Confirms that bookings outside dealership operating hours (09:00 - 19:00 IST) are rejected with HTTP 400 validation error.
- `BookAppointment_ShouldPersistAndConfirm_WhenConstraintRequirementsMet`: Validates successful appointment creation and entity state transition to `Confirmed`.

### 2. Physical SQLite Concurrency & WAL Integration Tests (`AppointmentConcurrencyIntegrationTests.cs`)
- `SqliteDatabase_ShouldHaveWalModeAndBusyTimeoutEnabled`: Connects to physical SQLite instance and queries `PRAGMA journal_mode;` and `PRAGMA busy_timeout;` to prove Write-Ahead Logging (WAL) and 5000ms concurrency timeouts are active.
- `ConcurrentBooking_TwoParallelRequestsForLastSlot_OneSucceedsOneFailsAtomically`: Spawns two uncoordinated concurrent threads (`Task.WhenAll`) attempting to claim the single remaining EV Bay at the exact same millisecond. Proves that `IsolationLevel.Serializable` transactions serialize writers, exactly one booking commits with 201 Created, and the competing thread is safely rejected with `ResourceConflictException` (HTTP 409 with suggested alternatives).
- `CreateAppointment_WhenSlotAlreadyBooked_ThrowsResourceConflictException`: Exercises direct repository transaction rollback and ensures zero orphan records remain in the database.

To execute all 11 tests:
```bash
docker build --target build -t keyloop-tests .
# or if local .NET 10 SDK is installed:
dotnet test
```

---

## 🤖 AI Collaboration Narrative

In accordance with the Keyloop technical assessment guidelines, Generative AI (Google Antigravity and AI coding agents) was utilized as an interactive engineering accelerator throughout this project. Below is the strategy, verification methodology, and quality assurance framework applied to guide the AI, audit its output, and ensure full engineering ownership of the final codebase.

### 1. High-Level Strategy for Guiding the AI
Rather than using AI for unbounded code generation, I directed the agent as a **Senior .NET Pair Programmer and Architecture Reviewer**:
- **Domain-Specific Scoping**: Grounded the AI strictly in Keyloop's aftersales workshop domain (automotive dealership management, technician certifications, bay capabilities, and multi-resource interval scheduling).
- **Phased Architectural Decomposition**: Decomposed the implementation into discrete engineering phases:
  1. Pure domain entity modeling with explicit normalization between technician profiles and skill certifications.
  2. Constraint solver formulation using continuous interval non-overlap logic across joint resources.
  3. ACID transactional concurrency controls with composite indices to prevent race conditions.
  4. Test-driven verification focusing on boundary and failure conditions before happy-path implementation.

### 2. Process for Verifying and Refining AI Output
Every suggestion and code block was critically reviewed, benchmarked, and revised:
- **Concurrency Collision Auditing**: When initial drafts relied on in-memory application-level filtering, I recognized the race-condition vulnerability under simultaneous bookings and directed the implementation of database-level serializable transactions (`BeginTransactionAsync`) with composite indexing on temporal ranges.
- **Data Model Normalization**: When early outputs modeled technician certifications as flat enums, I refactored the design into dedicated `TechnicianSkill` and `ServiceRequiredSkill` relational mappings to reflect real-world multi-skilled technician capabilities (e.g., EV Powertrain, ADAS Calibration, Brakes).
- **Architecture Decomposition**: When services grew monolithic, I enforced Single Responsibility (SRP) and Interface Segregation (ISP), decomposing the core logic into dedicated CQRS services (`AvailabilityService`, `BookingService`, `SlotRecommender`, `AppointmentQueryService`) and segregated repositories.

### 3. Ensuring Final Production Quality
1. **Automated Test Validation**: Backed every business requirement with comprehensive xUnit unit and integration tests (including multi-threaded concurrency tests verifying atomic conflict rejection with HTTP 409).
2. **Framework Alignment**: Upgraded and validated the entire solution against **.NET 10 LTS** and **EF Core 10.0.14** with zero compiler warnings and zero security advisories.
3. **Enterprise Standards**: Implemented RFC 7807 problem details error handling, structured logging with correlation IDs (`X-Correlation-ID`), and modular EF Core configurations.

---

## 📊 API Specification Summary

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/availability?dealershipId=&serviceTypeId=&date=` | Evaluates real-time continuous slots ensuring Bay + Qualified Tech pairing. |
| `POST` | `/api/v1/appointments` | Atomically creates and persists an appointment record; prevents double booking. |
| `GET` | `/api/v1/appointments?dealershipId=` | Retrieves confirmed appointments for the dealership dispatch board. |
| `GET` | `/api/v1/meta/dealerships` | Retrieves operating dealerships with operating hours and metadata. |
| `GET` | `/api/v1/meta/services` | Fetches service catalog with required bay types and skills. |
| `GET` | `/api/v1/meta/resources/{dealershipId}` | Inspects active bays and technicians with their certified skills. |
| `POST` | `/api/v1/demo/reset` | Resets the database to initial clean seed state for demo walkthroughs. |
| `POST` | `/api/v1/meta/reset-demo` | Backward-compatible alias forwarding to the demo reset handler. |

---

## 📋 Evaluation Criteria & Requirements Traceability Matrix

| Evaluation Dimension | Assessment Requirement | Implementation & Verification Evidence |
| :--- | :--- | :--- |
| **1. Problem Solving & System Design** | Clear architectural plan, component roles, data flow, constraint solver formulation, and concurrency strategy. | Documented in [`SYSTEM_DESIGN.md`](./SYSTEM_DESIGN.md) featuring Mermaid diagrams, DDD aggregates, CQRS decomposition, mathematical interval algebra formulation, and serializable transaction flows. |
| **2. Technical Execution** | High-quality, maintainable, tested service implementation with persistent storage and client harness. | Production-grade C# / .NET 10 LTS Clean Architecture, EF Core 10 with SQLite WAL mode, dual-resource allocation engine, segregated repositories, and responsive web client. |
| **3. AI Engineering & Verification** | Strategic AI direction, rigorous verification, debugging, and ownership of the solution. | Documented above in AI Collaboration Narrative; critical refactoring from in-memory checks to database transactional concurrency, skill normalization, and 100% test-backed validation. |
| **4. Communication & Presentation** | Clear documentation, API contracts, and test harness walkthrough. | Comprehensive `README.md`, Swagger OpenAPI documentation (`http://localhost:5001/swagger`), and interactive web UI with concurrency simulator. |
