# BEACON Diagrams

BEACON uses a C4+1 documentation model.

## Model

- **C1 system context**: shared platform boundary and external actors.
- **C2 container view**: shared logical containers and their relationships.
- **C3 slice**: per-use-case component view showing only components activated by that flow.
- **+1 sequence pair**: each use case has an SSD for the black-box interaction and an internal sequence for platform detail.
- **Deployment view**: local deployment topology and concrete runtime products.

## Reading Order Contract

For a use case, read diagrams in this order:

1. `ssd.puml`: actor to BEACON Platform, no internal components.
2. `c3-slice.puml`: activated containers/components only.
3. `sequence.puml`: internal interaction detail.

Shared diagrams provide context before selecting a use case:

- [shared/c1-system-context.puml](./shared/c1-system-context.puml)
- [shared/c2-container-logical.puml](./shared/c2-container-logical.puml)
- [shared/use-cases-overview.puml](./shared/use-cases-overview.puml)
- [shared/dynamic-communication-flow.puml](./shared/dynamic-communication-flow.puml)
- [shared/c4-deployment-local.puml](./shared/c4-deployment-local.puml)

## Use Case Diagram Sets

| Use case | SSD | C3 slice | Internal sequence |
|---|---|---|---|
| Probe registration | [ssd.puml](./use-cases/uc-probe-registration/ssd.puml) | [c3-slice.puml](./use-cases/uc-probe-registration/c3-slice.puml) | [sequence.puml](./use-cases/uc-probe-registration/sequence.puml) |
| Plugin assignment | [ssd.puml](./use-cases/uc-plugin-assignment/ssd.puml) | [c3-slice.puml](./use-cases/uc-plugin-assignment/c3-slice.puml) | [sequence.puml](./use-cases/uc-plugin-assignment/sequence.puml) |
| Probe runtime cycle | [ssd.puml](./use-cases/uc-probe-runtime-cycle/ssd.puml) | [c3-slice.puml](./use-cases/uc-probe-runtime-cycle/c3-slice.puml) | [sequence.puml](./use-cases/uc-probe-runtime-cycle/sequence.puml) |
| Report metrics | [ssd.puml](./use-cases/uc-report-metrics/ssd.puml) | [c3-slice.puml](./use-cases/uc-report-metrics/c3-slice.puml) | [sequence.puml](./use-cases/uc-report-metrics/sequence.puml) |
| Action trigger | [ssd.puml](./use-cases/uc-action-trigger/ssd.puml) | [c3-slice.puml](./use-cases/uc-action-trigger/c3-slice.puml) | [sequence.puml](./use-cases/uc-action-trigger/sequence.puml) |
| Dashboard embed | [ssd.puml](./use-cases/uc-dashboard-embed/ssd.puml) | [c3-slice.puml](./use-cases/uc-dashboard-embed/c3-slice.puml) | [sequence.puml](./use-cases/uc-dashboard-embed/sequence.puml) |
| Heartbeat | [ssd.puml](./use-cases/uc-heartbeat/ssd.puml) | [c3-slice.puml](./use-cases/uc-heartbeat/c3-slice.puml) | [sequence.puml](./use-cases/uc-heartbeat/sequence.puml) |
| Fleet status | [ssd.puml](./use-cases/uc-fleet-status/ssd.puml) | [c3-slice.puml](./use-cases/uc-fleet-status/c3-slice.puml) | [sequence.puml](./use-cases/uc-fleet-status/sequence.puml) |
