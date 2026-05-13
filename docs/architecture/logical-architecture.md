# Logical Architecture

BEACON is a distributed network measurement platform for campus Wi-Fi coverage, reliability, and interference analysis.

The system follows central aggregation. Probe runtimes authenticate to the platform, pull runtime configuration and pending actions, download assigned plugin bundles, execute scheduled or on-demand checks, and report heartbeat and metric snapshots. The platform owns authoritative fleet state and exposes aggregate metrics and dashboard integration points.

## Actors

- **Admin**: manages probes, plugins, assignments, scheduled checks, on-demand actions, and dashboard access.
- **Probe Runtime**: runs on a probe device or simulator, polls BEACON, executes plugins, and reports runtime state.
- **Dashboard Consumer**: views monitoring dashboards and embeds.
- **Metrics Collector**: scrapes aggregate BEACON metrics.
- **Dashboard Service**: renders dashboards and embeds.
- **Site Network Targets**: wireless and network endpoints measured by probes.

## Logical Components

- **Control Plane**: owns probe inventory, plugin metadata, assignments, action queueing, latest metric aggregation, coverage summaries, and monitoring integration.
- **Probe Runtime**: polls runtime configuration, downloads plugin bundles, executes checks, sends heartbeats, reports metrics, and processes actions.
- **Domain Data Store**: owns durable configuration and workflow state.
- **Metric Snapshot Store**: owns latest per-probe metric snapshots.
- **Metrics Collector**: stores time-series samples derived from BEACON metric export.
- **Dashboard Service**: renders dashboards and plugin-provided visualizations.

## Constraints

- The control plane is the source of truth for fleet and plugin state.
- Monitoring scrapes the control plane export, not individual probes.
- Plugin IDs, versions, manifests, bundle filenames, execution modes, and output shapes are cross-component contracts.
- Metric names and labels are contracts with dashboards and alerting rules.
- Logical architecture diagrams avoid implementation-library details.
