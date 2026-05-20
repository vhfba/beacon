# Minimum C Plugin Set

These plugins are intentionally one-shot executables for `pi_agent.py`: they print JSON with a `metrics` array and exit.

Build on Raspberry Pi/Linux:

```bash
sudo apt-get update
sudo apt-get install -y gcc iw iproute2 iputils-ping
cd probe-agent/plugins
bash build_minimum_c_plugins.sh
```

Plugins:

- `WIFI_SCAN_C`: Wi-Fi coverage/interference aggregates using `iw`.
- `WIFI_CONNECTIVITY_C`: gateway ping, DNS latency, and TCP connectivity.
- `WIRED_NETWORK_C`: Ethernet carrier, link speed, MTU, and counters.
- `SYSTEM_HEALTH_C`: CPU, memory, disk, temperature, and uptime.

Optional environment variables:

- `BEACON_WIFI_INTERFACE`, default `wlan0`
- `BEACON_ETHERNET_INTERFACE`, default `eth0`
- `BEACON_DNS_TEST_HOST`, default `example.org`
- `BEACON_TCP_TEST_HOST`, default `1.1.1.1`
- `BEACON_TCP_TEST_PORT`, default `443`
