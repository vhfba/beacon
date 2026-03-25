# BEACON Architecture Diagrams

This folder contains the architecture and C4 model diagrams for the BEACON project.

## Structure

- `architecture/`: focused technical architecture views
- `c4-model/`: C4 diagrams (levels 1 to 3)
- `adr/`: architecture decision records
- `styles/`: shared PlantUML style files

## Diagram Index

### Architecture

- `architecture/communication-flow.puml`  
  Communication flow between admin, central server, probes, and monitoring stack.

- `architecture/plugin-testing-architecture.puml`  
  Plugin-based testing flow inside probe agents and server-side plugin repository usage.

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

## Styling

All diagrams use the shared theme:

- `styles/theme-simple.puml`

To keep consistency, include this in each `.puml` file after `@startuml`:

```puml
!include ../styles/theme-simple.puml
```

(For files inside `architecture/` and `c4-model/`, the relative include path above is correct.)
