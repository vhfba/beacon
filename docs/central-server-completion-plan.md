# BEACON Central Server Completion Plan

## Purpose
This document maps the intended project objective (from ADR-001/002/003 and architecture diagrams) to the current .NET central server implementation and identifies what is still required for a "complete" central control plane.

## Objective Summary
The central server should provide:
- GraphQL control plane for Admin and Probe clients
- Probe registry and lifecycle management
- Probe test configuration management (enable/disable + intervals)
- Plugin repository metadata and distribution support
- Operational readiness for production deployment

## Scope Coverage Matrix

| Capability | Expected by docs/ADRs | Current status | Notes |
|---|---|---|---|
| Register probes | Yes (use cases + GraphQL) | Implemented | Available via registerProbe mutation |
| View fleet status | Yes | Implemented | Available via fleetStatus query |
| Update probe status | Yes | Implemented | Available via updateProbeStatus mutation |
| Configure test intervals and enablement | Yes | Implemented | updateProbeTestConfig + setProbeTestEnabled |
| Probe polling for configuration | Yes | Partially implemented | Probe config includes tests/intervals; plugin distribution details were missing |
| Plugin metadata registration | Yes | Implemented | registerPlugin/listPlugins/getPluginById |
| Plugin bundle download/distribution | Yes (diagram: fetch plugin/download bundle) | Missing before this phase | No server endpoint for bundle retrieval existed |
| Role-based auth/authorization | Yes (ADR-003 consequence text) | Missing | No auth middleware/policies enforced yet |
| Monitoring integration endpoints | Cross-system objective | Partially implemented | Health endpoint present; monitoring stack still placeholder |
| Production hardening baseline | Yes | Partially implemented | Compose and release build were hardened in prior iteration |

## Gaps To Close

### Priority 1 (implemented in this phase)
- Add plugin distribution primitives:
  - Include available plugin metadata in probeConfig response
  - Expose a bundle download endpoint for probes

### Priority 2 (recommended next)
- Add role-based API auth for Admin vs Probe operations:
  - Restrict admin mutations and fleet-wide reads
  - Keep probeConfig and plugin bundle retrieval available to probe identity

### Priority 3 (recommended next)
- Add automated validation coverage:
  - Integration tests for GraphQL workflows
  - Smoke test for migrations + health + plugin bundle endpoint

## Definition Of Done (Central Server)
A practical completion baseline for the central server is:
1. All documented use cases are available through stable API endpoints.
2. Probe poll flow returns both test config and plugin distribution metadata.
3. Probes can download plugin bundles from the central server.
4. Auth policies are enforced by client role.
5. Deployment path is reproducible with Docker Compose and documented verification steps.
6. Minimal automated tests cover critical paths.

## Implementation Log
- Phase 1: Plugin distribution support (current change set)
  - Added plugin metadata in probeConfig response
  - Added plugin bundle download endpoint
  - Added plugin bundle conventions/documentation
