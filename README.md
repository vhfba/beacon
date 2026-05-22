# BEACON Wi-Fi & Network Analyzer Plugins

This workspace contains three scheduled probe-agent C plugins and matching Grafana dashboards:

- `beacon-wifi`: Windows WLAN environment scanner.
- `beacon-ethernet`: Windows wired interface and connectivity monitor.
- `beacon-speedtest`: HTTP throughput, latency, jitter, and loss test.

The plugins emit normalized BEACON JSON on stdout so `probe-agent/pi_agent.py` can forward metrics to the central server. Human runtime logs are written to stderr to keep stdout parseable.

## Build

From each plugin folder:

```powershell
cd probe-agent\plugins\beacon-wifi
& C:\Users\joaom\Documents\bin\gcc.exe -std=c99 -O2 -Wall -Wextra -o beacon-wifi.exe beacon-wifi.c -lwlanapi -lole32 -liphlpapi

cd ..\beacon-ethernet
& C:\Users\joaom\Documents\bin\gcc.exe -std=c99 -O2 -Wall -Wextra -o beacon-ethernet.exe beacon-ethernet.c -liphlpapi -lws2_32

cd ..\beacon-speedtest
& C:\Users\joaom\Documents\bin\gcc.exe -std=c99 -O2 -Wall -Wextra -o beacon-speedtest.exe beacon-speedtest.c -lws2_32
```

The included Makefiles also support `windows-gcc`, `windows-cl`, and `linux` targets.

## Run Locally

```powershell
$env:BEACON_PROBE_ID = "dev-probe"
probe-agent\plugins\beacon-wifi\beacon-wifi.exe
probe-agent\plugins\beacon-ethernet\beacon-ethernet.exe
probe-agent\plugins\beacon-speedtest\beacon-speedtest.exe
```

Speedtest configuration can be overridden with:

- `BEACON_SPEEDTEST_HOST`
- `BEACON_SPEEDTEST_PORT`
- `BEACON_SPEEDTEST_PATH`
- `BEACON_SPEEDTEST_NAME`

## Bundles

Compiled bundle artifacts are stored in:

```text
code/central-server/plugin-bundles/
```

Current bundles:

- `beacon-wifi-1.0.0.zip`
- `beacon-ethernet-1.0.0.zip`
- `beacon-speedtest-1.0.0.zip`

Each bundle contains `manifest.json`, `plugin.json`, source, Makefile, dashboard JSON, and the Windows executable.

## Grafana

Dashboard JSON files are available in both:

```text
dashboards/
code/monitoring-stack/grafana/dashboards/
```

The monitoring stack provisions dashboards from `code/monitoring-stack/grafana/dashboards`.

Start the stack:

```powershell
cd code\monitoring-stack
docker compose up -d
```

Grafana will load:

- `BEACON Wi-Fi Analyzer`
- `BEACON Ethernet Monitor`
- `BEACON Speed Monitor`

Prometheus continues to scrape the central server at `/metrics`; probes report metrics to central-server through the existing GraphQL runtime flow.
