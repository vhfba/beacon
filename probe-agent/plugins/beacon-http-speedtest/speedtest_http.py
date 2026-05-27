#!/usr/bin/env python3

import json
import os
import socket
import time


DOWNLOAD_HOSTS = [
    ("speed.cloudflare.com", "/__down?bytes=10000000", 80),
]

UPLOAD_HOST = ("httpbin.org", "/post", 80)
UPLOAD_SIZE_BYTES = 5 * 1024 * 1024
TIMEOUT_SECONDS = 20


def http_get_timed(host, path, port=80, timeout=TIMEOUT_SECONDS):
    try:
        sock = socket.create_connection((host, port), timeout=timeout)
        connected_at = time.perf_counter()

        request = (
            f"GET {path} HTTP/1.1\r\n"
            f"Host: {host}\r\n"
            "Connection: close\r\n"
            "User-Agent: beacon-probe/1.0\r\n"
            "\r\n"
        )
        sock.sendall(request.encode())

        header_buf = b""
        first_byte_time = None
        while b"\r\n\r\n" not in header_buf:
            chunk = sock.recv(4096)
            if not chunk:
                break
            if first_byte_time is None:
                first_byte_time = time.perf_counter()
            header_buf += chunk

        latency_ms = round(((first_byte_time or time.perf_counter()) - connected_at) * 1000, 2)
        status_line = header_buf.split(b"\r\n")[0].decode(errors="ignore")
        if "200" not in status_line and "204" not in status_line:
            sock.close()
            return latency_ms, -1.0, 0

        body_start = time.perf_counter()
        total_bytes = len(header_buf.split(b"\r\n\r\n", 1)[-1])
        sock.settimeout(timeout)

        while True:
            chunk = sock.recv(65536)
            if not chunk:
                break
            total_bytes += len(chunk)

        body_elapsed = time.perf_counter() - body_start
        sock.close()

        if body_elapsed <= 0 or total_bytes == 0:
            return latency_ms, -1.0, 0

        throughput_mbps = round((total_bytes * 8) / (body_elapsed * 1_000_000), 2)
        return latency_ms, throughput_mbps, 1
    except Exception:
        return -1.0, -1.0, 0


def http_post_timed(host, path, port=80, upload_bytes=UPLOAD_SIZE_BYTES, timeout=TIMEOUT_SECONDS):
    try:
        sock = socket.create_connection((host, port), timeout=timeout)
        headers = (
            f"POST {path} HTTP/1.1\r\n"
            f"Host: {host}\r\n"
            "Content-Type: application/octet-stream\r\n"
            f"Content-Length: {upload_bytes}\r\n"
            "Connection: close\r\n"
            "User-Agent: beacon-probe/1.0\r\n"
            "\r\n"
        )
        sock.sendall(headers.encode())

        chunk_size = 65536
        sent = 0
        payload_chunk = b"\x00" * chunk_size
        start = time.perf_counter()

        while sent < upload_bytes:
            to_send = min(chunk_size, upload_bytes - sent)
            sock.sendall(payload_chunk[:to_send])
            sent += to_send

        elapsed = time.perf_counter() - start
        sock.settimeout(5)
        try:
            while sock.recv(4096):
                pass
        except Exception:
            pass
        sock.close()

        if elapsed <= 0:
            return -1.0, 0

        throughput_mbps = round((sent * 8) / (elapsed * 1_000_000), 2)
        return throughput_mbps, 1
    except Exception:
        return -1.0, 0


def ping_latency(host, port=80, samples=5):
    times = []
    for _ in range(samples):
        try:
            start = time.perf_counter()
            with socket.create_connection((host, port), timeout=3):
                pass
            times.append((time.perf_counter() - start) * 1000)
        except Exception:
            pass
        time.sleep(0.1)

    if not times:
        return -1.0
    return round(sum(times) / len(times), 2)


def metric(name, kind, value, labels):
    return {
        "name": name,
        "kind": kind,
        "value": value,
        "labels": labels,
    }


def main():
    dl_host = os.environ.get("BEACON_SPEEDTEST_HOST", DOWNLOAD_HOSTS[0][0])
    dl_path = os.environ.get("BEACON_SPEEDTEST_PATH", DOWNLOAD_HOSTS[0][1])
    dl_port = int(os.environ.get("BEACON_SPEEDTEST_PORT", DOWNLOAD_HOSTS[0][2]))
    up_host = os.environ.get("BEACON_SPEEDTEST_UPLOAD_HOST", UPLOAD_HOST[0])
    up_path = os.environ.get("BEACON_SPEEDTEST_UPLOAD_PATH", UPLOAD_HOST[1])
    up_port = int(os.environ.get("BEACON_SPEEDTEST_UPLOAD_PORT", UPLOAD_HOST[2]))

    latency_ms = ping_latency(dl_host, dl_port)
    _, download_mbps, dl_ok = http_get_timed(dl_host, dl_path, dl_port)
    upload_mbps, ul_ok = http_post_timed(up_host, up_path, up_port)

    print(json.dumps({
        "metrics": [
            metric("beacon_speedtest_latency_ms", "gauge", latency_ms, {"server": dl_host}),
            metric("beacon_speedtest_download_mbps", "gauge", download_mbps, {"server": dl_host, "success": str(dl_ok)}),
            metric("beacon_speedtest_upload_mbps", "gauge", upload_mbps, {"server": up_host, "success": str(ul_ok)}),
            metric("beacon_speedtest_download_success", "gauge", dl_ok, {"server": dl_host}),
            metric("beacon_speedtest_upload_success", "gauge", ul_ok, {"server": up_host}),
        ]
    }, indent=2))


if __name__ == "__main__":
    main()
