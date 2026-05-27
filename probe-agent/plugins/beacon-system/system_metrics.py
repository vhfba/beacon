#!/usr/bin/env python3

import json
import time


def read_cpu_temp():
    paths = [
        "/sys/class/thermal/thermal_zone0/temp",
        "/sys/class/hwmon/hwmon0/temp1_input",
    ]

    for path in paths:
        try:
            with open(path, "r") as handle:
                value = float(handle.read().strip())
            return value / 1000.0 if value > 1000 else value
        except Exception:
            continue

    return 0.0


def read_cpu_usage():
    def read_stat():
        with open("/proc/stat", "r") as handle:
            values = [float(x) for x in handle.readline().split()[1:]]
        return values[3], sum(values)

    idle1, total1 = read_stat()
    time.sleep(0.2)
    idle2, total2 = read_stat()

    idle_delta = idle2 - idle1
    total_delta = total2 - total1
    if total_delta <= 0:
        return 0.0

    return round(100.0 * (1.0 - idle_delta / total_delta), 2)


def read_memory_usage_percent():
    mem_total = 0
    mem_available = 0

    with open("/proc/meminfo", "r") as handle:
        for line in handle:
            if line.startswith("MemTotal:"):
                mem_total = int(line.split()[1])
            elif line.startswith("MemAvailable:"):
                mem_available = int(line.split()[1])

    if mem_total == 0:
        return 0.0

    return round(((mem_total - mem_available) / mem_total) * 100.0, 2)


def metric(name, kind, value):
    return {
        "name": name,
        "kind": kind,
        "value": value,
        "labels": {},
    }


def main():
    now = time.time()
    print(json.dumps({
        "metrics": [
            metric("cpu_temp", "gauge", read_cpu_temp()),
            metric("cpu_usage", "gauge", read_cpu_usage()),
            metric("memory_usage", "gauge", read_memory_usage_percent()),
            metric("timestamp", "gauge", now),
        ]
    }, indent=2))


if __name__ == "__main__":
    main()
