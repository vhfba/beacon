# ADR-008: Use HotChocolate GraphQL for the .NET central server

|Metadata|Value|
|--------|-----|
|Date|[2026-04-01]|
|Status|Accepted|
|Supersedes|ADR-005|
|Tags|beacon, graphql, hotchocolate, dotnet, csharp|

## Context
Given ADR-003's decision to use GraphQL and ADR-007's transition to .NET 9 with C#, BEACON requires a GraphQL framework that integrates naturally with ASP.NET Core's dependency injection, Entity Framework, and middleware patterns. The framework must support both Admin and probe-agent access patterns, provide robust schema validation, and enable evolving the API safely. HotChocolate (by ChilliCream) is the most mature and feature-rich GraphQL implementation for .NET 9.

## Decision
BEACON uses HotChocolate for the GraphQL layer. The schema is defined declaratively using C# classes with HotChocolate attributes or through code-first SDL generation. Resolvers are implemented as methods on query and mutation types with full dependency injection support from ASP.NET Core. The GraphQL endpoint is integrated into the ASP.NET Core middleware pipeline, enabling seamless use of filters, authentication, and middleware across the API. Subscriptions and advanced features (batching, complexity analysis, persisted queries) are available when needed.

## Alternatives considered

### 1. Strawberry (for .NET)
How it would work: While Strawberry exists primarily for Python, exploring a hypothetical .NET port or similar decorator-style GraphQL library.

Why it was rejected: No mature .NET equivalent exists. HotChocolate provides equivalent or superior ergonomics with deeper integration into the .NET ecosystem and ASP.NET Core conventions.

### 2. Graphene/.NET (minimal GraphQL library)
How it would work: Use a lightweight, low-level GraphQL library and implement resolvers and schema management manually.

Why it was rejected: Manual schema and resolver wiring increases maintenance burden. HotChocolate provides substantial value through automation, validation, and built-in features without significant complexity overhead.

### 3. Custom REST API (abandoning GraphQL)
How it would work: Replace GraphQL entirely with RESTful endpoints optimized for Admin and probe-agent workflows.

Why it was rejected: Conflicts with ADR-003's foundational decision to use GraphQL. REST endpoints would proliferate and diverge between clients, losing the benefits of a unified query language.

## Consequences

### Positive
- Mature, feature-rich framework actively maintained by ChilliCream with strong .NET community adoption.
- Seamless integration with ASP.NET Core dependency injection, middleware, and Entity Framework Core for data access.
- Code-first schema definition with C# types provides compile-time type safety and IntelliSense support.
- Built-in support for complex features: subscriptions, filtering, sorting, pagination, field middleware, authorization directives.
- Excellent documentation, community support, and regular updates aligned with .NET release cycles.
- SDL export and introspection enable client tooling and external integrations without manual schema management.
- Performance is competitive with or exceeds Spring GraphQL and other implementations.

### Negative
- Team must learn HotChocolate's conventions and patterns if unfamiliar with the library.
- Code-first approach in C# means schema lives in code rather than SDL files, requiring discipline to keep schema and resolvers cohesive.
- Ecosystem of third-party HotChocolate extensions is smaller than Java's Spring ecosystem, though core capabilities are built-in.

### Migration Impact
- GraphQL schema contract (/graphql endpoint) remains unchanged; clients connect identically.
- Resolver implementations are rewritten from Java/Spring to C#/HotChocolate using pattern-matching and async/await.
- Query performance and subscription behavior are re-validated in production load testing.
- Apollo Studio and other GraphQL tooling remain compatible through standard introspection.

## Related decisions
- Depends on ADR-003 (GraphQL API decision) and ADR-007 (.NET 9 platform decision).
- Influences persistence and transaction handling decisions; Entity Framework Core resolvers interact cleanly with HotChocolate's field resolver model.
- Monitoring and observability integrate with OpenTelemetry for GraphQL tracing and performance analysis.
