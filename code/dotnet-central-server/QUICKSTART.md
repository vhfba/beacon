# BEACON Central Server - .NET 9 API

## Quick Start

### Prerequisites
- .NET 9 SDK
- PostgreSQL 14+
- Docker & Docker Compose (optional)

### Local Development

1. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

2. **Set up database:**
   ```bash
   # Using Docker Compose
   docker-compose up -d postgres

   # Or configure connection string in appsettings.Development.json
   ```

3. **Run migrations:**
   ```bash
   dotnet ef database update
   ```

4. **Start the server:**
   ```bash
   dotnet run
   ```

5. **Access GraphQL:**
   - GraphQL Endpoint: http://localhost:5000/graphql
   - GraphiQL (dev): http://localhost:5000/graphql

### Docker Deployment

```bash
# Build and run with Docker Compose
docker-compose up --build

# Or build standalone
docker build -t beacon-central-server .
docker run -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="Host=db;..." \
  beacon-central-server
```

### Plugin Bundle Distribution

Put plugin bundles in `plugin-bundles/` using:

- `<plugin-id>-<plugin-version>.zip`

Probe agents can download bundles from:

- `http://localhost:5000/plugins/{pluginId}/{version}/bundle`

## Project Structure

Following **Onion Architecture**:
- `Domain/` - Pure business logic, domain models, repository contracts
- `Application/` - Use cases, DTOs, orchestration
- `Infrastructure/` - EF Core, data access, external integrations
- `Presentation/` - GraphQL types, resolvers

## GraphQL API

### Queries

**Get Fleet Status**
```graphql
query {
  fleetStatus {
    probes {
      id
      name
      status
      lastHeartbeat
    }
  }
}
```

**Get Probe Config** (called by probes)
```graphql
query {
  probeConfig(probeId: "probe-001") {
    probeId
    enabledTests {
      testType
      intervalSeconds
      enabled
    }
    availablePlugins {
      id
      version
      checksum
      bundleUrl
    }
  }
}
```

**List Plugins**
```graphql
query {
  plugins {
    id
    name
    version
    available
  }
}
```

**Get Plugin By ID**
```graphql
query {
  plugin(id: "plugin-http-v2") {
    id
    name
    version
    available
  }
}
```

### Mutations

**Register Plugin**
```graphql
mutation {
  registerPlugin(input: {
    id: "plugin-http-v2"
    name: "HTTP Probe"
    version: "2.1.0"
    checksum: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
    description: "HTTP endpoint reachability checks"
  }) {
    success
    plugin {
      id
      name
      version
      available
    }
  }
}
```

**Register Probe**
```graphql
mutation {
  registerProbe(input: {
    id: "probe-001"
    name: "Building A"
    location: "1st Floor"
    ipAddress: "192.168.1.100"
  }) {
    success
    probe {
      id
      status
    }
  }
}
```

**Update Test Config**
```graphql
mutation {
  updateProbeTestConfig(input: {
    probeId: "probe-001"
    testType: "PING"
    intervalSeconds: 60
    enabled: true
  }) {
    success
    config {
      probeId
      testType
      intervalSeconds
    }
  }
}
```

**Enable/Disable Existing Test Config**
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
      enabled
      intervalSeconds
    }
  }
}
```

**Enable/Disable Plugin Availability**
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

**Update Probe Status**
```graphql
mutation {
  updateProbeStatus(probeId: "probe-001", status: "ACTIVE") {
    success
    probe {
      id
      status
    }
  }
}
```

## Database

PostgreSQL-based persistence with EF Core migrations.

**Tables:**
- `probes` - Registered Raspberry Pi devices
- `test_types` - Available test modalities (RSSI, PING, HTTP, IPERF)
- `probe_test_configurations` - Test configuration mappings
- `plugins` - Plugin metadata and versions

**Seeded Data:**
- Standard test types (RSSI, PING, HTTP, IPERF)

## Development

### Add a New Migration

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Run Tests (when added)

```bash
dotnet test
```

### Publish for Production

```bash
dotnet publish -c Release -o ./publish/
```

For native AOT:
```bash
dotnet publish -c Release --self-contained -p PublishAot=true
```

## Configuration

**appsettings.json** - Database connection and GraphQL settings
**appsettings.Development.json** - Development-specific overrides
**Docker Compose** - Local PostgreSQL for development

## Health Check

```bash
curl http://localhost:5000/health
```

Response:
```json
{ "status": "healthy" }
```

## Related Documentation

- [ADR-007: Use .NET 9 with C#](../../docs/adr/ADR-007-use-dotnet-9-with-csharp-for-the-central-server.md)
- [ADR-008: Use HotChocolate for GraphQL](../../docs/adr/ADR-008-use-hotchocolate-graphql-for-dotnet.md)
- [GraphQL API Documentation](../../docs/graphql-api.md)
