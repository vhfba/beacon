# ADR-013: Import Grafana dashboards from plugin metadata

|Metadata|Value|
|--------|-----|
|Date|[2026-05-01]|
|Status|Accepted|
|Depends on|ADR-011, ADR-012|
|Tags|beacon, grafana, dashboards, plugins, monitoring|

## Context
BEACON plugins can now provide dashboard JSON at registration time, and central-server already has the Grafana API integration needed to import those dashboards. That makes plugin registration the right place to extend Grafana with plugin-specific visualizations. The earlier threshold-profile path added a second dashboard-management mechanism that was broader than the current product goal and created extra runtime complexity in both code and documentation.

## Decision
BEACON will only modify Grafana dashboards through plugin registration metadata.

1. `dashboardJson` on plugin registration must contain Grafana dashboard JSON.
2. Central-server validates that JSON and can import it into Grafana using a plugin-scoped dashboard UID.
3. The monitoring embed endpoint only returns an embed URL; it does not mutate Grafana dashboards or thresholds.
4. Site-specific threshold update flows are removed from the platform.
5. Admin-facing UI elements may derive plugin dashboard links from the same deterministic dashboard UID convention instead of storing separate dashboard URLs in BEACON state.

## Alternatives considered

### 1. Keep threshold-profile based dashboard mutation
How it would work inside BEACON: Central-server would continue storing threshold profiles and rewriting Grafana dashboards when embed sessions are requested.

Why it was rejected: It broadens the runtime control surface, adds non-essential behavior outside plugin registration, and makes monitoring docs and code harder to keep aligned.

### 2. Manage dashboards manually in Grafana
How it would work inside BEACON: Operators would hand-import dashboards directly in Grafana after registering plugins.

Why it was rejected: It breaks the platform workflow and makes plugin dashboards less reproducible across environments.

### 3. Store dashboard definitions separately from plugins
How it would work inside BEACON: Dashboard import would be its own management feature disconnected from plugin metadata.

Why it was rejected: The current need is specifically to let plugins contribute their own graphs, so a separate workflow would add complexity without clear value.

## Consequences

### Positive
- Grafana mutation now happens in exactly one workflow: plugin registration.
- The monitoring embed endpoint is simpler and side-effect free.
- The platform no longer carries threshold-profile storage or sync logic.

### Negative
- Site-specific dashboard customization is no longer supported through central-server.
- Plugin authors must provide valid Grafana dashboard JSON when they want custom graphs.

## Related decisions
- ADR-011 (central aggregated metrics export)
- ADR-012 (GraphQL control-plane runtime workflows)
