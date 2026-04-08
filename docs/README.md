# BEACON Architecture Diagrams

This folder contains the architecture and C4 model diagrams for the BEACON project.

## Structure

- `architecture/`: focused technical architecture views
- `c4-model/`: C4 diagrams (levels 1 to 3)
- `adr/`: architecture decision records

## Diagram Index

### API Documentation

- `graphql-api.md`
  GraphQL endpoint reference with schema summary and example queries/mutations.

- `central-server-completion-plan.md`
  Objective-to-implementation coverage matrix and prioritized central-server completion plan.

### Architecture

- `architecture/communication-flow.puml`  
  Communication flow between admin, central server, probes, and monitoring stack.

- `architecture/plugin-testing-architecture.puml`  
  Plugin-based testing flow inside probe agents and server-side plugin repository usage.

- `architecture/use-cases-diagram.puml`  
  Use case view for Admin and Probe Agent interactions with the central server and monitoring stack, including plugin registration.

- `architecture/use-case-uc2-view-fleet-status-sequence.puml`  
  Sequence flow for viewing fleet status by merging probe registry data with enabled tests.

- `architecture/use-case-uc3-set-tests-sequence.puml`  
  Sequence flow for enabling/disabling probe tests through auth, mutation resolver, and config service.

- `architecture/use-case-uc5-view-dashboards-sequence.puml`  
  Sequence flow for Grafana dashboard access and Prometheus query execution.

- `architecture/use-case-register-probe-sequence.puml`  
  Sequence flow for probe registration via RegisterProbeUseCase.

- `architecture/use-case-get-fleet-status-sequence.puml`  
  Sequence flow for fleet retrieval via GetFleetStatusUseCase.

- `architecture/use-case-get-probe-config-sequence.puml`  
  Sequence flow for probe config polling via GetProbeConfigUseCase.

- `architecture/use-case-update-probe-test-config-sequence.puml`  
  Sequence flow for interval/enabled updates via UpdateProbeTestConfigUseCase.

- `architecture/use-case-update-probe-status-sequence.puml`  
  Sequence flow for lifecycle state updates via UpdateProbeStatusUseCase.

- `architecture/use-case-list-plugins-sequence.puml`  
  Sequence flow for plugin listing via ListPluginsUseCase.

- `architecture/use-case-register-plugin-sequence.puml`  
  Sequence flow for plugin registration via RegisterPluginUseCase.

- `architecture/use-case-get-plugin-by-id-sequence.puml`  
  Sequence flow for single plugin lookup via GetPluginByIdUseCase.

- `architecture/use-case-set-probe-test-enabled-sequence.puml`  
  Sequence flow for toggling test enabled state via SetProbeTestEnabledUseCase.

- `architecture/use-case-set-plugin-availability-sequence.puml`  
  Sequence flow for toggling plugin availability via SetPluginAvailabilityUseCase.

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

- `adr/ADR-004-use-java-spring-boot-for-the-central-server.md`  
  Decision to implement the central server in Java 21 with Spring Boot.

- `adr/ADR-005-use-strawberry-for-the-graphql-layer.md`  
  Decision to use Spring GraphQL for the GraphQL layer.

- `adr/ADR-006-use-postgresql-with-sqlalchemy-and-alembic-for-configuration-storage.md`  
  Decision to use PostgreSQL with Spring Data JPA and Flyway for persistent configuration storage.

## Styling

- There is no shared local style include for diagrams.
- C4 diagrams under `c4-model/` use C4-PlantUML with internet `!includeurl` directives.
- Architecture diagrams under `architecture/` use plain PlantUML without a shared theme file.
