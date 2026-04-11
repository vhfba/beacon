# ADR-010: Use heartbeat-based probe liveness and ACTIVE-only scraping

|Metadata|Value|
|--------|-----|
|Date|[2026-04-11]|
|Status|Accepted|
|Depends on|ADR-001, ADR-009|
|Tags|beacon, liveness, heartbeat, prometheus, probe-status|

## Context
BEACON needs consistent fleet liveness visibility in the central UI and correct scrape eligibility for monitoring. Earlier behavior could lead to confusing states when probes sent updates while decommissioned or when heartbeat freshness alone was treated as online.

## Decision
Implement liveness with explicit heartbeat and status-aware rules:

1. Probes send periodic heartbeat to central (`POST /probes/{probeId}/heartbeat`).
2. Central records `lastHeartbeat` and marks probe ACTIVE on heartbeat unless probe is DECOMMISSIONED.
3. Decommissioned probes cannot update heartbeat (request rejected with conflict).
4. Prometheus service discovery includes ACTIVE probes only.
5. Probe agent polls runtime state (`GET /probes/{probeId}/runtime-state`) and emits test metrics only when `canEmitMetrics=true`.
6. UI heartbeat indicator is status-aware:
   - DECOMMISSIONED => shown as decommissioned
   - INACTIVE => shown as inactive
   - ACTIVE + fresh heartbeat => online
   - ACTIVE + old heartbeat => stale

## Alternatives considered

### 1. Heartbeat timestamp only (ignore status)
How it would work: derive online purely from recent heartbeat.

Why it was rejected: decommissioned probes could appear online, causing operational confusion.

### 2. Status-only with no heartbeat
How it would work: rely on manual/admin status transitions.

Why it was rejected: no real liveness signal; stale probes remain marked active too long.

### 3. Prometheus scrape success as liveness source
How it would work: infer probe online state from scrape availability.

Why it was rejected: couples liveness with monitoring path; cannot distinguish intentionally inactive/decommissioned probes cleanly.

## Consequences

### Positive
- Liveness in UI reflects both operational freshness and lifecycle policy.
- Monitoring stack scrapes only eligible ACTIVE probes.
- Probe agent and central server agree on runtime authority via `runtime-state`.

### Negative
- Requires synchronized probe keys/env configuration for heartbeat and runtime polling.
- Adds operational endpoints and more moving parts to test.

## Related decisions
- ADR-001 (pull-based metrics collection)
- ADR-009 (REST operational endpoints)
