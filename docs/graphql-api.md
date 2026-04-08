# BEACON GraphQL API Documentation

## Overview
The BEACON Central Server exposes a GraphQL API for:
- Admin operations (fleet visibility, probe lifecycle, test configuration, plugin management)
- Probe agent configuration fetch (`probeConfig`)

## Endpoints
- GraphQL endpoint: `http://localhost:5000/graphql`
- GraphQL IDE (development): `http://localhost:5000/graphql`
- Health check: `http://localhost:5000/health`
- Plugin bundle download: `http://localhost:5000/plugins/{pluginId}/{version}/bundle`

## Main Types

### Probe
- `id: String!`
- `name: String!`
- `location: String!`
- `ipAddress: String!`
- `status: ProbeStatusType!`
- `createdAt: DateTime!`
- `lastHeartbeat: DateTime`
- `lastConfigFetch: DateTime`

### ProbeTestConfig
- `probeId: String!`
- `testType: String!`
- `intervalSeconds: Int!`
- `enabled: Boolean!`

### ProbeConfig
- `probeId: String!`
- `enabledTests: [ProbeTestConfig!]!`
- `availablePlugins: [Plugin!]!`

### Plugin
- `id: String!`
- `name: String!`
- `version: String!`
- `checksum: String!`
- `description: String`
- `releasedAt: DateTime!`
- `available: Boolean!`
- `bundleUrl: String!`

## Queries
- `fleetStatus: FleetStatusResponse!`
- `probeConfig(probeId: String!): ProbeConfig!`
- `plugins: [Plugin!]!`
- `plugin(id: String!): Plugin`

## Mutations
- `registerProbe(input: ...): RegisterProbeResponse!`
- `updateProbeTestConfig(input: ...): UpdateProbeTestConfigResponse!`
- `setProbeTestEnabled(input: ...): SetProbeTestEnabledResponse!`
- `updateProbeStatus(probeId: String!, status: String!): UpdateProbeStatusResponse!`
- `registerPlugin(input: ...): RegisterPluginResponse!`
- `setPluginAvailability(input: ...): SetPluginAvailabilityResponse!`

## Example Operations

### Connectivity Check
```graphql
query {
  __typename
}
```

### Fleet Status
```graphql
query {
  fleetStatus {
    probes {
      id
      name
      location
      ipAddress
      status
      createdAt
      lastHeartbeat
      lastConfigFetch
    }
  }
}
```

### Register Probe
```graphql
mutation {
  registerProbe(input: {
    id: "probe-001"
    name: "Main Building Floor 1"
    location: "Room 101"
    ipAddress: "192.168.1.100"
  }) {
    success
    message
    probe {
      id
      status
      createdAt
    }
  }
}
```

### Configure Probe Test (Interval + Enabled)
```graphql
mutation {
  updateProbeTestConfig(input: {
    probeId: "probe-001"
    testType: "PING"
    intervalSeconds: 30
    enabled: true
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

### Toggle Probe Test (Enabled Only)
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

### Probe Config Fetch (Agent Polling)
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
      name
      version
      checksum
      bundleUrl
    }
  }
}
```

### Register Plugin
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
    message
    plugin {
      id
      name
      version
      available
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

### Download Plugin Bundle
```bash
curl -L http://localhost:5000/plugins/plugin-http-v2/2.1.0/bundle -o plugin-http-v2-2.1.0.zip
```

## Typical Validation Flow
1. Run `__typename` to verify endpoint health.
2. Register one probe.
3. Configure one test with `updateProbeTestConfig`.
4. Toggle it with `setProbeTestEnabled`.
5. Fetch `probeConfig(probeId)` to validate enabled tests and available plugins.
6. Register a plugin, then toggle availability with `setPluginAvailability`.
7. Download a plugin bundle from `/plugins/{pluginId}/{version}/bundle`.

## Notes
- `probeConfig` returns enabled tests and available plugins for probe-side plugin resolution.
- `plugins` returns all plugin records, including disabled ones, to support admin toggling.
- `DateTime` values are serialized as ISO-8601 timestamps.
