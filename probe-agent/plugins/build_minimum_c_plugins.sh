#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cc="${CC:-gcc}"
cflags="${CFLAGS:--O2 -Wall -Wextra}"

"$cc" $cflags "$root/wifi_scan_c/beacon_wifi_scan.c" -o "$root/wifi_scan_c/beacon_wifi_scan"
"$cc" $cflags "$root/wifi_connectivity_c/beacon_wifi_connectivity.c" -o "$root/wifi_connectivity_c/beacon_wifi_connectivity"
"$cc" $cflags "$root/wired_network_c/beacon_wired_network.c" -o "$root/wired_network_c/beacon_wired_network"
"$cc" $cflags "$root/system_health_c/beacon_system_health.c" -o "$root/system_health_c/beacon_system_health"

chmod +x \
  "$root/wifi_scan_c/beacon_wifi_scan" \
  "$root/wifi_connectivity_c/beacon_wifi_connectivity" \
  "$root/wired_network_c/beacon_wired_network" \
  "$root/system_health_c/beacon_system_health"

echo "Built minimum C plugin set."
