# ADR-014: Compute fleet coverage summaries in central-server

|Metadata|Value|
|--------|-----|
|Date|[2026-05-02]|
|Status|Accepted|
|Depends on|ADR-011, ADR-012, ADR-013|
|Tags|beacon, monitoring, coverage, graphql, ui|

## Context
BEACON already aggregates the latest probe metric snapshots in central-server and exposes them to Prometheus. The admin experience now needs a simple fleet-level answer to the question "where is Wi-Fi coverage weak right now?" without forcing operators to build that interpretation only in Grafana. The simulator UI also needs a location-grouped campus view and a single quality score that can be reused consistently across fleet, map, and plugin workflows. Computing that score only in the browser would duplicate business logic, while computing it only in Grafana would make it harder to reuse in the control plane.

## Decision
BEACON will compute fleet coverage summaries in central-server from the latest stored probe metric snapshots.

1. Central-server derives a `fleetCoverage` view from the latest pushed Wi-Fi and ping metrics already stored in the metrics snapshot store.
2. Coverage is exposed through GraphQL as an admin-facing summary with score, grade, latest relevant signal values, sample count, and snapshot timestamp.
3. The admin UI may use this summary to render campus-grid and fleet quality views grouped from existing probe `location` strings.
4. Coverage scoring is intentionally latest-snapshot based. Historical trend analysis remains Grafana and Prometheus work, not central-server domain state.
5. No new probe coordinate model or database migration is introduced for the first campus-grid version.

## Alternatives considered

### 1. Compute coverage only in Grafana
How it would work inside BEACON: Grafana panels and expressions would derive all quality scoring, while central-server would continue exposing only raw metrics and probe metadata.

Why it was rejected: The result would not be reusable in the simulator UI, plugin workflow, or GraphQL without duplicating logic elsewhere.

### 2. Compute coverage only in the browser
How it would work inside BEACON: The simulator UI would fetch raw metrics or ad hoc summaries and calculate coverage scores client-side.

Why it was rejected: It would duplicate interpretation logic in a presentation layer and make future reuse harder for other clients.

### 3. Persist a dedicated historical coverage table
How it would work inside BEACON: Every metric report would also produce stored coverage records for analytics and map rendering.

Why it was rejected: The current product goal is actionable latest-state visibility, not long-term analytical warehousing inside the control plane.

## Consequences

### Positive
- Coverage quality becomes part of the control-plane API instead of living only in dashboards.
- Fleet and campus-grid UI can show a consistent score and grade without re-implementing the formula.
- The design stays aligned with the existing central aggregation model.

### Negative
- Central-server now owns one more interpretation layer on top of raw metrics.
- The score is intentionally approximate and latest-state oriented, not a replacement for historical dashboard analysis.
- Probe `location` remains a free-form label, so campus grouping is useful but not as precise as a coordinate-based map.

## Related decisions
- ADR-011 (central aggregated metrics export)
- ADR-012 (GraphQL control-plane runtime workflows)
- ADR-013 (plugin-provided Grafana dashboards)
