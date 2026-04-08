# ADR-005: Use Spring GraphQL for the GraphQL layer

|Metadata|Value|
|--------|-----|
|Date|[2026-03-26]|
|Status|Declined|
|Superseded by|ADR-008|
|Tags|beacon, graphql, spring-graphql, java|

## Context
Given ADR-003's decision to use GraphQL and ADR-004's decision to implement the server with Spring Boot on Java, BEACON needed a GraphQL framework that integrates naturally with Spring's dependency injection, transaction boundaries, and validation model. The API must support both Admin and probe-agent access patterns while remaining easy to evolve and test. Schema governance and resolver wiring should stay close to standard Spring conventions.

## Decision
BEACON uses Spring GraphQL (`spring-boot-starter-graphql`) as the GraphQL framework. The schema is maintained in SDL files and mapped to resolver/controller methods using Spring annotations. The GraphQL endpoint is exposed through Spring Web MVC integration, and exception handling is centralized via GraphQL exception resolvers. This keeps GraphQL wiring aligned with the rest of the Spring application model.

## Alternatives considered

### 1. GraphQL Java without Spring GraphQL integration
How it would work: The service would wire GraphQL Java components manually and manage runtime wiring and HTTP integration directly.

Why it was rejected: Manual setup increases boilerplate and maintenance burden compared with Spring GraphQL's built-in endpoint, schema loading, and resolver integration.

### 2. REST-only API (no GraphQL)
How it would work: Replace GraphQL with multiple REST endpoints for Admin and probe-agent operations.

Why it was rejected: It conflicts with ADR-003 and would increase endpoint proliferation for mixed client needs.

### 3. Netflix DGS framework
How it would work: Use DGS on top of Spring for GraphQL schema and resolver development.

Why it was rejected: DGS would be a valid option, but Spring GraphQL offered a more direct path with fewer additional framework conventions for this project scope.

## Consequences

### Positive
- Native integration with Spring Boot reduces setup complexity and aligns GraphQL with existing app lifecycle and configuration.
- SDL-based schema keeps the contract explicit and easy to share across clients.
- Resolver methods use familiar Spring patterns for dependency injection and validation.
- Standardized exception handling and instrumentation are easier to integrate in the Spring ecosystem.

### Negative
- Teams must maintain SDL and resolver mappings in sync.
- Some advanced GraphQL UX features (such as GraphiQL plugin integrations) may depend on external assets if default tooling is used.

## Related decisions
Depends on ADR-003 (GraphQL API decision) and ADR-004 (Java/Spring language-platform decision). Influences ADR-006 because resolver implementations depend on JPA repositories and transaction boundaries.

## Revision history
- **2026-04-01**: Status changed to Declined. The central server platform has been transitioned to .NET 9 (ADR-007), requiring a new GraphQL framework decision. See ADR-008 for the replacement approach.