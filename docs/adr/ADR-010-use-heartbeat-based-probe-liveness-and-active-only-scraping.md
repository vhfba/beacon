# ADR-010: Use heartbeat-based probe liveness and ACTIVE-only scraping

|Metadata|Value|
|--------|-----|
|Date|[2026-04-11]|
|Status|Superseded by ADR-011 and ADR-012|
|Depends on|ADR-001, ADR-009|
|Tags|beacon, liveness, heartbeat, prometheus, probe-status|

## Context
BEACON needs consistent fleet liveness visibility in the central UI and correct scrape eligibility for monitoring. Earlier behavior could lead to confusing states when probes sent updates while decommissioned or when heartbeat freshness alone was treated as online.

## Decision
Implement liveness with explicit heartbeat and status-aware rules:

1. Probes send periodic heartbeat to central.
2. Central records `lastHeartbeat` and updates liveness state unless the probe is decommissioned.
3. Decommissioned probes cannot update heartbeat.
4. Earlier versions coupled scrape eligibility to ACTIVE probe state.
5. Probe agents used runtime state to decide if they could emit metrics.

## Alternatives considered

### 1. Heartbeat timestamp only
How it would work: derive online purely from recent heartbeat.

Why it was rejected: decommissioned probes could appear online.

### 2. Status-only with no heartbeat
How it would work: rely on manual or admin status transitions.

Why it was rejected: there is no real liveness signal.

### 3. Prometheus scrape success as liveness source
How it would work: infer probe online state from scrape availability.

Why it was rejected: it couples liveness with the monitoring path.

## Consequences

### Positive
- Liveness in the UI reflects both freshness and lifecycle state.

### Negative
- Requires synchronized runtime and status handling across probe and central flows.

## Related decisions
- ADR-001 (pull-based metrics collection)
- ADR-009 (REST operational endpoints)

## Supersession note
The heartbeat part of this ADR remains historically relevant, but the direct relationship between heartbeat and ACTIVE-only Prometheus target scraping no longer matches the deployed architecture. Probes now report heartbeat and runtime state through GraphQL, push metric snapshots to central-server, and Prometheus scrapes only the central exporter. See ADR-011 and ADR-012.
