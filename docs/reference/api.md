# BEACON API Reference

The BEACON central server exposes:

- a protected GraphQL endpoint for admin and probe workflows
- a Prometheus-compatible `/metrics` endpoint
- a Grafana embed-session endpoint for the monitoring UI
- a protected plugin bundle download endpoint for probe agents

Related contracts:

- Metrics: [metrics.md](./metrics.md)
- Plugin bundles and output: [plugin-contract.md](./plugin-contract.md)
- Local deployment: [local-deployment.md](../operations/local-deployment.md)

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
- `ProbeCoverageSummary`
  Central-server computed coverage quality summary built from the latest pushed metric snapshot.
- `Plugin`
  Distributed plugin metadata, including execution mode, bundle information, and optional Grafana dashboard metadata.
- `ProbePluginAssignment`
  Mapping between a probe and its assigned plugins.
- `ProbeActionExecution`
  Queued or completed on-demand action execution.
- `ProbeControlCommand`
  Queued or completed device-control command for profile and Wi-Fi administration.

### Enums

- `ProbeStatusType`: `REGISTERED`, `ACTIVE`, `INACTIVE`, `DECOMMISSIONED`
- `PluginExecutionModeType`: `SCHEDULED`, `ACTION`
- `ProbeActionExecutionStatusType`: `QUEUED`, `DELIVERED`, `RUNNING`, `SUCCEEDED`, `FAILED`, `TIMED_OUT`
- `ProbeControlCommandTypeType`: `SCAN_WIFI_NETWORKS`, `CONNECT_WIFI`, `UPDATE_PROFILE`
- `ProbeControlCommandStatusType`: `QUEUED`, `DELIVERED`, `RUNNING`, `SUCCEEDED`, `FAILED`, `TIMED_OUT`

## Queries

### Admin queries

- `fleetStatus`
- `fleetCoverage`
- `plugins`
- `plugin(id: String!)`
- `probePluginAssignments(probeId: String!)`
- `probeActionExecutions(probeId: String!, limit: Int = 50)`
- `probeControlCommands(probeId: String!, limit: Int = 50)`

### Probe-facing queries

- `probeConfig(probeId: String!)`
- `probeRuntime(probeId: String!)`
- `pendingProbeActions(probeId: String!, limit: Int = 10)`
- `pendingProbeControlCommands(probeId: String!, limit: Int = 10)`

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

### Example: fleet coverage

```graphql
query {
  fleetCoverage {
    probeId
    site
    score
    grade
    rssiDbm
    snrDb
    linkQualityPercent
    pingLatencyMs
    pingPacketLossPercent
    sampleCount
    receivedAtUtc
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
      hasDashboard
      dashboardUid
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
- `updateProbeProfile`
- `requestWifiScan`
- `requestWifiConnect`

### Plugin administration

- `registerPlugin`
- `setPluginAvailability`
- `deletePlugin`

### Runtime and action flow

- `recordProbeHeartbeat`
- `reportProbeMetrics`
- `triggerProbeAction`
- `updateProbeActionStatus`
- `updateProbeControlCommandStatus`

Probe lifecycle note:

- probes self-register on their first successful `recordProbeHeartbeat`
- admins no longer create probe records manually
- probe IDs remain stable; admins edit the display name/location through `updateProbeProfile`
- heartbeat updates observed runtime fields such as IP address, SSID, agent version, and last-seen timestamps, but does not overwrite admin-managed name/location

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
      hasDashboard
      dashboardUid
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

### Example: update probe profile

```graphql
mutation UpdateProfile($input: UpdateProbeProfileInputTypeInput!) {
  updateProbeProfile(input: $input) {
    success
    probe {
      id
      name
      location
    }
    command {
      commandId
      type
      status
    }
  }
}
```

### Example: request Wi-Fi scan

```graphql
mutation ScanWifi($input: RequestWifiScanInputTypeInput!) {
  requestWifiScan(input: $input) {
    success
    command {
      commandId
      status
    }
  }
}
```

### Example: request Wi-Fi connection

```graphql
mutation ConnectWifi($input: RequestWifiConnectInputTypeInput!) {
  requestWifiConnect(input: $input) {
    success
    command {
      commandId
      payloadJson
    }
  }
}
```

Notes:

- `requestWifiConnect` sends the password only in the pending command payload consumed by the probe.
- Admin command history redacts Wi-Fi passwords in `payloadJson`.

### Example: probe claims control commands

```graphql
query PendingControl($probeId: String!) {
  pendingProbeControlCommands(probeId: $probeId, limit: 10) {
    commandId
    type
    payloadJson
  }
}
```

### Example: probe reports control command result

```graphql
mutation UpdateControl($input: UpdateProbeControlCommandStatusInputTypeInput!) {
  updateProbeControlCommandStatus(input: $input) {
    success
    command {
      commandId
      status
      resultJson
      errorMessage
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

Returns the dashboard embed target for a selected Grafana dashboard and site.

- Auth: admin API key
- Body:

```json
{ "site": "building-a", "dashboardUid": "beacon-plugin-ping" }
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

### `GET /monitoring/grafana/dashboards`

Returns the Grafana dashboards available for the simulator monitoring selector.

- Auth: admin API key

Response items:

- `uid`
- `title`
- `url`

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
4. Admin enables scheduled execution with `updateProbeTestConfig`.
5. Probe reads `probeConfig` and `pendingProbeActions`.
6. Probe runs scheduled plugins and pushes metrics with `reportProbeMetrics`.
7. Central-server computes coverage summaries from the latest snapshots.
8. Prometheus scrapes `/metrics`.
9. Grafana reads Prometheus and the simulator UI reads `fleetCoverage`.

Coverage scoring note:

- central-server computes the `fleetCoverage` score from the latest pushed snapshot
- score is clamped to `0..100`
- grades are `EXCELLENT`, `GOOD`, `WEAK`, `UNUSABLE`, or `NO_DATA`

### On-demand action flow

1. Admin registers a plugin with `executionMode: ACTION`.
2. Probe boots and self-registers through `recordProbeHeartbeat`.
3. Admin assigns it to a probe.
4. Admin queues work with `triggerProbeAction`.
5. Probe polls `pendingProbeActions`.
6. Probe executes the action plugin.
7. Probe posts status changes with `updateProbeActionStatus`.
8. Admin reviews history with `probeActionExecutions`.

### Probe control flow

1. Admin edits profile or requests Wi-Fi scan/connect from the simulator UI.
2. Central-server stores a `ProbeControlCommand`.
3. Probe polls `pendingProbeControlCommands`.
4. Probe applies the command locally and reports `RUNNING`, then `SUCCEEDED` or `FAILED`.
5. Admin reviews history with `probeControlCommands`.

## Implementation Notes

- GraphQL introspection is disabled by default in `appsettings.json`.
- Request hardening enforces depth and complexity limits.
- The checked-in `GraphQL.schema.graphql` is generated from the live HotChocolate SDL and should be refreshed whenever resolver-visible schema behavior changes.
