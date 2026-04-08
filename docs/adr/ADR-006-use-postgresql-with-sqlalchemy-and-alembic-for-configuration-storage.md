# ADR-006: Use PostgreSQL with Spring Data JPA and Flyway for configuration storage

|Metadata|Value|
|--------|-----|
|Date|[2026-03-26]|
|Status|Accepted|
|Tags|beacon, database, postgresql, jpa, flyway, spring-data|

## Context
BEACON's central server needs persistent relational storage for probe registry records, per-probe test configurations, plugin manifests, and operational metadata. The schema evolves over time as test capabilities and admin workflows expand. The implementation stack selected in ADR-004 requires an ORM and migration approach aligned with Spring Boot and Java while still keeping production reliability high and local development straightforward.

## Decision
BEACON uses PostgreSQL as the primary relational database in production. Persistence is implemented via Spring Data JPA (Hibernate) for entity mapping and repository access, and Flyway is used to manage versioned schema migrations. For local development and fast iteration, H2 in-memory database may be used as a convenience profile while keeping PostgreSQL as the reference production target.

## Alternatives considered

### 1. SQLite
How it would work: A file-based database would be used locally and/or in deployment, with minimal operational setup.

Why it was rejected: SQLite is useful for local testing but lacks PostgreSQL's production-grade concurrency and operational characteristics for a central service.

### 2. MongoDB
How it would work: Configuration would be stored in flexible documents without strict relational schema constraints.

Why it was rejected: BEACON's model is relational and benefits from strong constraints, joins, and transactional semantics.

### 3. Plain JDBC with handwritten SQL (no JPA)
How it would work: SQL statements would be hand-managed with explicit row mapping and repository boilerplate.

Why it was rejected: Manual mapping and query management increase boilerplate and maintenance burden compared with Spring Data JPA.

### 4. H2 for development, PostgreSQL for production
How it would work: H2 is used for local development/tests while PostgreSQL is used in deployed environments.

Why it was not rejected entirely: This is the adopted approach for developer ergonomics while preserving a production-grade relational backend.

## Consequences

### Positive
- PostgreSQL provides strong ACID guarantees and robust concurrent access for central control-plane operations.
- Spring Data JPA reduces persistence boilerplate and keeps repositories aligned with domain-driven code organization.
- Flyway versioned migrations provide auditable, repeatable schema evolution.
- Local H2 usage enables fast local bootstrapping and integration testing workflows.

### Negative
- Mapping differences between H2 and PostgreSQL can hide edge-case SQL or dialect issues if tests are not run against PostgreSQL frequently.
- JPA/Hibernate abstraction can generate unexpected SQL patterns unless entities and fetch strategies are tuned carefully.
- Flyway scripts still require review discipline; migration ordering and rollback planning remain operational responsibilities.

## Related decisions
Depends on ADR-004 (Java/Spring platform) for ORM ecosystem compatibility. ADR-005 resolvers use application services that ultimately rely on JPA repositories and transactional persistence. Probe registration and configuration mutation flows persist through the same Spring-managed persistence layer.