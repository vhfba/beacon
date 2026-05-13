# BEACON Documentation

This directory is the source of truth for BEACON product, architecture, design, reference, and operations documentation.

The documentation is organized so reviewers can understand the system before reading code. Logical architecture describes responsibilities and contracts without binding them to concrete technology choices. Deployment, reference, and ADR documents then explain the implementation choices that realize that architecture.

## Reading Paths

### Engineering reviewers

1. [Problem statement](./product/problem-statement.md)
2. [Goals and non-goals](./product/goals-and-non-goals.md)
3. [Logical architecture](./architecture/logical-architecture.md)
4. [Runtime flows](./architecture/runtime-flows.md)
5. [Data and ownership](./architecture/data-and-ownership.md)
6. [Failure modes](./architecture/failure-modes.md)
7. [ADRs](./adr/)

### Implementers

1. [Design workflow](./designs/README.md)
2. [Design doc template](./designs/design-doc-template.md)
3. [API reference](./reference/api.md)
4. [Plugin contract](./reference/plugin-contract.md)
5. [Metrics reference](./reference/metrics.md)

### Operators

1. [Local deployment](./operations/local-deployment.md)
2. [Troubleshooting](./operations/troubleshooting.md)
3. [Runbooks](./operations/runbooks.md)

### Academic evaluators

1. [Problem statement](./product/problem-statement.md)
2. [Goals and non-goals](./product/goals-and-non-goals.md)
3. [System context diagram](./diagrams/c4/c1-system-context.puml)
4. [Logical container diagram](./diagrams/c4/c2-container-logical.puml)
5. [Local deployment diagram](./diagrams/c4/c4-deployment-local.puml)

## Documentation Map

- `product/`: problem framing, target users, scope, and glossary.
- `architecture/`: technology-neutral system design, data ownership, observability, security, and failure behavior.
- `designs/`: design-before-coding workflow and reusable design doc template.
- `diagrams/`: C4, sequence, and use-case diagrams.
- `reference/`: concrete API, metrics, and plugin contracts.
- `operations/`: deployment, troubleshooting, and runbooks.
- `examples/`: sample dashboard definitions and other reusable documentation artifacts.
- `adr/`: architecture decision records, including historical decisions.

## Documentation Standards

- Architecture docs and logical C4 diagrams use role names such as Control Plane, Probe Runtime, Metric Snapshot Store, Metrics Collector, Dashboard Service, and Site Network Targets.
- Concrete runtimes, frameworks, databases, observability products, ports, and local commands belong in reference, operations, deployment, and ADR docs.
- Sequence diagrams describe behavior and contracts. They should not mirror internal class calls unless the purpose is explicitly to document implementation internals.
- Any change to component boundaries, API contracts, metric names, plugin manifests, or deployment assumptions must update docs and ADRs in the same change.

## Historical Notes

- ADRs `ADR-004`, `ADR-005`, and `ADR-006` document the pre-current implementation direction and are kept for decision history.
- ADRs `ADR-001`, `ADR-009`, and `ADR-010` are historical and point to newer runtime and monitoring decisions.
