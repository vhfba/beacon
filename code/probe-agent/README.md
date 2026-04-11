# Probe Agent

This folder now contains a lightweight mock probe service to test the BEACON monitoring stack.

## What this mock service provides

- Background generation of mock Wi-Fi quality tests every X seconds.
- Prometheus metrics endpoint for scraping.
- JSON endpoints to inspect generated test data.
- Optional polling of central-server runtime state to decide if this probe is allowed to emit metrics.
- Periodic heartbeat POST to central-server so fleet UI can show probe liveness.

Generated test families:
- Ping quality (latency, jitter, packet loss)
- HTTP reachability/performance (latency, status code, success)
- iPerf-like throughput quality (download/upload throughput, jitter, packet loss)
- Wi-Fi link telemetry (RSSI, noise floor, SNR, link quality)

## Endpoints

- `GET /health`
- `GET /api/runtime`
- `GET /api/tests/latest`
- `GET /api/tests/history`
- `GET /api/wifi/summary`
- `GET /metrics`

When central sync is configured, the agent polls:

- `GET /probes/{probeId}/runtime-state` on central-server
- `POST /probes/{probeId}/heartbeat` on central-server

Behavior:

- If runtime state says `status=ACTIVE` and `canEmitMetrics=true`, metrics are generated.
- If runtime state says `INACTIVE` or `DECOMMISSIONED`, mock test generation pauses.
- Runtime control metrics (`beacon_probe_runtime_*`) still expose current state.

## Run locally

1. Install dependencies:

```powershell
pip install -r requirements.txt
```

2. Start the mock probe:

```powershell
python mock_probe_agent.py
```

Default bind:
- Host: `0.0.0.0`
- Port: `9464`

## Useful environment variables

- `PORT` (default: `9464`)
- `PROBE_ID` (default: `probe-mock-01`)
- `PROBE_SITE` (default: `building-a-floor-1`)
- `PROBE_SSID` (default: `BEACON-WIFI`)
- `PROBE_INTERFACE` (default: `wlan0`)
- `MOCK_INTERVAL_SECONDS` (default: `5`)
- `HISTORY_SIZE` (default: `100`)
- `PING_TARGETS` (default: `8.8.8.8,1.1.1.1`)
- `HTTP_TARGETS` (default: `https://example.com,https://api.example.com/health`)
- `IPERF_SERVERS` (default: `iperf-a.local,iperf-b.local`)
- `CENTRAL_SERVER_BASE_URL` (example: `http://localhost:5000`)
- `CENTRAL_SERVER_PROBE_API_KEY` (probe API key used in `X-Api-Key`)
- `CENTRAL_SYNC_INTERVAL_SECONDS` (default: `15`)
- `CENTRAL_REQUEST_TIMEOUT_SECONDS` (default: `3`)
- `HEARTBEAT_INTERVAL_SECONDS` (default: `15`)

## Key Prometheus metrics

- `beacon_wifi_rssi_dbm`
- `beacon_wifi_snr_db`
- `beacon_wifi_link_quality_percent`
- `beacon_ping_latency_ms`
- `beacon_ping_jitter_ms`
- `beacon_ping_packet_loss_percent`
- `beacon_http_latency_ms`
- `beacon_http_success`
- `beacon_iperf_throughput_mbps`
- `beacon_iperf_jitter_ms`
- `beacon_iperf_packet_loss_percent`
- `beacon_test_runs_total`
- `beacon_test_failures_total`
- `beacon_test_last_status`
- `beacon_probe_runtime_can_emit_metrics`
- `beacon_probe_runtime_status_active`
- `beacon_probe_runtime_last_sync_timestamp_seconds`
- `beacon_probe_runtime_sync_failures_total`

## Connect to monitoring-stack

In `code/monitoring-stack/prometheus/targets/probes.yml`, add an entry such as:

```yaml
- targets:
    - host.docker.internal:9464
  labels:
    probe_id: probe-mock-01
    site: building-a-floor-1
```

Prometheus will discover it through file SD and start scraping `/metrics`.
