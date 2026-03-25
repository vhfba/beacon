# ADR-002: Use a plugin-based architecture for probe test execution

|Metadata|Value|
|--------|-----|
|Date|[2026-03-20]|
|Status|Proposed|
|Tags|beacon, probe-agent, plugins, testing|

## Context
BEACON probes must run multiple test modalities: passive RSSI scanning plus active ping, HTTP, and iPerf checks, each with different execution patterns and dependencies. The project deploys N Raspberry Pis in a building, so distributing full agent upgrades for every test change would create repeated operational overhead and downtime risk. Hardware constraints on Raspberry Pi OS make it important to keep a single long-lived agent process predictable while still allowing test-level evolution. The central server already maintains configuration state and can host plugin metadata, which creates a natural control plane for test activation and versioning. The team also needs to iterate quickly within an internship timeline, where new test logic may appear after initial deployment. The decision therefore centered on whether tests should be static inside the agent binary/package or dynamically loaded through a managed extension mechanism.

## Decision
BEACON probe agents execute tests through a plugin loader that discovers and loads test modules at runtime based on centrally managed configuration. The scheduler triggers plugin execution on configured intervals, and each plugin returns normalized result objects that the formatter maps into Prometheus metrics. A central Plugin Repository stores plugin bundles and manifests (name, version, checksum), allowing probes to fetch updates without reinstalling the whole agent. The loader enforces a plugin interface contract so RSSI, ping, HTTP, and iPerf implementations can vary internally while exposing consistent output fields. This keeps the core agent stable and small, while enabling targeted rollout of new tests or fixes across distributed probes.

## Alternatives considered

### 1. Hardcoded test modules in the agent
How it would concretely work inside BEACON: The agent package would include all supported tests, and adding or changing a test would require releasing and redeploying the complete agent to each Raspberry Pi. Configuration would only toggle built-in modules on or off, with no runtime extension mechanism.

Why it was rejected: In building-scale deployment, full agent redeployment for small test changes creates avoidable operational friction and higher risk of inconsistent versions across probes. It couples unrelated concerns: a bug fix in one test forces redistribution of all code. This slows experimentation with network diagnostics during field learning cycles.

### 2. Scripting interface with shell scripts
How it would concretely work inside BEACON: The agent would invoke external shell scripts for each test type and parse stdout/stderr into metric records. Plugin updates would be script file replacements distributed from the central server or manual copy.

Why it was rejected: Script execution increases variability across probes due to shell differences, dependency path issues, and quoting/escaping edge cases on Raspberry Pi OS. Parsing text output into structured metrics is brittle under failure conditions and makes error classification inconsistent. Security posture is weaker because arbitrary script content is harder to validate and constrain than a typed plugin contract.

### 3. Separate process per test type
How it would concretely work inside BEACON: RSSI, ping, HTTP, and iPerf would each run as independent processes or services, coordinated by IPC from a supervising component. Each process would publish its own status and results, and the agent would aggregate them.

Why it was rejected: Multiprocess orchestration adds lifecycle complexity (startup order, IPC protocol, crash recovery) beyond what BEACON needs at current scale. On constrained Pis, extra resident processes increase memory footprint and context switching overhead. Debugging production issues becomes harder because failures can be in process coordination rather than test logic.

### 4. Remote code execution triggered directly from the server
How it would concretely work inside BEACON: The central server would send executable test payloads or commands to probes on demand, and probes would run them immediately without local plugin version management. Test behavior would be driven directly by remote instructions at execution time.

Why it was rejected: This model introduces high operational and security risk by making the control plane also the code-delivery channel for live execution. It requires strict sandboxing, auditing, and rollback controls that exceed the project's implementation scope. Intermittent connectivity also makes command-delivery guarantees difficult, causing inconsistent probe behavior across the building.

## Consequences

### Positive
- New test capabilities can be rolled out without replacing the core agent, reducing update friction across many Raspberry Pis.
- The plugin contract standardizes outputs, which simplifies metric formatting and downstream Prometheus queries.
- The central Plugin Repository enables controlled versioning and checksum validation for safer distributed updates.
- Core agent reliability improves because scheduler, loader, and formatter remain stable while test logic evolves independently.
- Team velocity improves during short project timelines because test development and agent platform maintenance are decoupled.

### Negative
- Plugin API/version compatibility must be managed explicitly, including migration rules when interfaces evolve.
- Runtime loading adds failure paths (missing plugin, bad checksum, load error) that require robust observability and fallback behavior.
- Plugin isolation is limited unless additional sandboxing is implemented, so faulty plugins can still affect agent stability.
- Repository governance is needed to prevent unreviewed or conflicting plugin versions from reaching production probes.

## Related decisions
This decision depends on ADR-003 because GraphQL is used to distribute enabled plugin sets and schedule intervals to each probe. It also supports ADR-001 because plugin outputs are transformed into stable Prometheus metrics exposed on the probe `/metrics` endpoint.
