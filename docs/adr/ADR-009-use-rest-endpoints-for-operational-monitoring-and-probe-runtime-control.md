# ADR-009: Use REST endpoints for operational monitoring and probe runtime control

|Metadata|Value|
|--------|-----|
|Date|[2026-04-11]|
|Status|Superseded by ADR-012|
|Depends on|ADR-003, ADR-007, ADR-008|
|Tags|beacon, api, rest, monitoring, prometheus, grafana, probe-runtime|

## Context
GraphQL remains BEACON's primary control-plane API for admin workflows and probe configuration queries. However, several integrations are operational by nature and fit REST semantics better:

- Prometheus HTTP service discovery
- Grafana embed/session orchestration
- Site threshold profile read/write
- Probe runtime polling and heartbeat
- Binary bundle download

These interfaces are consumed by systems and agents that prefer stable URL/resource contracts and simple request/response payloads.

## Decision
Adopt a hybrid API model:

- Keep GraphQL for domain business operations and UI control-plane interactions.
- Use REST endpoints for operational integrations with Prometheus, Grafana, and probe runtime agents.
- Enforce role-based authentication consistently across GraphQL and REST using the same API-key policies.
- Place REST endpoint mapping in the Presentation layer endpoint modules to preserve Onion boundaries.

## Alternatives considered

### 1. Keep everything in GraphQL
How it would work: expose operational functions as GraphQL fields and mutations only.

Why it was rejected: external systems like Prometheus and binary download clients require plain HTTP endpoint semantics and do not benefit from GraphQL tooling.

### 2. Move everything to REST
How it would work: deprecate GraphQL and replace it with many resource endpoints.

Why it was rejected: this conflicts with ADR-003 and would degrade schema-driven admin workflows.

### 3. Separate operational sidecar service
How it would work: create a second service for operational endpoints.

Why it was rejected: unnecessary deployment and auth complexity for the current scale.

## Consequences

### Positive
- Better interoperability with Prometheus, Grafana, and lightweight agents.
- Cleaner contracts for runtime endpoints and binary download.

### Negative
- Two API styles increase documentation and testing surface.
- Auth semantics must stay consistent across both styles.

## Related decisions
- ADR-003 (GraphQL for central configuration API)
- ADR-007 (.NET 9 / C# central server)
- ADR-008 (HotChocolate GraphQL in .NET)

## Supersession note
This ADR reflected an intermediate design where probe runtime polling and heartbeat were exposed as REST endpoints. The current system moved probe runtime, heartbeat, pending actions, metric reporting, and action status updates into GraphQL while keeping REST only for the central `/metrics` export, Grafana embed orchestration, and plugin bundle download. See ADR-012.
