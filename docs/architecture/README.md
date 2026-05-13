# Architecture

BEACON architecture documentation is organized as a C4+1 blueprint.

## Reading Order

1. Shared views:
   - [C1 system context](../diagrams/shared/c1-system-context.puml)
   - [C2 logical container view](../diagrams/shared/c2-container-logical.puml)
   - [Use cases overview](../diagrams/shared/use-cases-overview.puml)
   - [Dynamic communication flow](../diagrams/shared/dynamic-communication-flow.puml)
2. Per-use-case views:
   - `ssd.puml`: black-box system sequence diagram.
   - `c3-slice.puml`: C3 component slice containing only activated components.
   - `sequence.puml`: internal interaction detail.
3. Deployment view:
   - [Local deployment](../diagrams/shared/c4-deployment-local.puml)

## Blueprint Documents

- [Logical architecture](./logical-architecture.md)
- [Runtime flows](./runtime-flows.md)

## Boundary Rule

Logical architecture and C4 views use stable role names. Concrete runtime products, protocols, ports, and commands belong in reference and operations documentation.
