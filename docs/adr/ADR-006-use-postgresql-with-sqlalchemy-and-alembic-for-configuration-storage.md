# ADR-006: Use PostgreSQL with SQLAlchemy and Alembic for configuration storage

|Metadata|Value|
|--------|-----|
|Date|[2026-03-26]|
|Status|Proposed|
|Tags|beacon, database, postgresql, sqlalchemy, alembic|

## Context
BEACON's central server needs a persistent store for probe registry records, per-probe test configuration, plugin manifests and checksums, and API keys. The data model is relational: a probe has one configuration record, a configuration record references one or more plugin versions, and plugin versions reference bundle metadata. The team considered both file-based and server-based database options. Key constraints are: the schema will evolve during the project as new test types and plugin fields are added, the dataset is small (tens to low hundreds of probes), concurrent writes are low (one write per probe registration or admin mutation), and the deployment target is a single central server the team controls. SQLite was the primary alternative to PostgreSQL, and the choice between them is the most consequential part of this decision.

## Decision
BEACON uses PostgreSQL as the relational database, accessed through SQLAlchemy (async via asyncpg driver) with Alembic managing schema migrations. PostgreSQL runs as a system service on the central server alongside the GraphQL API process. SQLAlchemy models are defined as Python dataclasses using the MappedColumn API introduced in SQLAlchemy 2.0, which aligns with Strawberry's dataclass-based types and minimises translation between the ORM and GraphQL layers. Alembic autogenerates migration scripts from model changes and applies them at deploy time, keeping schema evolution auditable and reversible.

## Alternatives considered

### 1. SQLite
How it would work: The database is a single file on the server filesystem. SQLAlchemy connects to it with the same ORM interface, so application code barely changes. No separate database process is needed.

Why it was rejected: SQLite's write concurrency model (one writer at a time, file-level locking) becomes a bottleneck if probe registration and config poll writes ever overlap under load. More practically, SQLite's async support via aiosqlite is less mature than asyncpg, and WAL mode requires careful configuration to avoid corruption under concurrent async writes. PostgreSQL is operationally straightforward to deploy on a single server and removes these constraints entirely without meaningful added complexity at BEACON's scale. SQLite remains a valid option for a purely local development environment and is used as such.

### 2. MongoDB
How it would work: Probe records and plugin manifests are stored as documents. Schema evolution is handled by the application rather than migrations. The Python driver (motor) supports async natively.

Why it was rejected: BEACON's data model is inherently relational - probe-to-config and config-to-plugin relationships benefit from foreign key constraints and join semantics. A document store trades those guarantees for schema flexibility that the project does not need, since plugin manifests and probe configs are well-defined and stable structures. Introducing a non-relational store also adds operational complexity (separate MongoDB service, different query model) without a corresponding benefit.

### 3. Direct SQL with psycopg3 (no ORM)
How it would work: SQL strings are written directly, results are mapped to Python dicts or dataclasses manually. Schema migrations are plain .sql files applied by a shell script.

Why it was rejected: At BEACON's schema complexity, hand-written SQL is manageable, but the absence of an ORM means Strawberry resolver code must manually map query results to Python types. SQLAlchemy 2.0's MappedColumn API produces typed model classes that map directly to Strawberry types, eliminating a translation layer. Alembic's autogenerate is also significantly more reliable than manual SQL migration scripts for a team iterating on the schema during development.

### 4. SQLite for development, PostgreSQL for production (split environments)
How it would work: SQLite is used locally for fast iteration and PostgreSQL is used in the deployed environment. SQLAlchemy's dialect abstraction means application code is identical in both cases.

Why it was not rejected entirely: This is the adopted pattern. SQLite is used for local development and unit tests via an in-memory database. PostgreSQL is the target for the deployed central server. This is noted as a consequence rather than a rejected alternative because it is part of the decision, not an alternative to it.

## Consequences

### Positive
- PostgreSQL's ACID guarantees and row-level locking handle concurrent probe registrations and admin mutations correctly without application-level coordination.
- SQLAlchemy 2.0's typed MappedColumn API produces Python classes that map cleanly to Strawberry types, minimising boilerplate between the ORM and GraphQL layers.
- Alembic autogenerate creates auditable, reversible migration scripts from model diffs, making schema evolution safe on an active deployment.
- PostgreSQL's asyncpg driver is the most mature and performant async PostgreSQL driver for Python, suitable for Uvicorn's async event loop.
- Local development uses SQLite in-memory via the same SQLAlchemy interface, keeping test suite setup fast and dependency-free.

### Negative
- PostgreSQL requires a running database service on the central server, adding an operational dependency compared to SQLite.
- The team must manage connection pooling configuration (asyncpg pool size) to avoid exhausting connections under bursty probe activity.
- Alembic autogenerate does not detect all schema changes (for example, column constraint modifications) and requires manual review of generated migration scripts before applying them.

## Related decisions
Depends on ADR-004 (Python) for ORM and driver ecosystem compatibility. Resolvers in ADR-005 (Strawberry) call SQLAlchemy sessions to fulfil queries and mutations. The probe registration flow in UC1 writes a new row to the probe registry table and triggers a Prometheus SD file update in the same transaction boundary.