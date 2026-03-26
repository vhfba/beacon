# BEACON Architecture Diagrams

This folder contains the architecture and C4 model diagrams for the BEACON project.

## Structure

- `architecture/`: focused technical architecture views
- `c4-model/`: C4 diagrams (levels 1 to 3)
- `adr/`: architecture decision records

## Diagram Index

### Architecture

- `architecture/communication-flow.puml`  
  Communication flow between admin, central server, probes, and monitoring stack.

- `architecture/plugin-testing-architecture.puml`  
  Plugin-based testing flow inside probe agents and server-side plugin repository usage.

- `architecture/use-cases-diagram.puml`  
  Use case view for Admin and Probe Agent interactions with the central server and monitoring stack.

- `architecture/use-case-uc2-view-fleet-status-sequence.puml`  
  Sequence flow for viewing fleet status by merging probe registry data with enabled tests.

- `architecture/use-case-uc3-set-tests-sequence.puml`  
  Sequence flow for enabling/disabling probe tests through auth, mutation resolver, and config service.

- `architecture/use-case-uc5-view-dashboards-sequence.puml`  
  Sequence flow for Grafana dashboard access and Prometheus query execution.

### C4 Model

- `c4-model/level-1-system-context.puml`  
  System context view (C1).

- `c4-model/level-2-container-view.puml`  
  Container view (C2).

- `c4-model/level-3-central-server-components.puml`  
  Component view (C3) for the central server.

- `c4-model/level-3-monitoring-stack-components.puml`  
  Component view (C3) for the monitoring stack.

### Architecture Decision Records

- `adr/ADR-001-pull-based-metrics-collection.md`  
  Decision to collect probe metrics via central Prometheus scraping.

- `adr/ADR-002-plugin-based-probe-test-execution.md`  
  Decision to execute probe tests via runtime-loaded plugins.

- `adr/ADR-003-graphql-for-central-configuration-api.md`  
  Decision to use GraphQL for Admin and probe configuration workflows.

- `adr/ADR-004-use-python-for-the-central-server.md`  
  Decision to implement the central server in Python with Strawberry, SQLAlchemy, and Uvicorn.

## Styling

- There is no shared local style include for diagrams.
- C4 diagrams under `c4-model/` use C4-PlantUML with internet `!includeurl` directives.
- Architecture diagrams under `architecture/` use plain PlantUML without a shared theme file.
