# ADR-009: Use REST endpoints for operational monitoring and probe runtime control

|Metadata|Value|
|--------|-----|
|Date|[2026-04-11]|
|Status|Accepted|
|Depends on|ADR-003, ADR-007, ADR-008|
|Tags|beacon, api, rest, monitoring, prometheus, grafana, probe-runtime|

## Context
GraphQL remains BEACON's primary control-plane API for admin workflows and probe configuration queries. However, several integrations are operational by nature and fit REST semantics better:

- Prometheus HTTP service discovery (`/monitoring/prometheus/service-discovery`)
- Grafana embed/session orchestration (`/monitoring/grafana/embed-session`)
- Site threshold profile read/write (`/monitoring/thresholds/{site}`)
- Probe runtime polling and heartbeat (`/probes/{probeId}/runtime-state`, `/probes/{probeId}/heartbeat`)
- Binary bundle download (`/plugins/{pluginId}/{version}/bundle`)

These interfaces are consumed by systems and agents that prefer stable URL/resource contracts and simple request/response payloads.

## Decision
Adopt a hybrid API model:

- Keep GraphQL for domain business operations and UI control-plane interactions.
- Use REST endpoints for operational integrations with Prometheus, Grafana, and probe runtime agents.
- Enforce role-based authentication consistently across GraphQL and REST using the same API-key policies.
- Place REST endpoint mapping in the Presentation layer endpoint modules to preserve Onion boundaries.

## Alternatives considered

### 1. Keep everything in GraphQL
How it would work: expose operational functions as GraphQL fields/mutations only.

Why it was rejected: external systems like Prometheus and binary download clients require plain HTTP endpoint semantics and do not benefit from GraphQL tooling.

### 2. Move everything to REST
How it would work: deprecate GraphQL and replace with many resource endpoints.

Why it was rejected: conflicts with ADR-003 and would degrade schema-driven admin workflows and UI query flexibility.

### 3. Separate operational sidecar service
How it would work: create a second service for operational endpoints.

Why it was rejected: unnecessary deployment and auth complexity for current scale.

## Consequences

### Positive
- Better interoperability with Prometheus/Grafana and lightweight agents.
- Cleaner contracts for runtime endpoints and binary download.
- Reduced accidental coupling between GraphQL schema evolution and operations interfaces.
- Presentation-layer endpoint modules keep Program.cs as composition root.

### Negative
- Two API styles increase documentation and testing surface.
- Need to maintain consistent auth semantics across both API styles.

## Related decisions
- ADR-003 (GraphQL for central configuration API)
- ADR-007 (.NET 9 / C# central server)
- ADR-008 (HotChocolate GraphQL in .NET)
