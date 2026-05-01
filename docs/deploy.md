# Deployment Guide

This guide covers the current local deployment model for:

- `code/central-server`
- `code/monitoring-stack`
- optionally `code/probe-agent`

## Architecture In Deployment Terms

- `central-server` runs ASP.NET Core, PostgreSQL, and Redis-backed metric aggregation.
- `probe-agent` talks to central-server over GraphQL and downloads plugin bundles over HTTP.
- `monitoring-stack` scrapes only central-server `/metrics` and renders dashboards in Grafana.

## Prerequisites

- Docker Desktop or Docker Engine with Compose v2
- .NET 9 SDK if you want to run central-server outside Docker
- Python 3 if you want to run probe-agent locally

Default host ports:

- central-server: `5000`
- Prometheus: `9090`
- Grafana: `3000` or whatever you configure in `.env`

## 1. Configure Central Server

Files:

- template: [code/central-server/.env.example](/C:/Users/joaom/Faculdade/beacon/code/central-server/.env.example)
- local file: `code/central-server/.env`

Minimum environment values:

```dotenv
POSTGRES_DB=beacon_central
POSTGRES_USER=beacon
POSTGRES_PASSWORD=<strong-password>
CENTRAL_SERVER_PORT=5000
AUTH_ADMIN_API_KEY=<admin-api-key>
AUTH_PROBE_API_KEY=<probe-api-key>
GRAFANA_EMBED_BASE_URL=http://localhost:3000
GRAFANA_API_BASE_URL=http://host.docker.internal:3000
GRAFANA_DASHBOARD_BASE_UID=beacon-probe-health
GRAFANA_API_TOKEN=
```

Current compose behavior:

- PostgreSQL is required.
- Redis is required for metric snapshot storage.
- GraphQL introspection is disabled in the container by default.
- Plugin bundles are mounted from `code/central-server/plugin-bundles`.

## 2. Start Central Server

From `code/central-server`:

```powershell
docker compose up -d --build
```

Verify:

```powershell
docker compose ps
curl http://localhost:5000/health
```

Expected response:

```json
{ "status": "healthy" }
```

Optional metrics check:

```powershell
curl http://localhost:5000/metrics
```

## 3. Configure Monitoring Stack

Files:

- template: [code/monitoring-stack/.env.example](/C:/Users/joaom/Faculdade/beacon/code/monitoring-stack/.env.example)
- local file: `code/monitoring-stack/.env`

Example:

```dotenv
PROMETHEUS_PORT=9090
PROMETHEUS_RETENTION=7d
GRAFANA_PORT=3000
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=change-me-locally
```

Keep these aligned with central-server:

- `GRAFANA_EMBED_BASE_URL`
- `GRAFANA_API_BASE_URL`

## 4. Start Monitoring Stack

From `code/monitoring-stack`:

```powershell
docker compose up -d
```

Verify:

```powershell
docker compose ps
curl http://localhost:9090/-/healthy
curl http://localhost:3000
```

## 5. Optional: Run Probe Agent

Create `code/probe-agent/.env` with:

```dotenv
PROBE_ID=probe-mock-01
CENTRAL_SERVER_BASE_URL=http://localhost:5000
CENTRAL_SERVER_PROBE_API_KEY=<same-as-AUTH_PROBE_API_KEY>
```

Install and run:

```powershell
pip install -r requirements.txt
python mock_probe_agent.py
```

## 6. Seed A Working Scenario

Recommended order:

1. Build plugin bundles from `code/probe-agent`.
2. Start central-server.
3. Register plugins through GraphQL.
4. Register a probe.
5. Assign plugins with `setProbePlugins`.
6. Enable scheduled tests with `updateProbeTestConfig`.
7. Start probe-agent.
8. Confirm probe heartbeat, action polling, and metrics reporting.
9. Start monitoring-stack and verify Grafana panels.

## 7. Verify End To End

### Central server

- `GET /health` returns `200`.
- `GET /metrics` returns Prometheus text.
- `POST /graphql` works with `X-Api-Key`.

### Probe workflow

- `recordProbeHeartbeat` succeeds.
- `probeConfig` returns enabled tests and plugins.
- `reportProbeMetrics` accepts samples.
- `pendingProbeActions` returns queued work when actions are triggered.

### Monitoring workflow

- Prometheus target `beacon-central-server` is `UP`.
- Queries return BEACON metrics with `probe_id` labels.
- Grafana dashboard `beacon-probe-health` renders data.

## 8. Troubleshooting

- No metrics in Prometheus:
  - Confirm Redis is running inside `code/central-server/docker-compose.yml`.
  - Confirm the probe is calling `reportProbeMetrics`.
  - Confirm `/metrics` on central-server returns samples.

- Probe cannot download plugins:
  - Confirm bundles exist in `code/central-server/plugin-bundles`.
  - Confirm the plugin is registered and `available=true`.
  - Confirm the probe uses the probe API key.

- Probe heartbeat works but nothing runs:
  - Confirm plugins are assigned with `setProbePlugins`.
  - Confirm scheduled plugins also have `updateProbeTestConfig` entries.
  - Confirm action plugins are queued with `triggerProbeAction`.

- Grafana embed links are wrong:
  - Recheck `GRAFANA_EMBED_BASE_URL`.
  - Recheck `GRAFANA_API_BASE_URL` for container-to-host access.

## 9. Stop The Stack

Central server:

```powershell
cd code/central-server
docker compose down
```

Monitoring stack:

```powershell
cd code/monitoring-stack
docker compose down
```

Remove persisted volumes when you want a clean reset:

```powershell
docker compose down -v
```
