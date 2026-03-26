# ADR-004: Use Python for the central server

|Metadata|Value|
|--------|-----|
|Date|[2026-03-26]|
|Status|Proposed|
|Tags|beacon, central-server, language, python|

## Context
BEACON's central server must implement a GraphQL API, manage probe and plugin configuration in a relational database, write Prometheus file-based service discovery targets on probe registration, and serve both an Admin client and N Raspberry Pi probe agents. The team is small and working within an internship timeline, so language choice directly affects how much boilerplate must be written versus how quickly domain logic can be expressed. The probe agents are already Python on Raspberry Pi OS, meaning team familiarity is concentrated in one language. The server also needs to interact with the filesystem (writing Prometheus SD target files) and orchestrate background tasks such as heartbeat staleness checks, both of which are routine in Python. The decision centred on whether a different language would offer enough architectural advantage to justify the context-switching cost for a small team.

## Decision
BEACON's central server is implemented in Python. The GraphQL layer uses Strawberry, the database layer uses SQLAlchemy with Alembic for migrations, and background tasks use APScheduler or a lightweight asyncio loop. The server runs as a single process under an ASGI server (Uvicorn), which provides adequate concurrency for the expected load of N probes polling every 30-60 seconds plus occasional Admin interactions.

## Alternatives considered

### 1. Node.js / TypeScript
How it would work: The GraphQL API would use Apollo Server or Pothos with Prisma as the ORM. TypeScript provides strong typing across resolvers and schema. Node.js handles concurrent I/O well natively.

Why it was rejected: The team's primary experience is Python, and the probe agents are already Python. Splitting the codebase across two runtimes increases context-switching and tooling overhead without solving any bottleneck BEACON actually has. Apollo Server's configuration surface is also significantly larger than Strawberry's for a project of this scale.

### 2. Go
How it would work: gqlgen generates type-safe GraphQL resolvers from a schema file. Go's compiled binary deploys cleanly and has excellent concurrency primitives.

Why it was rejected: Go's verbosity and unfamiliarity to the team creates a steep ramp on an internship timeline. The performance headroom Go offers over Python is irrelevant at BEACON's probe count and polling frequency. The ecosystem for rapid schema iteration is also thinner than Python's.

### 3. FastAPI with a REST API (no GraphQL)
How it would work: Python is kept but GraphQL is dropped in favour of FastAPI's automatic OpenAPI/REST generation. Each resource gets standard CRUD endpoints.

Why it was rejected: This contradicts ADR-003, which justifies GraphQL specifically to serve both a rich Admin client and a narrow probe client from one typed schema without endpoint proliferation. The language choice here is orthogonal to the API style decision already made.

## Consequences

### Positive
- Single language across probe agents and central server reduces the team's cognitive load and enables code reuse for shared models and constants.
- Strawberry's code-first schema definition means Python dataclasses drive both the GraphQL type system and internal models, reducing duplication.
- Python's filesystem and subprocess APIs are mature and well-documented for writing Prometheus SD files and invoking system utilities.
- Rich ecosystem of libraries for scheduling (APScheduler), secrets handling, and testing (pytest) accelerates development.

### Negative
- Python's GIL limits true parallelism; under high probe counts the ASGI concurrency model must be understood to avoid blocking the event loop on database calls.
- Type safety is enforced by convention and tooling (mypy, Strawberry's type system) rather than the compiler, requiring discipline to maintain as the schema grows.
- Deployment requires a Python runtime environment on the server host, unlike a compiled binary.

## Related decisions
This decision enables ADR-005 by determining which GraphQL library is available. It also constrains ADR-006 because ORM and migration tooling choices are scoped to the Python ecosystem.