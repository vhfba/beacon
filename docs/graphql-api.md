# BEACON Central API Documentation (GraphQL + Operational REST)

## Overview
The BEACON Central Server exposes:

- GraphQL for core admin and probe configuration workflows.
- REST endpoints for operational monitoring, Prometheus service discovery, probe runtime controls, and plugin bundle download.

This hybrid API keeps schema-driven business operations in GraphQL while using REST where external systems (Prometheus/Grafana/probe runtime agents) need simple endpoint semantics.

## Base URLs

- Simulator root: `http://localhost:5000/`
- GraphQL endpoint/IDE: `http://localhost:5000/graphql`
- Health check: `http://localhost:5000/health`

## Authentication

- Protected APIs require header: `X-Api-Key: <key>`
- Roles:
  - Admin key (`AUTH_ADMIN_API_KEY`): full admin + probe operations.
  - Probe key (`AUTH_PROBE_API_KEY`): probe operations + bundle download.
  - Prometheus service discovery token (`PROMETHEUS_SD_TOKEN`): used on service discovery endpoint (`token` query param or `X-Service-Discovery-Token` header).

## GraphQL API

### Core Types

#### Probe
- `id: String!`
- `name: String!`
- `location: String!`
- `ipAddress: String!` (host or host:port; service discovery resolves target endpoint)
- `status: ProbeStatusType!`
- `createdAt: DateTime!`
- `lastHeartbeat: DateTime`
- `lastConfigFetch: DateTime`

#### ProbeConfig
- `probeId: String!`
- `enabledTests: [ProbeTestConfig!]!`
- `availablePlugins: [Plugin!]!`

#### Plugin
- `id: String!`
- `name: String!`
- `version: String!`
- `checksum: String!`
- `description: String`
- `releasedAt: DateTime!`
- `available: Boolean!`
- `bundleUrl: String!`

### Queries
- `fleetStatus: FleetStatusResponse!`
- `probeConfig(probeId: String!): ProbeConfig!`
- `plugins: [Plugin!]!`
- `plugin(id: String!): Plugin`

### Mutations
- `registerProbe(input: RegisterProbeInput!): RegisterProbeResponse!`
- `updateProbeTestConfig(input: UpdateProbeTestConfigInput!): UpdateProbeTestConfigResponse!`
- `setProbeTestEnabled(input: SetProbeTestEnabledInput!): SetProbeTestEnabledResponse!`
- `updateProbeStatus(probeId: String!, status: String!): UpdateProbeStatusResponse!`
- `registerPlugin(input: RegisterPluginInput!): RegisterPluginResponse!`
- `setPluginAvailability(input: SetPluginAvailabilityInput!): SetPluginAvailabilityResponse!`

### GraphQL Example (fleet)
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

### GraphQL Example (probe config)
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
      bundleUrl
    }
  }
}
```

## Operational REST API

### Monitoring / Prometheus

#### GET `/monitoring/prometheus/service-discovery`
Returns Prometheus HTTP SD targets for ACTIVE probes only.

- Auth: service discovery token
- Query or header:
  - `?token=<PROMETHEUS_SD_TOKEN>`
  - or `X-Service-Discovery-Token: <PROMETHEUS_SD_TOKEN>`

Example:
```bash
curl "http://localhost:5000/monitoring/prometheus/service-discovery?token=dev-prom-sd-token"
```

#### GET `/monitoring/thresholds/{site}`
Read Wi-Fi threshold profile for a site.

- Auth: Admin

#### PUT `/monitoring/thresholds/{site}`
Save threshold profile and apply Grafana sync/fallback.

- Auth: Admin

#### POST `/monitoring/grafana/embed-session`
Create embed session metadata and return dashboard embed URL.

- Auth: Admin

Request body:
```json
{ "site": "building-a" }
```

### Probe Runtime

#### GET `/probes/{probeId}/runtime-state`
Probe polls this endpoint to decide if it can emit metrics and which tests are enabled.

- Auth: Probe or Admin

Response includes:
- `status` (`REGISTERED|ACTIVE|INACTIVE|DECOMMISSIONED`)
- `canEmitMetrics` (bool)
- `enabledTests` (array)

#### POST `/probes/{probeId}/heartbeat`
Probe sends periodic heartbeat to update liveness.

- Auth: Probe or Admin
- Decommissioned probes are rejected with conflict.

### Plugin Distribution

#### GET `/plugins/{pluginId}/{version}/bundle`
Download zip bundle for available plugin version.

- Auth: Probe or Admin

Example:
```bash
curl -L \
  -H "X-Api-Key: change-this-probe-api-key" \
  "http://localhost:5000/plugins/plugin-http-v2/2.1.0/bundle" \
  -o plugin-http-v2-2.1.0.zip
```

## Probe Registration Note

The simulator UI now captures host and metrics port separately and stores `ipAddress` as host:port when registering probes, so Prometheus can scrape per-probe ports correctly.

## Typical End-to-End Validation Flow

1. Register probe (`registerProbe`) with host/port.
2. Configure tests (`updateProbeTestConfig`, `setProbeTestEnabled`).
3. Start probe agent with probe key and central base URL.
4. Probe polls `/probes/{probeId}/runtime-state` and sends `/probes/{probeId}/heartbeat`.
5. Validate `fleetStatus` shows updated `lastHeartbeat` and status.
6. Validate service discovery returns only ACTIVE probes.
7. Open Grafana embed from Monitoring Hub and confirm time series.

## Notes

- GraphQL remains the control-plane API for business operations.
- REST endpoints cover integration boundaries (Prometheus/Grafana/probe runtime).
- Date/time values are ISO-8601 timestamps.
