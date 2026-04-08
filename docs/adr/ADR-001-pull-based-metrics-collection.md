# ADR-001: Use pull-based metrics collection with Prometheus

|Metadata|Value|
|--------|-----|
|Date|[2026-03-20]|
|Status|Accepted|
|Tags|beacon, prometheus, metrics, monitoring|

## Context
BEACON deploys multiple Raspberry Pi probes across a building, each attached to local Wi-Fi segments with variable signal quality and occasional packet loss. The system needs a consistent way to collect RSSI, ping, HTTP, and iPerf-derived measurements from all probes without requiring manual intervention on every host. Probes are small devices with limited CPU, memory, and storage, so telemetry transport cannot rely on heavy buffering or complex delivery logic running continuously. The network topology also matters: probes may be intermittently reachable due to power cycles, Wi-Fi roaming, or local AP congestion, while the central monitoring stack remains the most stable component. Operationally, the team needs an approach where probe outages are visible as first-class signals instead of silently dropping data. This made the data flow model (push from probes vs pull from central collector) a meaningful architecture decision rather than an implementation detail.

## Decision
BEACON uses Prometheus in pull mode, where Prometheus periodically scrapes each probe's `/metrics` HTTP endpoint in Prometheus exposition format. The probe agent is responsible for executing tests and updating in-memory metric values; it does not manage delivery retries, queueing semantics, or time-series persistence. Prometheus runs centrally and scrapes each configured target on a fixed interval (for example every 15 seconds), storing both value samples and target health. When a probe is unreachable, the scrape failure is recorded directly by Prometheus, making liveness and data availability observable from the same monitoring plane. Grafana queries Prometheus for dashboards and alerting rules use scrape health plus test metrics to detect probe and network issues.

## Alternatives considered

### 1. Probes push directly to a time-series database
How it would concretely work inside BEACON: Each Raspberry Pi probe would open outbound connections to a central TSDB endpoint and send test results after every scheduler cycle. The probe agent would need batching, retry logic, and local buffering to handle intermittent Wi-Fi and avoid losing measurements during outages.

Why it was rejected: This shifts delivery reliability complexity to every probe, multiplying operational risk across N devices. Under unstable Wi-Fi, probes would either lose samples or consume local disk for retries, and each probe would need robust backpressure handling. It also weakens central visibility of probe reachability because transport failures are distributed across agents rather than measured uniformly by a collector.

### 2. Probes push to a message broker (MQTT)
How it would concretely work inside BEACON: Each probe would publish test events to MQTT topics such as `beacon/probes/<id>/metrics`, and a central consumer would transform those events into Prometheus-compatible series. The Admin and monitoring stack would depend on broker uptime, topic design, and consumer correctness for complete observability.

Why it was rejected: MQTT introduces an additional always-on subsystem (broker plus ingestion consumer) that the small team must deploy, secure, and operate. The event pipeline increases failure modes: broker partitions, retained-message policy errors, and consumer lag can all distort current state. BEACON's required output is already Prometheus exposition, so introducing broker translation adds complexity without solving a bottleneck in this deployment size.

### 3. Probes write directly to a shared database
How it would concretely work inside BEACON: Each probe would maintain credentials to a shared central database and insert rows for every test execution, including timestamps and probe identifiers. Dashboards would query that database directly or through a custom API for visualization.

Why it was rejected: Direct writes from many edge devices expand the attack surface and credential management burden on constrained hardware. Schema evolution for heterogeneous tests (RSSI scans vs iPerf throughput) becomes tightly coupled to probe rollout, making agent upgrades riskier. This approach also bypasses Prometheus-native scrape health semantics and requires custom retention, downsampling, and aggregation logic.

### 4. Custom push gateway
How it would concretely work inside BEACON: Probes would POST metrics payloads to a custom gateway service, and that service would expose consolidated `/metrics` for Prometheus to scrape. The gateway would act as a relay, managing temporary storage and endpoint normalization.

Why it was rejected: Building a custom gateway duplicates capabilities that Prometheus already provides through target scraping and service discovery patterns. Gateway reliability would become critical path; if it fails, all probes appear healthy locally but metrics stop centrally. The added component also creates ambiguity around failure attribution (probe failure vs gateway failure), complicating troubleshooting during internship-timeline delivery.

## Consequences

### Positive
- Probe agents remain lightweight: they only expose `/metrics` and run tests, reducing CPU and memory overhead on Raspberry Pi OS devices.
- Centralized scrape results provide immediate visibility into probe reachability and stale targets, which is essential in a building where devices can disconnect.
- Prometheus retention, query model, and alert rules are used directly without maintaining a custom ingestion protocol.
- Adding new probes is operationally simple: register target and scrape it, without configuring per-probe delivery pipelines.
- Failure modes are easier to reason about because data collection behavior is concentrated in one monitoring subsystem.

### Negative
- Prometheus must be able to reach probe endpoints, so network ACLs and addressability must be configured correctly across building segments.
- Short-lived metrics can be missed between scrape intervals, requiring careful metric design for bursty tests.
- Scrape fan-out from central Prometheus can grow with probe count and may require tuning interval, timeout, and concurrency.
- Historical gaps appear when probes are offline; pull does not backfill missing samples unless probes persist and re-expose prior data.

## Related decisions
This decision depends on ADR-002 because plugin outputs must be normalized into stable metric names and labels before exposure at `/metrics`. It also influences ADR-003 because probe configuration retrieval cadence from GraphQL affects how quickly scrape target metadata and test behavior converge.
