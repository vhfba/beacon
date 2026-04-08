# BEACON Central Server (.NET 9)

## Overview

Central Server is a production-ready backend service for the **BEACON** platform - a distributed network monitoring and diagnostic system built on Raspberry Pi probes deployed across buildings.

The system is responsible for:
- **Coordinating** data from external probe agents
- **Managing** probe configuration, tests, and results
- **Exposing** a GraphQL API for Admin tooling and probe agents
- **Distributing** plugins for probe test execution

## Architecture

### Stack
- **.NET 9** - Latest LTS runtime
- **ASP.NET Core** - Web framework
- **HotChocolate** - GraphQL API layer (ADR-008)
- **PostgreSQL** - Relational database
- **Entity Framework Core** - ORM
- **C# 13** - Language

### Design Pattern: Onion Architecture (Clean Architecture)

```
┌─────────────────────────────────────────────────┐
│           Presentation Layer                     │
│  (GraphQL: Types, Queries, Mutations)           │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│         Application Layer                        │
│  (Use Cases, DTOs, Service Orchestration)       │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Infrastructure Layer                           │
│  (EF Core, Entities, Repository Adapters)      │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│         Domain Layer (PURE)                      │
│  (Models, Value Objects, Repository Interfaces)│
└─────────────────────────────────────────────────┘
```

**Key Principles:**
- Domain layer is pure C# - no EF Core, no ASP.NET, no framework dependencies
- Dependencies always point inward
- Repository interfaces defined in domain, implemented in infrastructure
- Use cases orchestrate pure domain logic
- GraphQL resolvers only call use cases (never bypass to repositories)
- DTOs separate API contracts from domain models

## Project Structure

```
CentralServer/
├── CentralServer.csproj               # Project file
├── appsettings.json                   # Configuration
├── Program.cs                         # Entry point
├── Domain/
│   ├── Models/
│   │   ├── Probe.cs                   # Probe aggregate root
│   │   ├── ProbeId.cs                 # Value object
│   │   ├── ProbeStatus.cs             # Enum
│   │   ├── TestType.cs                # Value object
│   │   ├── ProbeTestConfiguration.cs  # Value object
│   │   ├── Plugin.cs                  # Plugin aggregate root
│   │   └── DomainException.cs         # Base domain exception
│   └── Repositories/
│       ├── IProbeRepository.cs        # Port interface
│       ├── ITestTypeRepository.cs     # Port interface
│       ├── IProbeTestConfigurationRepository.cs
│       └── IPluginRepository.cs
├── Application/
│   ├── DTOs/
│   │   ├── RegisterProbeInput.cs
│   │   ├── ProbeDTO.cs
│   │   ├── ProbeTestConfigurationDTO.cs
│   │   ├── PluginDTO.cs
│   │   ├── ProbeConfigResponseDTO.cs
│   │   ├── UpdateProbeTestConfigInput.cs
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
├── Infrastructure/
│   ├── Persistence/
│   │   ├── Entities/
│   │   │   ├── ProbeEntity.cs
│   │   │   ├── TestTypeEntity.cs
│   │   │   ├── ProbeTestConfigEntity.cs
│   │   │   └── PluginEntity.cs
│   │   ├── Migrations/
│   │   ├── CentralServerDbContext.cs
│   │   └── Repositories/
│   │       ├── ProbeRepositoryAdapter.cs
│   │       ├── TestTypeRepositoryAdapter.cs
│   │       ├── ProbeTestConfigurationRepositoryAdapter.cs
│   │       └── PluginRepositoryAdapter.cs
│   └── Configuration/
│       └── ServiceConfiguration.cs
└── Presentation/
    ├── GraphQL/
    │   ├── Types/
    │   │   ├── ProbeType.cs
    │   │   ├── PluginType.cs
    │   │   ├── ProbeTestConfigType.cs
    │   │   └── ProbeConfigType.cs
    │   ├── Queries.cs
    │   ├── Mutations.cs
    │   └── Errors/
    │       └── ErrorHandler.cs
    └── Middleware/
        └── ExceptionMiddleware.cs
```

## Getting Started

### Prerequisites
- .NET 9 SDK
- PostgreSQL 14+
- Docker (optional, for containerization)

### Development

1. **Install dependencies:**
   ```bash
   dotnet restore
   ```

2. **Configure database:**
   Update `appsettings.json` with your PostgreSQL connection string.

3. **Run migrations:**
   ```bash
   dotnet ef database update
   ```

4. **Start the server:**
   ```bash
   dotnet run
   ```

   The GraphQL endpoint is available at `http://localhost:5000/graphql`

### Docker

Build and run the container:
```bash
docker build -t beacon-central-server .
docker run -p 5000:8080 -e ConnectionString="Host=host.docker.internal;..." beacon-central-server
```

## Use Cases

The server implements the following use cases:

1. **RegisterProbeUseCase** - Register a new Raspberry Pi probe device
2. **GetFleetStatusUseCase** - Retrieve the status of all probes
3. **GetProbeConfigUseCase** - Fetch configuration for a specific probe
4. **UpdateProbeTestConfigUseCase** - Create/update test config for a probe
5. **UpdateProbeStatusUseCase** - Update probe status (active, inactive, etc.)
6. **ListPluginsUseCase** - List all registered plugins (available and disabled)
7. **RegisterPluginUseCase** - Register a plugin package/version
8. **GetPluginByIdUseCase** - Retrieve one plugin by ID
9. **SetProbeTestEnabledUseCase** - Enable or disable an existing test config on a probe
10. **SetPluginAvailabilityUseCase** - Enable or disable plugin distribution

## API Examples

### Get Fleet Status
```graphql
query {
  fleetStatus {
    probes {
      id
      name
      location
      status
      lastHeartbeat
    }
  }
}
```

### Register a Probe
```graphql
mutation {
  registerProbe(input: {
    id: "probe-001"
    name: "Building A - Flow 1"
    location: "1st Floor"
    ipAddress: "192.168.1.100"
  }) {
    probe {
      id
      status
    }
  }
}
```

### Get Probe Configuration
```graphql
query {
  probeConfig(probeId: "probe-001") {
    probeId
    enabledTests {
      testType
      intervalSeconds
      enabled
    }
  }
}
```

### Toggle Probe Test Enablement
```graphql
mutation {
  setProbeTestEnabled(input: {
    probeId: "probe-001"
    testType: "PING"
    enabled: false
  }) {
    success
    message
    config {
      probeId
      testType
      intervalSeconds
      enabled
    }
  }
}
```

### Toggle Plugin Availability
```graphql
mutation {
  setPluginAvailability(input: {
    pluginId: "plugin-http-v2"
    available: false
  }) {
    success
    message
    plugin {
      id
      available
    }
  }
}
```

## Architecture Decision Records

- **ADR-007**: Use .NET 9 with C# for the central server
- **ADR-008**: Use HotChocolate GraphQL for the .NET central server
- **ADR-006**: PostgreSQL with Entity Framework Core for configuration storage (updated from ADR-006)

## Related Documentation

See `docs/` directory for:
- GraphQL API schema
- Architecture diagrams
- ADRs (Architecture Decision Records)
