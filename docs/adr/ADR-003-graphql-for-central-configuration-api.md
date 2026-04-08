# ADR-003: Use GraphQL for the central configuration API

|Metadata|Value|
|--------|-----|
|Date|[2026-03-20]|
|Status|Accepted|
|Tags|beacon, graphql, configuration, api|

## Context
BEACON has two distinct API clients: an Admin-facing interface that needs broad, composable reads and controlled mutations, and probe agents that need narrow, periodic config retrieval for their own identity. The same backend domain includes probe registry data, plugin enablement, schedule intervals, and operational status, which naturally forms a connected graph rather than isolated resources. Building deployments can include intermittently reachable probes, so configuration access must support idempotent polling from devices while still enabling rich administrative workflows. A pure endpoint-per-view strategy would likely duplicate backend joins and evolve into many specialized REST paths as dashboard and operations needs grow. At the same time, the team must keep the central control plane understandable and maintainable in a small project context. The non-trivial decision was whether a single GraphQL schema could serve both clients cleanly without overcomplicating agent interactions.

## Decision
BEACON exposes a GraphQL API on the central server as the single configuration and control interface for both Admin tooling and probe agents. Probes execute a minimal query scoped to their own probe ID to fetch enabled tests, plugin versions, and schedule interval at regular polling intervals (for example every 30–60 seconds). Admin clients use richer queries and mutations to inspect probe fleet state and update configuration without adding new endpoint variants per screen. Resolver boundaries map to internal services such as Probe Registry, Test Config Service, and Plugin Repository metadata access, while auth middleware enforces role-based mutation permissions. This delivers one typed schema for both machine and human clients, with request shape tailored per caller.

## Alternatives considered

### 1. REST API
How it would concretely work inside BEACON: The central server would expose multiple endpoints such as `/probes`, `/probes/{id}/config`, `/plugins`, and `/tests`, with additional endpoints for mutations. Probe agents would call one or more fixed paths to obtain their configuration, while Admin UI would compose several endpoint responses.

Why it was rejected: Serving both clients would likely require endpoint proliferation to satisfy Admin dashboards without over-fetching or under-fetching, increasing maintenance overhead. Probe and Admin use cases evolve at different rates, so REST path design would either become too granular or too coarse. The project would need extra aggregation logic client-side or bespoke backend endpoints for each operational view.

### 2. gRPC API
How it would concretely work inside BEACON: The server and probes would communicate via protobuf-defined services for config fetch and updates, and Admin tooling would require a gRPC-compatible backend integration layer. Schema evolution would be managed through protobuf versions and generated stubs.

Why it was rejected: Browser-oriented Admin interfaces do not consume gRPC directly without proxies or gRPC-Web translation, adding infrastructure and debugging complexity. Operational introspection and ad hoc querying are less convenient than GraphQL for human operators during troubleshooting. For BEACON's mixed client profile, gRPC adds protocol complexity where flexible query composition is more valuable.

### 3. Probes read config from a shared database directly
How it would concretely work inside BEACON: Each probe would connect to the central config database and query rows for its assigned test set and interval, bypassing an API layer. Admin changes would write directly to tables and probes would poll database state.

Why it was rejected: Granting database credentials to many edge devices increases security risk and complicates credential rotation. It tightly couples probe software to storage schema, making backend refactors expensive and fragile. This also bypasses centralized authorization, validation, and audit paths that should exist at the control-plane boundary.

### 4. Message broker (MQTT) for config delivery
How it would concretely work inside BEACON: The server would publish config updates to probe-specific MQTT topics, and probes would subscribe to receive changes in near real time. Admin mutations would be translated into broker publications and retained messages.

Why it was rejected: Delivery semantics become harder to reason about under intermittent connectivity, especially around retained messages, replay, and stale subscription state after reconnect. The system would still need an authoritative store for current configuration, so broker delivery adds another consistency surface. For probe behavior that already tolerates periodic polling, event-driven delivery adds complexity without proportional operational gain.

## Consequences

### Positive
- A single typed schema serves both Admin and probe clients while allowing each to request only required fields.
- Admin workflows can evolve without introducing many bespoke endpoints, reducing API surface sprawl.
- Probe config polling remains simple and deterministic, which fits intermittent Wi-Fi conditions in building deployments.
- Resolver-level separation aligns with existing central-server components (registry, config, plugin metadata), improving code organization.
- Schema introspection and validation improve contract clarity between frontend, backend, and agent teams.

### Negative
- GraphQL introduces resolver and schema governance overhead that must be maintained carefully as features expand.
- Poorly designed queries can create expensive resolver paths, requiring guardrails such as depth limits and query complexity controls.
- Caching and observability can be less straightforward than fixed REST paths unless instrumentation is designed intentionally.
- Probe clients still need strict query discipline to avoid unnecessary payload growth on constrained links.

## Related decisions
This decision enables ADR-002 by providing the control-plane API that distributes plugin enablement and scheduling data per probe. It also complements ADR-001 by coordinating probe behavior that determines which metrics are exposed for Prometheus scraping.
