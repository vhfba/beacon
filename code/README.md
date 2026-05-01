# BEACON Workspace

The `code/` folder contains the runnable parts of the platform.

## Components

- [central-server](./central-server/README.md)
  .NET 9 control plane for probe registry, plugin distribution, runtime orchestration, action queueing, and Prometheus metric export.
- [probe-agent](./probe-agent/README.md)
  Lightweight probe runtime that polls config, executes plugins, reports metrics, and performs on-demand actions.
- [monitoring-stack](./monitoring-stack/README.md)
  Prometheus and Grafana deployment for fleet observability.

## Current Platform Shape

BEACON currently works as a central aggregation model:

1. probes authenticate to central-server
2. probes pull config and pending actions over GraphQL
3. probes download plugin bundles from central-server
4. probes push heartbeat and metric snapshots back to central-server
5. Prometheus scrapes central-server `/metrics`
6. Grafana visualizes Prometheus data

## Main Use Cases

- register and manage probes
- register and distribute scheduled or action plugins
- assign plugins to probes
- configure scheduled checks such as `PING`, `HTTP`, `IPERF`, and `WIFI`
- trigger on-demand actions such as `WIFI_SCAN_ACTION`
- aggregate probe metrics centrally for monitoring dashboards

## Documentation Map

- Cross-repo docs: [docs/README.md](../docs/README.md)
- API reference: [docs/graphql-api.md](../docs/graphql-api.md)
- Deployment: [docs/deploy.md](../docs/deploy.md)
