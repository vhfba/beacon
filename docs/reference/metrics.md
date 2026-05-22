# Metrics Reference

BEACON metrics are exported by the control plane from the latest metric snapshots reported by probe runtimes.

## Collection Model

- Probe runtimes produce normalized samples from scheduled plugins and action plugins.
- Probe runtimes report samples through the runtime API.
- Control plane stores the latest snapshot per probe.
- Metrics collector scrapes only the control plane metrics export.
- Dashboard service queries the metrics collector for time-series views.

## Label Expectations

Common labels should remain stable:

- `probe_id`: stable probe identifier.
- `plugin_id`: plugin that produced the sample, when available.
- `test_type`: scheduled test type, when available.
- `site` or location-derived labels: physical grouping, when available.
- target labels: measured endpoint or network target, when useful.

## Coverage Inputs

Fleet coverage uses latest Wi-Fi and latency-related samples when available:

- signal strength
- signal quality
- signal-to-noise ratio
- latency
- packet loss
- sample freshness

Coverage summaries are latest-state operational views. Long-term trends belong in the metrics collector and dashboard service.

## Analyzer Plugin Metrics

`beacon-wifi` emits Wi-Fi environment, connected AP, channel, security, and scan metadata samples:

- `beacon_wifi_visible_aps`
- `beacon_wifi_visible_networks`
- `beacon_wifi_open_networks`
- `beacon_wifi_duplicate_ssid_count`
- `beacon_wifi_ap_signal_dbm`
- `beacon_wifi_ap_quality_score`
- `beacon_wifi_connected`
- `beacon_wifi_connected_signal_dbm`
- `beacon_wifi_connected_rx_rate_mbps`
- `beacon_wifi_connected_tx_rate_mbps`
- `beacon_wifi_connected_quality_score`
- `beacon_wifi_channel_ap_count`
- `beacon_wifi_channel_quality_score`
- `beacon_wifi_band_ap_count`
- `beacon_wifi_security_ap_count`
- `beacon_wifi_strongest_signal_dbm`
- `beacon_wifi_24ghz_best_channel`
- `beacon_wifi_24ghz_overlapping_aps`
- `beacon_wifi_24ghz_channel_overlap_score`
- `beacon_wifi_24ghz_channel_recommendation_score`
- `beacon_wifi_24ghz_channel_direct_aps`
- `beacon_wifi_scan_duration_ms`
- `beacon_wifi_last_scan_timestamp`

`beacon-ethernet` emits wired interface, gateway, internet, and DNS samples:

- `beacon_eth_link_up`
- `beacon_eth_link_speed_mbps`
- `beacon_eth_duplex`
- `beacon_eth_rx_bytes_total`
- `beacon_eth_tx_bytes_total`
- `beacon_eth_rx_errors_total`
- `beacon_eth_tx_errors_total`
- `beacon_eth_rx_dropped_total`
- `beacon_eth_mtu_bytes`
- `beacon_eth_gateway_reachable`
- `beacon_eth_gateway_latency_ms`
- `beacon_eth_internet_reachable`
- `beacon_eth_internet_latency_ms`
- `beacon_eth_dns_resolve_ms`

`beacon-speedtest` emits periodic HTTP throughput test samples:

- `beacon_speed_download_mbps`
- `beacon_speed_upload_mbps`
- `beacon_speed_latency_ms`
- `beacon_speed_jitter_ms`
- `beacon_speed_packet_loss_percent`
- `beacon_speed_last_test_timestamp`
- `beacon_speed_test_duration_s`

## Change Rules

- Adding a metric requires updating this reference, dashboard queries, and any alert or recording rules that consume it.
- Renaming or removing a metric is a breaking contract change and should have an ADR or design doc.
- New labels should be low-cardinality unless the design explicitly justifies otherwise.
- Probe runtimes should normalize plugin output before reporting it to the control plane.

## Related Files

- API contract: [api.md](./api.md)
- Plugin contract: [plugin-contract.md](./plugin-contract.md)
- Monitoring deployment: [local-deployment.md](../operations/local-deployment.md)
