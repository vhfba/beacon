# Runtime Flows

Runtime flows are documented per use case using the same reading order:

1. SSD: actor-to-platform interaction.
2. C3 slice: activated containers and components.
3. Internal sequence: detailed interaction inside the platform.

## Use Cases

| Use case | SSD | C3 slice | Sequence |
|---|---|---|---|
| Probe registration | [SSD](../diagrams/use-cases/uc-probe-registration/ssd.puml) | [C3 slice](../diagrams/use-cases/uc-probe-registration/c3-slice.puml) | [Sequence](../diagrams/use-cases/uc-probe-registration/sequence.puml) |
| Plugin assignment | [SSD](../diagrams/use-cases/uc-plugin-assignment/ssd.puml) | [C3 slice](../diagrams/use-cases/uc-plugin-assignment/c3-slice.puml) | [Sequence](../diagrams/use-cases/uc-plugin-assignment/sequence.puml) |
| Probe runtime cycle | [SSD](../diagrams/use-cases/uc-probe-runtime-cycle/ssd.puml) | [C3 slice](../diagrams/use-cases/uc-probe-runtime-cycle/c3-slice.puml) | [Sequence](../diagrams/use-cases/uc-probe-runtime-cycle/sequence.puml) |
| Report metrics | [SSD](../diagrams/use-cases/uc-report-metrics/ssd.puml) | [C3 slice](../diagrams/use-cases/uc-report-metrics/c3-slice.puml) | [Sequence](../diagrams/use-cases/uc-report-metrics/sequence.puml) |
| Action trigger | [SSD](../diagrams/use-cases/uc-action-trigger/ssd.puml) | [C3 slice](../diagrams/use-cases/uc-action-trigger/c3-slice.puml) | [Sequence](../diagrams/use-cases/uc-action-trigger/sequence.puml) |
| Dashboard embed | [SSD](../diagrams/use-cases/uc-dashboard-embed/ssd.puml) | [C3 slice](../diagrams/use-cases/uc-dashboard-embed/c3-slice.puml) | [Sequence](../diagrams/use-cases/uc-dashboard-embed/sequence.puml) |
| Heartbeat | [SSD](../diagrams/use-cases/uc-heartbeat/ssd.puml) | [C3 slice](../diagrams/use-cases/uc-heartbeat/c3-slice.puml) | [Sequence](../diagrams/use-cases/uc-heartbeat/sequence.puml) |
| Fleet status | [SSD](../diagrams/use-cases/uc-fleet-status/ssd.puml) | [C3 slice](../diagrams/use-cases/uc-fleet-status/c3-slice.puml) | [Sequence](../diagrams/use-cases/uc-fleet-status/sequence.puml) |

## Probe Control Commands

Profile and Wi-Fi administration use central-server queued commands rather than direct inbound access to probes:

1. Admin queues `UPDATE_PROFILE`, `SCAN_WIFI_NETWORKS`, or `CONNECT_WIFI`.
2. Probe pulls pending control commands over GraphQL.
3. Probe reports command status and result JSON back to central-server.
4. Admin UI reads command history from central-server.

Probe IDs stay stable. `UPDATE_PROFILE` changes display name/location only, while heartbeat continues to refresh observed IP, SSID, agent version, and liveness timestamps.
