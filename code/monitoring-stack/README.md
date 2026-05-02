# Monitoring Stack

This folder contains the Prometheus and Grafana stack used to observe BEACON.

## Purpose

- scrape central-server `/metrics`
- provide a baseline Grafana dashboard
- define starter recording rules and alerts

The current monitoring model is centralized:

- probes push metrics to central-server
- central-server stores the latest snapshots
- Prometheus scrapes central-server only

## Included Files

- `docker-compose.yml`
- `.env.example`
- `prometheus/prometheus.yml`
- `prometheus/rules/recording.rules.yml`
- `prometheus/rules/alerts.rules.yml`
- `grafana/provisioning/datasources/prometheus.yml`
- `grafana/provisioning/dashboards/dashboards.yml`
- `grafana/dashboards/beacon-probe-health.json`

## Run Locally

From this folder:

```powershell
Copy-Item .env.example .env
docker compose up -d
```

Access points:

- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

Stop:

```powershell
docker compose down
```

## Verification

1. Confirm central-server `/metrics` returns data.
2. Open Prometheus targets and verify `beacon-central-server` is `UP`.
3. Open Grafana and confirm dashboard `BEACON Probe Health` renders data.

Helpful checks:

```powershell
docker compose ps
docker compose logs prometheus --tail=50
docker compose logs grafana --tail=50
curl http://localhost:9090/api/v1/rules
```

## Alerts And Assumptions

The bundled rules assume the central-server exporter is the single scrape target for BEACON metrics.

Starter alerts include:

- unreachable central metrics endpoint
- scrape failure ratio issues
- stale or missing metrics

If the metric model changes, update:

- Prometheus alert rules
- Prometheus recording rules
- Grafana dashboard queries

## Security Notes

- keep real Grafana credentials out of version control
- use `.env.example` as the template
- set a real Grafana API token, or configure central-server with `GRAFANA_API_USER` and `GRAFANA_API_PASSWORD`, before relying on dashboard sync features
