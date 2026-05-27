#!/usr/bin/env python3

import json
import os
import socket
import subprocess
import time


TEST_DOMAINS = [
    "google.com",
    "cloudflare.com",
    "github.com",
]

RESOLVERS = {
    "system": None,
    "google": "8.8.8.8",
    "cloudflare": "1.1.1.1",
}


def resolve_with_system(domain):
    try:
        start = time.perf_counter()
        socket.getaddrinfo(domain, None)
        elapsed = (time.perf_counter() - start) * 1000
        return round(elapsed, 2), 1
    except Exception:
        return -1.0, 0


def resolve_via_raw_socket(domain, resolver_ip, port=53):
    try:
        def encode_name(name):
            encoded = b""
            for part in name.split("."):
                encoded += bytes([len(part)]) + part.encode()
            return encoded + b"\x00"

        packet = (
            b"\xaa\xbb"
            + b"\x01\x00"
            + b"\x00\x01"
            + b"\x00\x00"
            + b"\x00\x00"
            + b"\x00\x00"
            + encode_name(domain)
            + b"\x00\x01"
            + b"\x00\x01"
        )

        start = time.perf_counter()
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
            sock.settimeout(3)
            sock.sendto(packet, (resolver_ip, port))
            data, _ = sock.recvfrom(512)
        elapsed = (time.perf_counter() - start) * 1000
        success = 1 if data[3] & 0x0F == 0 else 0
        return round(elapsed, 2), success
    except Exception:
        return -1.0, 0


def resolve_with_dig(domain, resolver_ip):
    try:
        result = subprocess.run(
            ["dig", f"@{resolver_ip}", domain, "+time=3", "+tries=1", "+stats"],
            capture_output=True,
            text=True,
            timeout=5,
        )
        output = result.stdout

        for line in output.splitlines():
            if "Query time:" in line:
                parts = line.split()
                latency = float(parts[parts.index("time:") + 1])
                success = 1 if result.returncode == 0 and "NOERROR" in output else 0
                return latency, success

        success = 1 if result.returncode == 0 and "NOERROR" in output else 0
        return -1.0, success
    except FileNotFoundError:
        return resolve_via_raw_socket(domain, resolver_ip)
    except subprocess.TimeoutExpired:
        return -1.0, 0
    except Exception:
        return -1.0, 0


def metric(name, kind, value, labels):
    return {
        "name": name,
        "kind": kind,
        "value": value,
        "labels": labels,
    }


def main():
    domain = os.environ.get("BEACON_DNS_TEST_DOMAIN", TEST_DOMAINS[0])
    all_domains = os.environ.get("BEACON_DNS_ALL_DOMAINS", "0") == "1"
    domains = TEST_DOMAINS if all_domains else [domain]
    metrics = []

    for test_domain in domains:
        sys_latency, sys_ok = resolve_with_system(test_domain)
        metrics.append(metric(
            "beacon_dns_latency_ms",
            "gauge",
            sys_latency,
            {"domain": test_domain, "resolver": "system", "resolver_ip": "system"},
        ))
        metrics.append(metric(
            "beacon_dns_success",
            "gauge",
            sys_ok,
            {"domain": test_domain, "resolver": "system", "resolver_ip": "system"},
        ))

        for resolver_name, resolver_ip in RESOLVERS.items():
            if resolver_ip is None:
                continue

            latency, ok = resolve_with_dig(test_domain, resolver_ip)
            labels = {
                "domain": test_domain,
                "resolver": resolver_name,
                "resolver_ip": resolver_ip,
            }
            metrics.append(metric("beacon_dns_latency_ms", "gauge", latency, labels))
            metrics.append(metric("beacon_dns_success", "gauge", ok, labels))

    print(json.dumps({"metrics": metrics}, indent=2))


if __name__ == "__main__":
    main()
