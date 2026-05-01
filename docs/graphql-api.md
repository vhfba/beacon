# BEACON API Reference

The BEACON central server exposes:

- a protected GraphQL endpoint for admin and probe workflows
- a Prometheus-compatible `/metrics` endpoint
- a Grafana embed-session endpoint for the monitoring UI
- a protected plugin bundle download endpoint for probe agents

## Base URLs

- App root: `http://localhost:5000/`
- GraphQL: `http://localhost:5000/graphql`
- Health: `http://localhost:5000/health`
- Metrics: `http://localhost:5000/metrics`

## Authentication

All GraphQL operations and plugin downloads require `X-Api-Key`.

- Admin key: `Auth__AdminApiKey`
- Probe key: `Auth__ProbeApiKey`

Authorization model:

- Admin-only operations manage fleet inventory, plugins, assignments, and action history.
- Probe-or-admin operations cover runtime polling, heartbeat, action pickup, metric reporting, and probe status updates.
- `/metrics` and `/health` are not API-key protected.

## GraphQL Model

### Core object types

- `Probe`
  Registry record for a probe, including lifecycle status and last-seen timestamps.
- `ProbeConfig`
  Probe-facing config payload with enabled scheduled tests and available plugins.
- `ProbeRuntime`
  Runtime eligibility view returned during heartbeat/runtime flows.
- `Plugin`
  Distributed plugin metadata, including execution mode and bundle information.
- `ProbePluginAssignment`
  Mapping between a probe and its assigned plugins.
- `ProbeActionExecution`
  Queued or completed on-demand action execution.

### Enums

- `ProbeStatusType`: `REGISTERED`, `ACTIVE`, `INACTIVE`, `DECOMMISSIONED`
- `PluginExecutionModeType`: `SCHEDULED`, `ACTION`
- `ProbeActionExecutionStatusType`: `QUEUED`, `DELIVERED`, `RUNNING`, `SUCCEEDED`, `FAILED`, `TIMED_OUT`

## Queries

### Admin queries

- `fleetStatus`
- `plugins`
- `plugin(id: String!)`
- `probePluginAssignments(probeId: String!)`
- `probeActionExecutions(probeId: String!, limit: Int = 50)`

### Probe-facing queries

- `probeConfig(probeId: String!)`
- `probeRuntime(probeId: String!)`
- `pendingProbeActions(probeId: String!, limit: Int = 10)`

### Example: fleet view

```graphql
query {
  fleetStatus {
    probes {
      id
      name
      location
      ipAddress
      status
      lastHeartbeat
      lastConfigFetch
    }
  }
}
```

### Example: probe config

```graphql
query ProbeConfig($probeId: String!) {
  probeConfig(probeId: $probeId) {
    probeId
    enabledTests {
      testType
      intervalSeconds
      enabled
    }
    availablePlugins {
      id
      version
      executionMode
      checksum
      bundleUrl
      bundleDownloadUrl
    }
  }
}
```

### Example: pending actions

```graphql
query PendingActions($probeId: String!) {
  pendingProbeActions(probeId: $probeId, limit: 10) {
    executionId
    pluginId
    status
    requestedAtUtc
  }
}
```

## Mutations

### Fleet and probe administration

- `deleteProbe`
- `updateProbeStatus`
- `updateProbeTestConfig`
- `setProbeTestEnabled`
- `setProbePlugins`

### Plugin administration

- `registerPlugin`
- `setPluginAvailability`
- `deletePlugin`

### Runtime and action flow

- `recordProbeHeartbeat`
- `reportProbeMetrics`
- `triggerProbeAction`
- `updateProbeActionStatus`

Probe lifecycle note:

- probes self-register on their first successful `recordProbeHeartbeat`
- admins no longer create probe records manually

### Example: register plugin

```graphql
mutation RegisterPlugin($input: RegisterPluginInputTypeInput!) {
  registerPlugin(input: $input) {
    success
    message
    plugin {
      id
      name
      version
      available
      executionMode
    }
  }
}
```

Example variables:

```json
{
  "input": {
    "id": "PING",
    "name": "Ping Check",
    "version": "1.0.0",
    "checksum": "replace-with-sha256",
    "description": "Scheduled ICMP health check",
    "executionMode": "SCHEDULED"
  }
}
```

### Example: assign plugins to a probe

```graphql
mutation Assign($input: SetProbePluginsInputTypeInput!) {
  setProbePlugins(input: $input) {
    success
    message
    assignments {
      probeId
      pluginId
      pluginVersion
      pluginAvailable
    }
  }
}
```

### Example: heartbeat

```graphql
mutation Heartbeat($input: ProbeHeartbeatInputTypeInput!) {
  recordProbeHeartbeat(input: $input) {
    success
    autoRegistered
    probe {
      id
      status
      lastHeartbeat
    }
    runtime {
      probeId
      status
      canEmitMetrics
    }
  }
}
```

### Example: metric reporting

```graphql
mutation ReportMetrics($input: ReportProbeMetricsInputTypeInput!) {
  reportProbeMetrics(input: $input) {
    success
    probeId
    acceptedSamples
    receivedAtUtc
  }
}
```

### Example: trigger an action

```graphql
mutation Trigger($input: TriggerProbeActionInputTypeInput!) {
  triggerProbeAction(input: $input) {
    success
    message
    execution {
      executionId
      probeId
      pluginId
      status
      requestedAtUtc
    }
  }
}
```

### Example: mark an action complete

```graphql
mutation UpdateAction($input: UpdateProbeActionStatusInputTypeInput!) {
  updateProbeActionStatus(input: $input) {
    success
    message
    execution {
      executionId
      status
      startedAtUtc
      completedAtUtc
      errorMessage
    }
  }
}
```

## Operational HTTP Endpoints

### `GET /health`

Returns a simple health payload:

```json
{ "status": "healthy" }
```

### `GET /metrics`

Returns Prometheus text exposition generated from the latest probe metric snapshots stored by the central server.

Notes:

- probes push metrics through `reportProbeMetrics`
- central-server stores the latest per-probe snapshot
- Prometheus scrapes only the central server, not each probe directly

### `POST /monitoring/grafana/embed-session`

Returns the dashboard embed target for a site.

- Auth: admin API key
- Body:

```json
{ "site": "building-a" }
```

Response fields:

- `site`
- `dashboardUid`
- `embedUrl`
- `grafanaSyncApplied`
- `grafanaSyncMessage`

Notes:

- this endpoint no longer updates Grafana thresholds or clones site dashboards
- Grafana dashboard import only happens during plugin registration when `dashboardJson` is provided

### `GET /plugins/{pluginId}/{version}/bundle`

Downloads a plugin bundle zip from the configured plugin bundle directory.

- Auth: admin or probe API key

Example:

```bash
curl -L ^
  -H "X-Api-Key: change-this-probe-api-key" ^
  "http://localhost:5000/plugins/PING/1.0.0/bundle" ^
  -o PING-1.0.0.zip
```

## Typical End-To-End Flows

### Scheduled monitoring flow

1. Admin registers a plugin with `executionMode: SCHEDULED`.
2. Probe boots and self-registers through `recordProbeHeartbeat`.
3. Admin assigns plugins to a probe with `setProbePlugins`.
4. Admin enables intervals with `updateProbeTestConfig`.
5. Probe reads `probeConfig` and `pendingProbeActions`.
6. Probe runs scheduled plugins and pushes metrics with `reportProbeMetrics`.
7. Prometheus scrapes `/metrics`.
8. Grafana reads Prometheus.

### On-demand action flow

1. Admin registers a plugin with `executionMode: ACTION`.
2. Probe boots and self-registers through `recordProbeHeartbeat`.
3. Admin assigns it to a probe.
4. Admin queues work with `triggerProbeAction`.
5. Probe polls `pendingProbeActions`.
6. Probe executes the action plugin.
7. Probe posts status changes with `updateProbeActionStatus`.
8. Admin reviews history with `probeActionExecutions`.

## Implementation Notes

- GraphQL introspection is disabled by default in `appsettings.json`.
- Request hardening enforces depth and complexity limits.
- The checked-in `GraphQL.schema.graphql` is not yet fully aligned with the live probe runtime operations. Prefer the resolver code and this document until the schema snapshot is refreshed.
