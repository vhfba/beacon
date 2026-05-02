# BEACON Docs

This folder is the source of truth for cross-repository documentation.

## Start Here

- [Platform Overview](../code/README.md)
- [Central Server](../code/central-server/README.md)
- [Probe Agent](../code/probe-agent/README.md)
- [Monitoring Stack](../code/monitoring-stack/README.md)
- [API Reference](./graphql-api.md)
- [Deployment Guide](./deploy.md)

## What Lives Here

- `graphql-api.md`
  Current GraphQL and operational HTTP reference for the .NET central server.
- `deploy.md`
  Local and container deployment guide for the full platform.
- `adr/`
  Architecture decision records, including historical decisions that were later superseded.
- `architecture/`
  PlantUML communication, use-case, and sequence diagrams for the current control plane.
- `c4-model/`
  C4 context, container, and component views for the current deployable system.

## Architecture Coverage

Current architecture diagrams focus on:

- central-server as the control plane
- probe-agent heartbeat, config polling, action polling, and metric reporting
- plugin assignment and bundle delivery
- Prometheus scraping only central-server `/metrics`
- Grafana embed and plugin-dashboard synchronization flows
- central-server computed coverage scoring from latest probe snapshots
- campus-grid monitoring grouped from probe location labels

Current C4 views include:

- system context
- container view
- central-server component view
- probe-agent component view
- monitoring-stack component view

## Recommended Reading Order

1. Read [code/README.md](../code/README.md) for the platform shape.
2. Read [code/central-server/README.md](../code/central-server/README.md) for the control plane and domain model.
3. Read [code/probe-agent/README.md](../code/probe-agent/README.md) for probe runtime behavior.
4. Read [docs/graphql-api.md](./graphql-api.md) when integrating with the API.
5. Read [docs/deploy.md](./deploy.md) when standing up the full stack.

## Notes On Historical Material

- ADRs `ADR-004`, `ADR-005`, and `ADR-006` document the pre-.NET design and are kept for decision history only.
- ADRs `ADR-001`, `ADR-009`, and `ADR-010` are now historical and point to the newer decisions that replaced their earlier runtime and monitoring assumptions.
