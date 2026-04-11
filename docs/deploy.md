# Deployment Guide: Central Server + Monitoring Stack

This guide explains how to deploy and run:

- BEACON central server (ASP.NET Core + PostgreSQL)
- Monitoring stack (Prometheus + Grafana)

for local Docker-based environments.

## 1. Prerequisites

- Docker Desktop (or Docker Engine) with Compose v2
- Ports available on host:
  - central server: `5000`
  - Prometheus: `9090` (default)
  - Grafana: `3000` or `3001` (depending on your local `.env`)

## 2. Configure Central Server Environment

Location:
- `code/central-server/.env`
- template: `code/central-server/.env.example`

Minimum required values:

```dotenv
POSTGRES_DB=beacon_central
POSTGRES_USER=beacon
POSTGRES_PASSWORD=<strong-password>
CENTRAL_SERVER_PORT=5000
AUTH_ADMIN_API_KEY=<admin-api-key>
AUTH_PROBE_API_KEY=<probe-api-key>
PROMETHEUS_SD_TOKEN=<prometheus-sd-token>
PROBE_METRICS_PORT=9464
PROBE_METRICS_PATH=/metrics
GRAFANA_EMBED_BASE_URL=http://localhost:3001
GRAFANA_API_BASE_URL=http://host.docker.internal:3001
GRAFANA_DASHBOARD_BASE_UID=beacon-probe-health
GRAFANA_API_TOKEN=
```

Notes:
- `POSTGRES_PASSWORD`, `AUTH_ADMIN_API_KEY`, `AUTH_PROBE_API_KEY`, and `PROMETHEUS_SD_TOKEN` are required by compose.
- Keep `AUTH_PROBE_API_KEY` aligned with probe-agent `CENTRAL_SERVER_PROBE_API_KEY`.
- `GRAFANA_API_TOKEN` is optional for basic embed flow; needed for API-driven dashboard sync.

## 3. Configure Monitoring Stack Environment

Location:
- `code/monitoring-stack/.env`
- template: `code/monitoring-stack/.env.example`

Example:

```dotenv
PROMETHEUS_PORT=9090
PROMETHEUS_RETENTION=7d
GRAFANA_PORT=3001
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=change-me-locally
```

Recommended alignment with central server:
- If `GRAFANA_PORT=3001`, keep central server:
  - `GRAFANA_EMBED_BASE_URL=http://localhost:3001`
  - `GRAFANA_API_BASE_URL=http://host.docker.internal:3001`

## 4. Start Central Server

From `code/central-server`:

```powershell
docker compose up -d --build
```

Verify:

```bash
docker compose ps
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5000/health
```

Expected health status code: `200`.

## 5. Start Monitoring Stack

From `code/monitoring-stack`:

```powershell
docker compose up -d
```

Verify:

```bash
docker compose ps
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:9090/-/healthy
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:3001
```

Expected status code: `200` for both.

## 6. Validate Integration

### 6.1 Service Discovery from Central Server

Prometheus should call central service discovery:

```bash
curl -s "http://localhost:5000/monitoring/prometheus/service-discovery?token=<PROMETHEUS_SD_TOKEN>"
```

Expected:
- JSON list of scrape targets
- ACTIVE probes only

### 6.2 Prometheus Targets

Open:
- `http://localhost:9090/targets`

Check `beacon-probes` job:
- registered targets appear
- target state is `UP` for reachable probes

### 6.3 Grafana Dashboard

Open:
- `http://localhost:3001`

Then open BEACON dashboard and confirm Prometheus-backed data appears.

## 7. Optional: Probe Agent Runtime Wiring

If you run probe-agent, configure `code/probe-agent/.env`:

```dotenv
CENTRAL_SERVER_BASE_URL=http://localhost:5000
CENTRAL_SERVER_PROBE_API_KEY=<same-as-AUTH_PROBE_API_KEY>
```

This enables runtime-state polling and heartbeat updates to central.

## 8. Common Troubleshooting

- Prometheus shows no probe targets:
  - Check `PROMETHEUS_SD_TOKEN` matches central `PROMETHEUS_SD_TOKEN`.
  - Confirm central endpoint responds with targets.

- Grafana embed fails in simulator:
  - Ensure Grafana is running on configured port.
  - Ensure monitoring stack has `GF_SECURITY_ALLOW_EMBEDDING=true` (already in compose).

- Probe appears unknown/stale:
  - Confirm probe-agent is running with correct `PROBE_ID`.
  - Confirm probe-agent has central URL and probe API key configured.

- `.env` changes are not reflected:
  - Recreate affected stack:

```powershell
docker compose up -d --build --force-recreate
```

## 9. Stop / Cleanup

Stop central:

```powershell
cd code/central-server
docker compose down
```

Stop monitoring:

```powershell
cd code/monitoring-stack
docker compose down
```

Remove volumes (destructive):

```powershell
docker compose down -v
```
