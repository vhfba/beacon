# ADR-011: Use a central-server aggregated Prometheus export for probe metrics

|Metadata|Value|
|--------|-----|
|Date|[2026-05-01]|
|Status|Accepted|
|Depends on|ADR-002, ADR-007, ADR-008|
|Tags|beacon, prometheus, metrics, redis, monitoring|

## Context
BEACON originally assumed Prometheus would scrape each probe directly. The implementation has since moved toward a central aggregation model: probe agents already talk to central-server for heartbeat, configuration, action polling, and plugin download, and central-server now has Redis-backed storage for the latest metric snapshots reported by probes. This simplifies deployment because Prometheus only needs network access to one stable service instead of every probe. It also reduces the operational coupling between probe reachability and Prometheus target management, which had started to drift from the actual codebase and deployment artifacts.

## Decision
BEACON will use central-server as the single Prometheus scrape target for probe metrics.

1. Probe agents push metric snapshots to central-server through the `reportProbeMetrics` GraphQL mutation.
2. Central-server stores the latest per-probe snapshots in a metrics store, backed by Redis in deployed environments.
3. Central-server exposes a single Prometheus-compatible `/metrics` endpoint that renders the latest aggregated samples.
4. Prometheus scrapes only central-server `/metrics`.
5. Probe liveness remains a domain concern derived from probe status plus heartbeat freshness, not from direct Prometheus scrape success against each probe.

## Alternatives considered

### 1. Keep direct Prometheus scraping of every probe
How it would work inside BEACON: Each probe would continue exposing its own scrape target and Prometheus would discover or statically manage every probe endpoint.

Why it was rejected: This no longer matches the implemented runtime and increases network and target-management overhead.

### 2. Push metrics directly from probes into a TSDB
How it would work inside BEACON: Probe agents would manage delivery to a metrics backend instead of reporting through central-server.

Why it was rejected: It duplicates transport responsibilities across every probe and bypasses the central control plane.

### 3. Persist every metric sample in PostgreSQL
How it would work inside BEACON: Central-server would store all reported samples in the primary relational database and build `/metrics` from that store.

Why it was rejected: The current need is latest-snapshot export, not long-term analytical storage in the transactional database.

## Consequences

### Positive
- Prometheus configuration is simpler because there is only one scrape target for BEACON metrics.
- Probe agents remain lightweight and only need to talk to central-server.
- Metric transport, auth, and fleet identity converge on the same control plane.
- Central-server can enforce normalization and snapshot TTL policy consistently.

### Negative
- Central-server becomes a dependency for probe metrics visibility.
- The exporter represents latest known snapshots, not a full event log.
- Redis adds another infrastructure component to central-server deployments.

## Related decisions
- ADR-002 (plugin-based probe execution)
- ADR-007 (.NET 9 / C# central server)
- ADR-008 (HotChocolate GraphQL in .NET)

## Supersedes
- ADR-001
- Parts of ADR-010
