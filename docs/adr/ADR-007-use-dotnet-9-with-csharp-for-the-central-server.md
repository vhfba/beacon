# ADR-007: Use .NET 9 with C# for the central server

|Metadata|Value|
|--------|-----|
|Date|[2026-04-01]|
|Status|Accepted|
|Supersedes|ADR-004|
|Tags|beacon, central-server, language, dotnet, csharp, linux|

## Context
BEACON's central server requires a control plane that is highly optimized for Linux container deployment, with minimal resource footprint and strong cross-platform support. After production experience with the Java/Spring Boot implementation, the team identified that the JVM overhead (memory usage, startup time) and operational complexity create friction in cloud-native environments. .NET 9 provides a lightweight, AOT-compilable alternative with native Linux support, strong type safety, and a modern ecosystem stack equivalent to or superior to the Spring ecosystem. The transition will enable more efficient containerization, faster deployment cycles, and reduced infrastructure costs.

## Decision
BEACON's central server will be reimplemented in C# using .NET 9, deployed as a native AOT application or containerized application optimized for Linux execution. The service will maintain the same layered Onion/Clean architecture (domain, application, infrastructure, presentation) and will use ASP.NET Core for the web API and GraphQL implementation. The server will run as a standalone .NET application and expose GraphQL endpoints for both Admin and probe-agent clients.

## Alternatives considered

### 1. Continue with Java 21 / Spring Boot
How it would work: Maintain the existing JVM-based implementation with iterative optimizations to reduce memory footprint.

Why it was rejected: Despite improvements, the JVM carries inherent overhead (minimum ~100-200MB memory baseline) that impacts multi-instance deployments in resource-constrained cloud environments. Native AOT solutions are limited within the Java ecosystem. Operational complexity remains higher than .NET for containerized workloads.

### 2. Go
How it would work: Reimplement in Go with similar layered architecture using a Go web framework and GraphQL library.

Why it was rejected: While Go excels at simple services, the team requires stronger domain-driven design patterns, dependency injection frameworks, and enterprise-grade layering that are more naturally expressed in C# and .NET. Go's ecosystem for complex business logic and ORM is comparatively weaker.

### 3. Python with async/await (FastAPI, Strawberry)
How it would work: Implement with FastAPI and Strawberry GraphQL for rapid iteration and ecosystem consistency with probe-agent.

Why it was rejected: The production demands for the control plane (transaction handling, type safety, performance under load, deployment efficiency) are better served by compiled languages. Python's GIL and lack of true parallelism create bottlenecks for concurrent workloads.

## Consequences

### Positive
- Native AOT compilation eliminates JVM warmup, enabling sub-second startup times and minimal memory footprint (10-50MB baseline), ideal for Kubernetes autoscaling.
- .NET 9 provides synchronous compilation, modern language features (nullable reference types, pattern matching, async/await as first-class), and strong type safety matching or exceeding Java.
- ASP.NET Core is production-ready with built-in middleware, dependency injection, and options for both traditional and minimal APIs.
- Entity Framework Core offers LINQ-based queries and migrations with similar expressiveness to Spring Data JPA.
- Excellent Docker support with multi-stage builds and optimized base images; native Linux deployment is first-class.
- Single unified platform (C#/.NET) across central-server and probe-agent simplifies ecosystem management and developer experience.
- Cost reduction through smaller container images, faster deployments, and reduced memory requirements in production.

### Negative
- Requires team retraining on C# and .NET ecosystem if not already familiar.
- Transition involves complete rewrite of codebase (not incremental migration path from Java).
- Ecosystem for some specialized libraries may be smaller than Java, though core requirements (ORM, GraphQL, database drivers) are well-supported.
- Existing Spring Boot production experience must be revalidated in .NET context (performance, reliability, monitoring).

### Migration Impact
- Domain models and business logic will be re-expressed from Java to C# with similar patterns.
- GraphQL schema and API contracts remain unchanged; external interfaces are preserved.
- Database schema remains compatible; Entity Framework migrations will replace Flyway.
- Deployment model changes: AOT executable or lightweight container replaces WAR on traditional app server.
- Operational monitoring and observability tooling must be reconfigured for .NET telemetry (OpenTelemetry support available).

## Related decisions
- This decision enables a streamlined deployment model for cloud-native operations.
- Probe-agent may align with .NET ecosystem if C# adoption extends across BEACON components (future consideration).
- ADR-005 and ADR-006 requirements are re-evaluated for .NET equivalents (ASP.NET Core GraphQL and Entity Framework Core + SQL migrations).
