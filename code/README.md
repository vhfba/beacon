# Code Workspace

This folder contains runtime code separated by component:

- `central-server/`: (Legacy) Java 21/Spring Boot API, configuration store, and orchestration logic. **[Deprecated]**
  - See [ADR-004](../docs/adr/ADR-004-use-java-spring-boot-for-the-central-server.md) for decision history
  
- `dotnet-central-server/`: **.NET 9 / C# implementation** - New central server (recommended)
  - Optimized for Linux containers, minimal resource footprint
  - HotChocolate GraphQL, Entity Framework Core, PostgreSQL
  - See [ADR-007](../docs/adr/ADR-007-use-dotnet-9-with-csharp-for-the-central-server.md) for migration rationale
  - [Quick Start Guide](dotnet-central-server/QUICKSTART.md)

- `probe-agent/`: lightweight runtime for Raspberry Pi probes.

- `monitoring-stack/`: deployment artifacts for Prometheus/Grafana/Alertmanager.

## Migration Status

The BEACON platform is transitioning from Java/Spring Boot to .NET 9 for the central server component. Both implementations currently coexist for reference and gradual migration purposes.

**Use `dotnet-central-server/` for new deployments and development.**
