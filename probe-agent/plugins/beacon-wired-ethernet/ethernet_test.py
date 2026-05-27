#!/usr/bin/env python3

import json
import os
import re
import socket
import subprocess
import time


def get_link_state(iface):
    try:
        with open(f"/sys/class/net/{iface}/operstate") as handle:
            return 1 if handle.read().strip() == "up" else 0
    except Exception:
        return 0


def get_local_ip(iface):
    try:
        out = subprocess.check_output(
            ["ip", "-4", "addr", "show", iface],
            stderr=subprocess.DEVNULL,
        ).decode()
        match = re.search(r"inet (\d+\.\d+\.\d+\.\d+)", out)
        return match.group(1) if match else "unknown"
    except Exception:
        return "unknown"


def get_link_speed_mbps(iface):
    try:
        with open(f"/sys/class/net/{iface}/speed") as handle:
            return int(handle.read().strip())
    except Exception:
        return -1


def get_default_gateway_for_iface(iface):
    try:
        out = subprocess.check_output(
            ["ip", "route", "show", "dev", iface],
            stderr=subprocess.DEVNULL,
        ).decode()
        match = re.search(r"default via (\d+\.\d+\.\d+\.\d+)", out)
        if match:
            return match.group(1)
        match = re.search(r"via (\d+\.\d+\.\d+\.\d+)", out)
        return match.group(1) if match else None
    except Exception:
        return None


def get_default_gateway_global():
    try:
        with open("/proc/net/route") as handle:
            for line in handle.readlines()[1:]:
                parts = line.strip().split()
                if parts[1] == "00000000":
                    return socket.inet_ntoa(bytes.fromhex(parts[2])[::-1])
    except Exception:
        pass
    return None


def ping(host, count=5):
    try:
        out = subprocess.check_output(
            ["ping", "-c", str(count), "-W", "2", host],
            stderr=subprocess.DEVNULL,
        ).decode()
        loss_match = re.search(r"(\d+)% packet loss", out)
        loss = float(loss_match.group(1)) if loss_match else 100.0
        rtt_match = re.search(r"rtt min/avg/max/mdev = [\d.]+/([\d.]+)/", out)
        avg_ms = float(rtt_match.group(1)) if rtt_match else -1.0
        return avg_ms, loss
    except subprocess.CalledProcessError:
        return -1.0, 100.0
    except Exception:
        return -1.0, 100.0


def dns_resolution_time(hostname="google.com"):
    try:
        start = time.perf_counter()
        socket.getaddrinfo(hostname, None)
        return round((time.perf_counter() - start) * 1000, 2)
    except Exception:
        return -1.0


def get_rx_tx_bytes(iface):
    try:
        with open(f"/sys/class/net/{iface}/statistics/rx_bytes") as handle:
            rx = int(handle.read().strip())
        with open(f"/sys/class/net/{iface}/statistics/tx_bytes") as handle:
            tx = int(handle.read().strip())
        return rx, tx
    except Exception:
        return 0, 0


def get_rx_tx_errors(iface):
    try:
        with open(f"/sys/class/net/{iface}/statistics/rx_errors") as handle:
            rx_err = int(handle.read().strip())
        with open(f"/sys/class/net/{iface}/statistics/tx_errors") as handle:
            tx_err = int(handle.read().strip())
        return rx_err, tx_err
    except Exception:
        return 0, 0


def metric(name, kind, value, labels):
    return {
        "name": name,
        "kind": kind,
        "value": value,
        "labels": labels,
    }


def main():
    iface = os.environ.get("BEACON_ETH_IFACE", "eth0")
    link_up = get_link_state(iface)
    local_ip = get_local_ip(iface)
    link_speed = get_link_speed_mbps(iface)
    rx_bytes, tx_bytes = get_rx_tx_bytes(iface)
    rx_errors, tx_errors = get_rx_tx_errors(iface)
    gateway = get_default_gateway_for_iface(iface) or get_default_gateway_global()

    gw_latency_ms = -1.0
    gw_packet_loss = 100.0
    if link_up and gateway:
        gw_latency_ms, gw_packet_loss = ping(gateway, count=5)

    dns_latency_ms = dns_resolution_time("google.com") if link_up else -1.0

    print(json.dumps({
        "metrics": [
            metric("beacon_eth_link_up", "gauge", link_up, {"iface": iface, "ip": local_ip}),
            metric("beacon_eth_link_speed_mbps", "gauge", link_speed, {"iface": iface}),
            metric("beacon_eth_gateway_latency_ms", "gauge", gw_latency_ms, {"iface": iface, "gateway": gateway or "unknown"}),
            metric("beacon_eth_gateway_packet_loss_percent", "gauge", gw_packet_loss, {"iface": iface, "gateway": gateway or "unknown"}),
            metric("beacon_eth_dns_latency_ms", "gauge", dns_latency_ms, {"iface": iface}),
            metric("beacon_eth_rx_bytes_total", "counter", rx_bytes, {"iface": iface}),
            metric("beacon_eth_tx_bytes_total", "counter", tx_bytes, {"iface": iface}),
            metric("beacon_eth_rx_errors_total", "counter", rx_errors, {"iface": iface}),
            metric("beacon_eth_tx_errors_total", "counter", tx_errors, {"iface": iface}),
        ]
    }, indent=2))


if __name__ == "__main__":
    main()
