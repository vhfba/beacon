# ADR-004: Use Java 21 with Spring Boot for the central server

|Metadata|Value|
|--------|-----|
|Date|[2026-03-26]|
|Status|Declined|
|Superseded by|ADR-007|
|Tags|beacon, central-server, language, java, spring-boot|

## Context
BEACON's central server is a long-running control plane that exposes a GraphQL API, persists probe and plugin configuration, and coordinates both Admin and probe-agent workflows. The implementation must be strongly structured for maintainability, with explicit layer boundaries and robust type-safe tooling for domain and API evolution. Runtime reliability and clear integration with enterprise-ready libraries are important because the service runs continuously and acts as the authoritative source of configuration. Language and framework choice therefore directly affect architecture quality, developer productivity, and operational stability.

## Decision
BEACON's central server is implemented in Java 21 using Spring Boot. The service uses Spring's dependency injection and transaction model as the application foundation, with a layered Onion/Clean architecture in the codebase (domain, application, infrastructure, presentation). The server runs as a Spring Boot application on the JVM and exposes GraphQL endpoints for both Admin and probe-agent clients.

## Alternatives considered

### 1. Python
How it would work: The server would use an ASGI stack with Python-based GraphQL and ORM libraries, keeping a single language with probe-agent runtime.

Why it was rejected: Although productive for rapid prototyping, the team required stronger compile-time guarantees and tighter integration with enterprise-grade layering and transactional patterns for the long-lived control-plane service.

### 2. Node.js / TypeScript
How it would work: The server would use a Node-based GraphQL framework and ORM with TypeScript typing.

Why it was rejected: The additional runtime/toolchain divergence from the selected Java ecosystem reduced consistency with existing architecture and governance expectations for this service.

### 3. Go
How it would work: The server would be implemented as a compiled Go service with GraphQL and SQL integrations.

Why it was rejected: Team familiarity and framework ecosystem fit were weaker for the intended layered architecture compared with Spring Boot.

## Consequences

### Positive
- Strong compile-time typing and mature JVM tooling improve maintainability for a central control-plane service.
- Spring Boot provides production-ready configuration, dependency injection, and operational conventions out of the box.
- Layered architecture is clearer to enforce through package structure and framework integration.
- Broad ecosystem support for persistence, GraphQL, validation, and testing reduces integration risk.

### Negative
- JVM startup and memory overhead are typically higher than lighter runtime alternatives.
- The Java/Spring stack introduces additional framework concepts and configuration depth for contributors new to the ecosystem.

## Related decisions
This decision enables ADR-005 by selecting Spring's GraphQL stack and constrains ADR-006 to Java ORM and migration tooling choices.

## Revision history
- **2026-04-01**: Status changed to Declined. The team has decided to transition the central server to .NET 9 with C# for better Linux compatibility and different operational characteristics. See ADR-007 for rationale.