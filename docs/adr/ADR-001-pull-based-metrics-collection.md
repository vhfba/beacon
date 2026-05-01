# ADR-001: Use pull-based metrics collection with Prometheus

|Metadata|Value|
|--------|-----|
|Date|[2026-03-20]|
|Status|Superseded by ADR-011|
|Tags|beacon, prometheus, metrics, monitoring|

## Context
BEACON deploys multiple Raspberry Pi probes across a building, each attached to local Wi-Fi segments with variable signal quality and occasional packet loss. The system needs a consistent way to collect RSSI, ping, HTTP, and iPerf-derived measurements from all probes without requiring manual intervention on every host. Probes are small devices with limited CPU, memory, and storage, so telemetry transport cannot rely on heavy buffering or complex delivery logic running continuously. The network topology also matters: probes may be intermittently reachable due to power cycles, Wi-Fi roaming, or local AP congestion, while the central monitoring stack remains the most stable component. Operationally, the team needs an approach where probe outages are visible as first-class signals instead of silently dropping data. This made the data flow model (push from probes vs pull from central collector) a meaningful architecture decision rather than an implementation detail.

## Decision
BEACON uses Prometheus in pull mode, where Prometheus periodically scrapes each probe's `/metrics` HTTP endpoint in Prometheus exposition format. The probe agent is responsible for executing tests and updating in-memory metric values; it does not manage delivery retries, queueing semantics, or time-series persistence. Prometheus runs centrally and scrapes each configured target on a fixed interval, storing both value samples and target health. When a probe is unreachable, the scrape failure is recorded directly by Prometheus, making liveness and data availability observable from the same monitoring plane. Grafana queries Prometheus for dashboards and alerting rules use scrape health plus test metrics to detect probe and network issues.

## Alternatives considered

### 1. Probes push directly to a time-series database
How it would concretely work inside BEACON: Each Raspberry Pi probe would open outbound connections to a central TSDB endpoint and send test results after every scheduler cycle.

Why it was rejected: This shifts delivery reliability complexity to every probe, multiplying operational risk across N devices.

### 2. Probes push to a message broker
How it would concretely work inside BEACON: Each probe would publish test events to a broker and a central consumer would transform them into Prometheus-compatible series.

Why it was rejected: It introduces additional always-on infrastructure and translation complexity.

### 3. Probes write directly to a shared database
How it would concretely work inside BEACON: Each probe would insert test rows into a central database.

Why it was rejected: This expands the attack surface and bypasses Prometheus-native scrape health semantics.

### 4. Custom push gateway
How it would concretely work inside BEACON: Probes would POST metrics to a custom gateway that exposes consolidated `/metrics`.

Why it was rejected: It duplicates functionality while creating another critical-path component.

## Consequences

### Positive
- Probe agents remain lightweight.
- Centralized scrape results provide immediate visibility into probe reachability.
- Prometheus retention, query model, and alert rules are used directly.

### Negative
- Prometheus must be able to reach every probe endpoint.
- Short-lived metrics can be missed between scrape intervals.
- Scrape fan-out grows with probe count.

## Related decisions
This decision depends on ADR-002 because plugin outputs must be normalized into stable metric names and labels before exposure at `/metrics`. It also influences ADR-003 because probe configuration retrieval cadence from GraphQL affects how quickly scrape target metadata and test behavior converge.

## Supersession note
This ADR captured the initial direct-scrape approach where Prometheus scraped each probe endpoint. The current system no longer does that. See ADR-011 for the current design, where probes push metric snapshots to central-server and Prometheus scrapes only the central `/metrics` endpoint.
