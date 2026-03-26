# ADR-005: Use Strawberry for the GraphQL layer

|Metadata|Value|
|--------|-----|
|Date|[2026-03-26]|
|Status|Proposed|
|Tags|beacon, graphql, strawberry, python|

## Context
Given ADR-003's decision to use GraphQL and ADR-004's decision to use Python, the team must select a specific Python GraphQL library. The options differ significantly in their approach to schema definition: some are schema-first (write SDL, generate resolvers), others are code-first (write Python, derive the schema). BEACON's schema includes probe registry types, plugin manifest types, and configuration mutation inputs - a moderate surface that will evolve during the project. The team also needs the library to integrate naturally with Python dataclasses and type annotations, since Strawberry's type system and the SQLAlchemy models must coexist without excessive translation layers.

## Decision
BEACON uses Strawberry as the GraphQL library. Strawberry is code-first: Python classes decorated with @strawberry.type and @strawberry.input define the schema, and resolver functions are standard Python methods. It integrates directly with ASGI via strawberry.asgi.GraphQL, runs over Uvicorn, and supports async resolvers natively. Authentication middleware sits in front of the ASGI app and injects probe or admin identity into the Strawberry context object, which resolvers access without coupling auth logic to business logic.

## Alternatives considered

### 1. Ariadne
How it would work: Schema is defined in SDL files, and Python resolver functions are bound to types by name at startup. Admin and probe queries map to separate resolver modules.

Why it was rejected: SDL-first development splits the source of truth across .graphql files and Python functions, which increases the maintenance surface during rapid iteration. When a field changes, both the SDL and the resolver binding must be updated in sync. For a project iterating quickly within an internship, code-first keeps everything in one place.

### 2. Graphene
How it would work: Python classes inherit from graphene.ObjectType, and resolvers are methods on those classes. Graphene has a large existing user base and many examples.

Why it was rejected: Graphene's API predates Python's modern type annotation system and does not use dataclasses or typing natively. The result is verbose class definitions that diverge from the rest of the codebase's style. Strawberry was designed specifically for modern Python and integrates cleanly with dataclasses, typing, and pydantic - all of which BEACON's models already use.

### 3. Strawberry with a code-generated schema exported to SDL
How it would work: Strawberry is used code-first but the generated SDL is exported and committed to the repository for documentation and contract validation purposes.

Why it was not a separate alternative: This is a usage pattern within Strawberry, not a competing library. BEACON will adopt this practice as part of its development workflow regardless.

## Consequences

### Positive
- Code-first schema definition keeps types, resolvers, and validation logic colocated in Python, reducing the number of files that must change together.
- Native async resolver support means probe config polling and database queries can be awaited without blocking the event loop.
- Strawberry's context mechanism provides a clean injection point for authentication identity, keeping resolvers free of auth boilerplate.
- Type annotations on resolver return values are checked by mypy and validated at startup, catching schema drift early.
- Lightweight footprint - Strawberry adds minimal dependencies beyond the standard ASGI stack.

### Negative
- Strawberry is younger than Graphene and Ariadne; some advanced features (subscriptions, federation) have a smaller body of community examples to draw from.
- Code-first means the SDL is derived, not primary - tooling that expects SDL as input (some client code generators) requires an extra export step.
- Debugging complex resolver chains requires familiarity with Python async tracebacks, which can be less readable than synchronous stacks.

## Related decisions
Depends on ADR-003 (GraphQL API decision) and ADR-004 (Python language decision). Influences ADR-006 because Strawberry resolvers call SQLAlchemy sessions directly, so the ORM choice affects how async database access is structured inside resolvers.