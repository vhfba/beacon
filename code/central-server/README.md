# BEACON Central Server

The central server is the BEACON control plane. It owns probe inventory, plugin metadata, plugin assignment, scheduled test configuration, action queueing, and the aggregated `/metrics` export used by Prometheus. Probes create their own inventory record when they first send a heartbeat.

## Responsibilities

- expose a protected GraphQL API for admin and probe clients
- persist probe, plugin, assignment, and action state in PostgreSQL
- store latest probe metric snapshots in Redis
- serve plugin bundle archives to probe agents
- expose `/metrics` for Prometheus and `/monitoring/grafana/embed-session` for dashboard embedding
- host the local simulator UI at `/beacon-simulator.html`

## Stack

- .NET 9
- ASP.NET Core minimal hosting
- HotChocolate GraphQL
- Entity Framework Core
- PostgreSQL
- Redis

## Architecture

The service follows an onion-style layout:

- `Domain/`
  Business entities, enums, value objects, exceptions, and repository contracts.
- `Application/`
  Use cases, DTOs, and services grouped by feature area such as `Probes`, `Plugins`, `Actions`, and `Monitoring`.
- `Infrastructure/`
  Persistence, metrics storage, and external adapters colocated with the subsystem they configure.
- `Presentation/`
  GraphQL queries, mutations, types, HTTP endpoints, auth, and simulator assets grouped by feature.

The main composition happens in [Program.cs](/C:/Users/joaom/Faculdade/beacon/code/central-server/Program.cs), where the app wires persistence, metrics, GraphQL security, monitoring endpoints, static assets, and plugin bundle delivery.

## Current Use Cases

### Fleet and plugin administration

- `DeleteProbeUseCase`
- `GetFleetStatusUseCase`
- `UpdateProbeStatusUseCase`
- `RegisterPluginUseCase`
- `DeletePluginUseCase`
- `ListPluginsUseCase`
- `GetPluginByIdUseCase`
- `SetPluginAvailabilityUseCase`
- `SetProbePluginsUseCase`
- `GetProbePluginAssignmentsUseCase`

### Probe runtime and execution

- `GetProbeConfigUseCase`
- `GetProbeRuntimeUseCase`
- `RecordProbeHeartbeatUseCase`
- `GetPendingProbeActionsUseCase`
- `TriggerProbeActionUseCase`
- `UpdateProbeActionStatusUseCase`
- `ListProbeActionExecutionsUseCase`
- `UpdateProbeTestConfigUseCase`
- `SetProbeTestEnabledUseCase`

### Monitoring

- `ReportProbeMetricsUseCase`
- `ExportPrometheusMetricsUseCase`

## Runtime Model

### Scheduled plugins

Scheduled plugins are assigned to a probe after it auto-registers and then activated through `updateProbeTestConfig`.

Examples:

- `PING`
- `HTTP`
- `IPERF`
- `WIFI`

### Action plugins

Action plugins are assigned to a probe after it auto-registers and then queued on demand through `triggerProbeAction`.

Example:

- `WIFI_SCAN_ACTION`

## API Surface

Primary endpoints:

- `POST /graphql`
- `GET /metrics`
- `GET /health`
- `POST /monitoring/grafana/embed-session`
- `GET /plugins/{pluginId}/{version}/bundle`

GraphQL is protected by API-key auth and request hardening:

- introspection disabled by default
- query depth limit
- query complexity limit

## Local Development

### Run with Docker

```powershell
docker compose up -d --build
```

### Run with the .NET SDK

```powershell
dotnet restore
dotnet run
```

The app automatically creates or migrates the database on startup.

## Testing

Run the test project:

```powershell
dotnet test tests/CentralServer.Tests/CentralServer.Tests.csproj
```

Current coverage includes:

- domain behavior
- use-case behavior
- GraphQL runtime flows
- health and security integration
- central `/metrics` export

## Plugin Bundles

Bundle files are served from `plugin-bundles/` using the naming convention:

- `<plugin-id>-<plugin-version>.zip`

Example download:

```powershell
curl -L -H "X-Api-Key: <probe-key>" http://localhost:5000/plugins/PING/1.0.0/bundle -o PING-1.0.0.zip
```

## Related Docs

- [Platform overview](../README.md)
- [API reference](../../docs/graphql-api.md)
- [Deployment guide](../../docs/deploy.md)
- [ADR-007](../../docs/adr/ADR-007-use-dotnet-9-with-csharp-for-the-central-server.md)
- [ADR-008](../../docs/adr/ADR-008-use-hotchocolate-graphql-for-dotnet.md)
