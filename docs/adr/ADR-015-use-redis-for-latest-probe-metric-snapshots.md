# ADR-015: Use Redis for latest probe metric snapshots

|Metadata|Value|
|--------|-----|
|Date|[2026-05-02]|
|Status|Accepted|
|Depends on|ADR-007, ADR-011, ADR-012|
|Tags|beacon, redis, metrics, snapshots, monitoring|

## Context
BEACON's central-server accepts pushed metric snapshots from probe agents and exposes the latest known values through a single Prometheus-compatible `/metrics` endpoint. The platform does not currently need a full historical metrics warehouse inside the control plane. What it does need is a lightweight store for the newest per-probe snapshot with predictable read and write performance, TTL-based expiry, and simple deployment alongside the existing central-server stack. The implementation already separates configuration data in PostgreSQL from aggregated metrics state, so the remaining decision is which backing store should hold these latest snapshots.

## Decision
BEACON will use Redis as the deployed backing store for latest probe metric snapshots.

1. Probe agents push snapshots to central-server through `reportProbeMetrics`.
2. Central-server stores the latest snapshot for each probe in Redis under a BEACON-specific key prefix.
3. Snapshot entries use TTL expiry so stale probes naturally age out of the metrics cache.
4. Central-server renders `/metrics` and related latest-state monitoring summaries from the current Redis snapshot set.
5. In-memory metrics storage may still be used in tests or lightweight local scenarios, but Redis is the reference deployed metrics store.

## Alternatives considered

### 1. Store latest snapshots in PostgreSQL
How it would work inside BEACON: The central relational database would hold the newest metric snapshot rows and central-server would read them back when exporting `/metrics`.

Why it was rejected: Latest-snapshot metrics are cache-like operational state, not core relational configuration data. Using PostgreSQL for this path would add unnecessary write churn and schema complexity.

### 2. Keep all snapshots only in process memory
How it would work inside BEACON: Central-server would store snapshots in memory without any external backing store.

Why it was rejected: Process-local state would be lost on restart and would not reflect a realistic multi-component deployment.

### 3. Write metrics directly to a time-series database from central-server
How it would work inside BEACON: Central-server would forward probe metrics to a TSDB and then read them back for monitoring views.

Why it was rejected: The current platform goal is to expose latest-state visibility with minimum infrastructure. Adding a separate TSDB would expand the stack without improving the core demo workflow enough.

## Consequences

### Positive
- Redis matches the latest-snapshot access pattern well: fast writes, simple keyed reads, and TTL-based expiry.
- Metrics state stays separate from PostgreSQL configuration state.
- The deployed control plane can restart independently of probe agents while still restoring the most recent snapshot cache from Redis persistence settings.
- Central-server can reuse the same latest-snapshot store for `/metrics` export and fleet-level coverage summaries.

### Negative
- Redis adds another infrastructure dependency to the central-server deployment.
- The snapshot cache is still latest-state oriented and does not replace historical analytics in Prometheus/Grafana.
- Key naming, TTL tuning, and cleanup behavior become part of BEACON's operational contract.

## Related decisions
- ADR-007 (.NET 9 / C# central server)
- ADR-011 (central aggregated metrics export)
- ADR-012 (GraphQL control-plane runtime workflows)
