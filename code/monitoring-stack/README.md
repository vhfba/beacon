# BEACON Monitoring Stack MVP

This folder contains a practical monitoring MVP for BEACON based on Prometheus and Grafana.

Goals of this MVP:
- Keep monitoring pull-based: Prometheus scrapes probe `/metrics` endpoints.
- Keep the stack local-friendly and reproducible.
- Keep target management centralized via central-server service discovery.
- Provide minimal, useful starter alerting and dashboards.

## What Is Included

- `docker-compose.yml`: Prometheus and Grafana services with sane restart policies.
- `.env.example`: Environment-driven defaults for ports, retention, and Grafana admin credentials.
- `prometheus/prometheus.yml`: Global scrape settings, rule loading, self-scrape, and probe scrape job.
- `prometheus/targets/probes.yml`: Optional fallback target list (not primary flow).
- `prometheus/rules/recording.rules.yml`: Starter recording rules for probe counts and scrape ratios.
- `prometheus/rules/alerts.rules.yml`: Starter alerts for probe reachability and scrape quality.
- `grafana/provisioning/datasources/prometheus.yml`: Auto-provisioned Prometheus datasource.
- `grafana/provisioning/dashboards/dashboards.yml`: Auto-loading of dashboard JSON files.
- `grafana/dashboards/beacon-probe-health.json`: Baseline probe health and scrape visibility dashboard.

## Prerequisites

- Docker Desktop (or Docker Engine) with Compose v2 support.
- Network path from Prometheus container to probe `/metrics` endpoints.
- Optional: `curl` for API-level verification.

## Run The Stack

From this folder:

```powershell
Copy-Item .env.example .env
docker compose up -d
```

Access points:
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

Default Grafana credentials come from `.env`:
- `GRAFANA_ADMIN_USER`
- `GRAFANA_ADMIN_PASSWORD`

Stop stack:

```powershell
docker compose down
```

Stop and remove persisted data volumes:

```powershell
docker compose down -v
```

## Add Or Remove Probe Targets

Primary flow: register probes in central-server.

Prometheus now discovers probe targets from:
- `GET /monitoring/prometheus/service-discovery` on central-server

Target address is derived from registered probe `ipAddress` and the configured default metrics port/path in central-server monitoring settings.

After probe registration, Prometheus picks up target changes on its service discovery refresh interval (default 30s).

## End-To-End Verification

1. Stack health checks

```powershell
docker compose ps
docker compose logs prometheus --tail=50
docker compose logs grafana --tail=50
```

2. Verify Prometheus loaded config and rules

```powershell
curl http://localhost:9090/api/v1/status/config
curl http://localhost:9090/api/v1/rules
```

3. Verify scrape target status
- Open `http://localhost:9090/targets`
- Confirm `prometheus` self-scrape is `UP`.
- Confirm probe targets in `beacon-probes` are visible and have expected `UP`/`DOWN` status.
- Confirm each target has labels (`probe_id`, `site`, `status`) coming from central-server.

4. Verify Grafana provisioning
- Sign in to Grafana.
- Confirm datasource `Prometheus` exists and is default.
- Open dashboard `BEACON Probe Health`.
- Confirm panels render data for `up`, scrape ratio, and `scrape_samples_scraped`.

5. Verify alert behavior
- Query `ALERTS` in Prometheus graph UI.
- Bring a probe target down (or point one target to a non-existing endpoint) and wait for alert `for` durations.

## Starter Alerts Included

- `BeaconProbeUnreachable`: target is down continuously.
- `BeaconProbeScrapeFailuresSustained`: low scrape success ratio over 15 minutes.
- `BeaconProbeMetricsMissingOrStale`: target is up but returns no samples for an extended window.
- `BeaconProbeTargetsMissing`: no configured probe targets found.

## Troubleshooting

- Prometheus cannot scrape probe target:
	- Confirm probe is registered in central-server and has correct `ipAddress`.
	- Verify central-server service discovery token matches Prometheus URL token.
	- Check central-server endpoint `GET /monitoring/prometheus/service-discovery` is reachable from Prometheus container.
	- Validate route from Docker network to probe host (firewall, NAT, DNS).
	- Verify probe exposes Prometheus format at `/metrics`.

- Grafana shows no data:
	- Confirm datasource URL is `http://prometheus:9090` inside Docker network.
	- Check Prometheus target status at `/targets`.
	- Query `up{job="beacon-probes"}` in Prometheus expression browser.

- Rules not loaded:
	- Check Prometheus logs for rule parsing errors.
	- Validate YAML indentation in `prometheus/rules/*.yml`.

- Changed `.env` values not applied:
	- Recreate containers with `docker compose up -d --force-recreate`.

## Metric Assumptions

At this stage, BEACON probe-agent metric names are not implemented in code yet.
This MVP therefore uses Prometheus-native scrape signals as the stable baseline:

- `up{job="beacon-probes"}` for target reachability
- `scrape_samples_scraped{job="beacon-probes"}` for metrics presence/volume proxy

As soon as probe-agent exports stable BEACON-specific metrics, update:
- alert expressions in `prometheus/rules/alerts.rules.yml`
- recording rules in `prometheus/rules/recording.rules.yml`
- dashboard queries in `grafana/dashboards/beacon-probe-health.json`

## Security Note

Do not commit real credentials in `.env`.
Use `.env.example` as a template and keep `.env` local.

For service discovery and dashboard sync:
- Do not use development tokens in production.
- Set secure values for central-server `Monitoring:Prometheus:ServiceDiscoveryToken` and `Monitoring:Grafana:ApiToken`.
