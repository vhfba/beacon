# ADR-012: Use GraphQL for probe runtime, heartbeat, and action workflows

|Metadata|Value|
|--------|-----|
|Date|[2026-05-01]|
|Status|Accepted|
|Depends on|ADR-003, ADR-007, ADR-008|
|Tags|beacon, graphql, probe-runtime, heartbeat, actions|

## Context
An intermediate design introduced REST endpoints for probe runtime state and heartbeat, but the implementation evolved toward a richer probe-side workflow: probe agents now need configuration polling, runtime eligibility, heartbeat, pending action retrieval, metric reporting, and action status updates. These operations are tightly connected to the BEACON domain model and already live naturally beside the existing GraphQL admin operations. Keeping them split across GraphQL and REST created documentation drift and no longer reflected the actual resolver surface in the central-server codebase.

## Decision
BEACON will use GraphQL as the primary control-plane API for both admin and probe runtime workflows.

Probe-facing runtime operations are exposed through GraphQL:

- `probeConfig`
- `probeRuntime`
- `recordProbeHeartbeat`
- `pendingProbeActions`
- `reportProbeMetrics`
- `updateProbeActionStatus`

REST remains only where simple endpoint semantics are the better fit:

- `GET /metrics`
- `POST /monitoring/grafana/embed-session`
- `GET /plugins/{pluginId}/{version}/bundle`

## Alternatives considered

### 1. Keep probe runtime split across GraphQL and REST
How it would work inside BEACON: Admin workflows would remain in GraphQL while probes would use a mix of GraphQL plus dedicated REST runtime endpoints.

Why it was rejected: It creates two control-plane styles for one domain and encourages drift between code, schema, and documentation.

### 2. Move all probe workflows to REST
How it would work inside BEACON: Probe config, actions, heartbeat, and metrics reporting would all become resource-oriented REST endpoints.

Why it was rejected: It works against the graph-shaped domain already modeled in GraphQL and would duplicate authorization and shape evolution effort.

### 3. Use a message broker for pending actions and heartbeat
How it would work inside BEACON: Probes would receive actions and publish status through a broker instead of polling GraphQL.

Why it was rejected: It adds infrastructure and delivery semantics that are unnecessary for the current deployment model and team size.

## Consequences

### Positive
- One typed control-plane API now covers both admin and probe workflows.
- Probe runtime behavior is easier to document and test because it is part of the same resolver surface.
- Action delivery and status updates stay close to the central domain model.
- Security and query hardening can be applied consistently at one API boundary.

### Negative
- Probe clients must stay disciplined about query shape and mutation payload size.
- The GraphQL schema and resolver documentation need active maintenance as runtime features evolve.

## Related decisions
- ADR-003 (GraphQL for the central configuration API)
- ADR-007 (.NET 9 / C# central server)
- ADR-008 (HotChocolate GraphQL in .NET)

## Supersedes
- ADR-009
- Runtime-specific assumptions in ADR-010
