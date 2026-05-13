# Metrics Reference

BEACON metrics are exported by the control plane from the latest metric snapshots reported by probe runtimes.

## Collection Model

- Probe runtimes produce normalized samples from scheduled plugins and action plugins.
- Probe runtimes report samples through the runtime API.
- Control plane stores the latest snapshot per probe.
- Metrics collector scrapes only the control plane metrics export.
- Dashboard service queries the metrics collector for time-series views.

## Label Expectations

Common labels should remain stable:

- `probe_id`: stable probe identifier.
- `plugin_id`: plugin that produced the sample, when available.
- `test_type`: scheduled test type, when available.
- `site` or location-derived labels: physical grouping, when available.
- target labels: measured endpoint or network target, when useful.

## Coverage Inputs

Fleet coverage uses latest Wi-Fi and latency-related samples when available:

- signal strength
- signal quality
- signal-to-noise ratio
- latency
- packet loss
- sample freshness

Coverage summaries are latest-state operational views. Long-term trends belong in the metrics collector and dashboard service.

## Change Rules

- Adding a metric requires updating this reference, dashboard queries, and any alert or recording rules that consume it.
- Renaming or removing a metric is a breaking contract change and should have an ADR or design doc.
- New labels should be low-cardinality unless the design explicitly justifies otherwise.
- Probe runtimes should normalize plugin output before reporting it to the control plane.

## Related Files

- API contract: [api.md](./api.md)
- Plugin contract: [plugin-contract.md](./plugin-contract.md)
- Monitoring deployment: [local-deployment.md](../operations/local-deployment.md)
