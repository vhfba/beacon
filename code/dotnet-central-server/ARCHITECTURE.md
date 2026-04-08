# BEACON Central Server - .NET 9 Migration Documentation

## Overview
This directory contains the .NET 9 C# implementation of the BEACON Central Server, replacing the previous Java/Spring Boot version.

## Architecture
The project follows **Onion Architecture** (Clean Architecture) with strict separation of concerns:

```
┌─────────────────────────────────────────────────┐
│           Presentation Layer                     │
│  (GraphQL: Types, Queries, Mutations)           │
│  - HotChocolate types and resolvers             │
│  - GraphQL response mapping                      │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│         Application Layer                        │
│  (Use Cases, DTOs, Business Orchestration)      │
│  - Domain-drivenuse cases                       │
│  - Data Transfer Objects (DTOs)                 │
│  - Cross-cutting concerns                       │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Infrastructure Layer                           │
│  (EF Core, Repositories, External Services)    │
│  - Entity Framework Core DbContext              │
│  - Repository implementations                   │
│  - Database migrations                          │
│  - External integrations                        │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│         Domain Layer (PURE)                      │
│  (Models, Value Objects, Repository Contracts) │
│  - No external dependencies                     │
│  - Pure C# business logic                       │
│  - Repository interfaces (contracts)            │
│  - Aggregate roots and value objects            │
└─────────────────────────────────────────────────┘
```

**Key Principles:**
- **Dependency Inversion**: Each layer depends only on abstractions from inner layers
- **Domain Purity**: Domain layer contains zero framework dependencies
- **Use Case Orchestration**: Application layer orchestrates domain logic
- **Repository Pattern**: Data access is abstracted behind interfaces
- **Onion Boundaries**: Clear interfaces at each layer boundary

## Directory Structure

```
CentralServer/
├── CentralServer.csproj           # Project configuration
├── Program.cs                     # Dependency injection setup
├── appsettings.json               # Configuration
├── appsettings.Development.json   # Dev overrides
├── Dockerfile                     # Container image
├── docker-compose.yml             # Local dev environment
├── .editorconfig                  # Code style rules
├── README.md                      # Project overview
├── QUICKSTART.md                  # Getting started guide
├── ARCHITECTURE.md                # This file
│
├── Domain/                        # PURE domain layer
│   ├── Models/
│   │   ├── Probe.cs               # Probe aggregate root
│   │   ├── ProbeId.cs             # Value object
│   │   ├── ProbeStatus.cs         # Enum
│   │   ├── TestType.cs            # Value object
│   │   ├── ProbeTestConfiguration.cs
│   │   ├── Plugin.cs              # Plugin aggregate root
│   │   ├── DomainException.cs
│   │   └── ProbeRegistrationException.cs
│   └── Repositories/
│       ├── IProbeRepository.cs
│       ├── ITestTypeRepository.cs
│       ├── IProbeTestConfigurationRepository.cs
│       └── IPluginRepository.cs
│
├── Application/                   # Application layer
│   ├── DTOs/
│   │   ├── RegisterProbeInput.cs
│   │   ├── ProbeDTO.cs
│   │   ├── ProbeTestConfigurationDTO.cs
│   │   ├── UpdateProbeTestConfigInput.cs
│   │   ├── ProbeConfigDTO.cs
│   │   ├── PluginDTO.cs
│   │   └── ErrorResponse.cs
│   └── UseCases/
│       ├── RegisterProbeUseCase.cs
│       ├── GetFleetStatusUseCase.cs
│       ├── GetProbeConfigUseCase.cs
│       ├── UpdateProbeTestConfigUseCase.cs
│       ├── UpdateProbeStatusUseCase.cs
│       ├── ListPluginsUseCase.cs
│       ├── RegisterPluginUseCase.cs
│       ├── GetPluginByIdUseCase.cs
│       ├── SetProbeTestEnabledUseCase.cs
│       └── SetPluginAvailabilityUseCase.cs
│
├── Infrastructure/                # Infrastructure layer
│   └── Persistence/
│       ├── CentralServerDbContext.cs
│       ├── Entities/
│       │   ├── ProbeEntity.cs
│       │   ├── TestTypeEntity.cs
│       │   ├── ProbeTestConfigEntity.cs
│       │   └── PluginEntity.cs
│       ├── Repositories/
│       │   ├── ProbeRepositoryAdapter.cs
│       │   ├── TestTypeRepositoryAdapter.cs
│       │   ├── ProbeTestConfigurationRepositoryAdapter.cs
│       │   └── PluginRepositoryAdapter.cs
│       ├── Migrations/
│       │   ├── 20260401000000_InitialCreate.cs
│       │   └── CentralServerDbContextModelSnapshot.cs
│       └── Configuration/
│           └── ServiceConfiguration.cs
│
└── Presentation/                  # Presentation layer
    └── GraphQL/
        ├── Query.cs
        ├── Mutation.cs
        ├── Responses/
        │   ├── FleetStatusResponse.cs
        │   ├── RegisterProbeResponse.cs
        │   ├── RegisterPluginResponse.cs
        │   ├── UpdateProbeTestConfigResponse.cs
        │   ├── UpdateProbeStatusResponse.cs
        │   ├── SetProbeTestEnabledResponse.cs
        │   └── SetPluginAvailabilityResponse.cs
        ├── Types/
        │   ├── ProbeType.cs
        │   ├── ProbeTestConfigType.cs
        │   ├── ProbeConfigType.cs
        │   ├── PluginType.cs
        │   ├── ProbeStatusType.cs
        │   ├── RegisterProbeInputType.cs
        │   ├── RegisterPluginInputType.cs
        │   ├── UpdateProbeTestConfigInputType.cs
        │   ├── SetProbeTestEnabledInputType.cs
        │   └── SetPluginAvailabilityInputType.cs
        ├── Errors/
        │   └── ErrorHandler.cs   # Error handling
        └── Middleware/
            └── ExceptionMiddleware.cs
```

## Key Design Decisions

### 1. Onion Architecture
- Aligns with the existing Java implementation's Clean Architecture
- Maintains port-adapter pattern for data access
- Enables testing and flexibility

### 2. Entity Framework Core
- Replaces Hibernate/Spring Data JPA
- LINQ-based queries match expressiveness of Spring Data
- EF Core migrations for database versioning

### 3. HotChocolate for GraphQL
- Per ADR-008: chosen for .NET ecosystem integration
- Code-first schema definition maintains strong typing
- Seamless ASP.NET Core middleware integration

### 4. Value Objects in Domain
- `ProbeId`, `TestType` encapsulate business logic
- Prevent invalid states at construction time
- Enable compile-time type safety

### 5. Use Cases as First-Class Citizens
- Each use case is a dedicated class
- Injected with only required dependencies
- Easy to understand and modify independently

## Domain Models

### Probe (Aggregate Root)
Represents a Raspberry Pi device in the BEACON network.
- **Properties**: Id, Name, Location, IpAddress, Status, CreatedAt, LastHeartbeat, LastConfigFetch
- **Behaviors**: UpdateStatus, RecordHeartbeat, RecordConfigFetch
- **Invariants**: Id/IP uniqueness, status validity

### TestType (Value Object)
Represents test modalities (RSSI, PING, HTTP, iPerf).
- **Immutable**: Cannot be modified after creation
- **Validation**: Name and description required

### ProbeTestConfiguration (Value Object)
Maps enabled tests to probes with intervals.
- **Immutable**: Creates new instances with modifications
- **Validation**: Interval bounds (5-3600 seconds)

### Plugin (Aggregate Root)
Metadata for plugins distributed to probes.
- **Properties**: Id, Name, Version, Checksum, Description, ReleasedAt, Available
- **Behaviors**: Retire, Restore

## Use Cases

1. **RegisterProbeUseCase** - Register new probe
2. **GetFleetStatusUseCase** - Retrieve all probe statuses
3. **GetProbeConfigUseCase** - Fetch probe test configuration
4. **UpdateProbeTestConfigUseCase** - Create/update probe test config and interval
5. **UpdateProbeStatusUseCase** - Change probe status
6. **ListPluginsUseCase** - List all plugins (enabled and disabled)
7. **RegisterPluginUseCase** - Register plugin metadata/version
8. **GetPluginByIdUseCase** - Fetch one plugin by ID
9. **SetProbeTestEnabledUseCase** - Toggle enabled state of existing probe test config
10. **SetPluginAvailabilityUseCase** - Toggle plugin availability for distribution

## GraphQL API Contract

**Queries:**
- `fleetStatus: FleetStatusResponse!`
- `probeConfig(probeId: String!): ProbeConfigType!`
- `plugins: [PluginType!]!`
- `plugin(id: String!): PluginType`

**Mutations:**
- `registerProbe(input: RegisterProbeInputType!): RegisterProbeResponse!`
- `updateProbeTestConfig(input: UpdateProbeTestConfigInputType!): UpdateProbeTestConfigResponse!`
- `updateProbeStatus(probeId: String!, status: String!): UpdateProbeStatusResponse!`
- `registerPlugin(input: RegisterPluginInputType!): RegisterPluginResponse!`
- `setProbeTestEnabled(input: SetProbeTestEnabledInputType!): SetProbeTestEnabledResponse!`
- `setPluginAvailability(input: SetPluginAvailabilityInputType!): SetPluginAvailabilityResponse!`

**Note**: GraphQL contract is code-first in HotChocolate and may evolve with additional admin operations while preserving core probe workflows.

## Database Schema

### Tables
- `probes` - Probe device registry
- `test_types` - Available test modalities (seeded)
- `probe_test_configurations` - Test configuration mappings
- `plugins` - Plugin metadata

### Indexes
- `idx_probes_status` - Query by status
- `idx_probes_created_at` - Sort by creation
- `idx_probe_config_probe_enabled` - Filter enabled configs
- `idx_plugins_name_version` - Unique plugin versions

## Migration Path

### From Java/Spring Boot → .NET/C#

**What Changed:**
- Language: Java → C#
- Framework: Spring Boot → ASP.NET Core
- GraphQL: Spring GraphQL → HotChocolate
- ORM: JPA/Hibernate → Entity Framework Core
- Build: Maven → dotnet CLI

**What Stayed the Same:**
- Domain logic and business rules
- GraphQL schema and API contract
- Database schema (PostgreSQL)
- Architecture patterns (Onion)
- Use cases and workflows

## Development Workflow

### Add a New Feature

1. **Domain**: Define domain model/value object
2. **Application**: Create use case calling domain logic
3. **Infrastructure**: Implement repository if needed
4. **Presentation**: Add GraphQL query/mutation
5. **Tests**: Add tests at each layer (when test project is created)

### Entity ↔ Domain Mapping

**Pattern**: Repositories map between Entity (EF) and Domain models
```csharp
// To domain
var probe = new Probe(
    new ProbeId(entity.Id),
    entity.Name,
    entity.Location,
    entity.IpAddress
);

// To entity
var entity = new ProbeEntity
{
    Id = probe.Id.Value,
    Name = probe.Name,
    // ...
};
```

## Performance Considerations

1. **Native AOT**: Publishable as native executable for minimal memory footprint
2. **LINQ Compilation**: EF Core queries compile to SQL
3. **Async/Await**: All I/O operations are async
4. **Caching**: Consider Redis for frequently accessed entities
5. **Batch Operations**: EF Core supports batch updates/deletes

## Security

- **Input Validation**: Use FluentValidation for DTO validation
- **SQL Injection**: EF Core parameterized queries prevent SQL injection
- **Authorization**: Add authorization middleware in Program.cs
- **CORS**: Configured in Program.cs for cross-origin requests

## Monitoring & Observability

1. **OpenTelemetry**: Instrumentation available via ASP.NET Core
2. **Logging**: Structured logging via Microsoft.Extensions.Logging
3. **Metrics**: Can integrate with Prometheus via /metrics endpoint
4. **Tracing**: GraphQL query tracing available via HotChocolate middleware

## Testing Strategy (Future)

When adding tests:
- **Domain Tests**: Unit tests for models and value objects
- **Use Case Tests**: Integration tests with mock repositories
- **GraphQL Tests**: Schema and resolver tests
- **Integration Tests**: End-to-end with test database

## Deployment

### Docker
```bash
docker build -t beacon-central-server .
docker run -e ConnectionStrings__DefaultConnection="..." beacon-central-server
```

### Native AOT (Zero-startup container)
```bash
dotnet publish -c Release -p PublishAot=true
```

### Kubernetes
Ready for Kubernetes deployment with:
- Liveness probe: `/health`
- Readiness probe: GraphQL endpoint
- Environment variable configuration

## References

- **ADR-007**: Use .NET 9 with C# for the central server
- **ADR-008**: Use HotChocolate for GraphQL
- **Original Java Implementation**: `/code/central-server/`
