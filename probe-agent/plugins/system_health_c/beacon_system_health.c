// gcc beacon_system_health.c -o beacon_system_health

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/statvfs.h>
#include <unistd.h>

static double read_first_double(const char *path)
{
    FILE *fp = fopen(path, "r");
    if (!fp) return 0.0;
    double value = 0.0;
    fscanf(fp, "%lf", &value);
    fclose(fp);
    return value;
}

static void read_meminfo(double *total_kb, double *available_kb)
{
    FILE *fp = fopen("/proc/meminfo", "r");
    if (!fp) return;

    char key[64];
    double value = 0;
    char unit[32];
    while (fscanf(fp, "%63s %lf %31s", key, &value, unit) == 3) {
        if (strcmp(key, "MemTotal:") == 0) *total_kb = value;
        if (strcmp(key, "MemAvailable:") == 0) *available_kb = value;
    }

    fclose(fp);
}

static int read_cpu_times(unsigned long long *idle, unsigned long long *total)
{
    FILE *fp = fopen("/proc/stat", "r");
    if (!fp) return 0;

    char cpu[16];
    unsigned long long user = 0, nice = 0, system = 0, idle_v = 0, iowait = 0, irq = 0, softirq = 0, steal = 0;
    int ok = fscanf(fp, "%15s %llu %llu %llu %llu %llu %llu %llu %llu",
                    cpu, &user, &nice, &system, &idle_v, &iowait, &irq, &softirq, &steal);
    fclose(fp);
    if (ok < 5) return 0;

    *idle = idle_v + iowait;
    *total = user + nice + system + idle_v + iowait + irq + softirq + steal;
    return 1;
}

static double cpu_usage_percent(void)
{
    unsigned long long idle1 = 0, total1 = 0, idle2 = 0, total2 = 0;
    if (!read_cpu_times(&idle1, &total1)) return 0.0;
    usleep(200000);
    if (!read_cpu_times(&idle2, &total2)) return 0.0;

    unsigned long long total_delta = total2 - total1;
    unsigned long long idle_delta = idle2 - idle1;
    if (total_delta == 0) return 0.0;
    return 100.0 * (double)(total_delta - idle_delta) / (double)total_delta;
}

int main(void)
{
    double mem_total = 0.0;
    double mem_available = 0.0;
    read_meminfo(&mem_total, &mem_available);

    struct statvfs disk;
    double disk_total = 0.0;
    double disk_available = 0.0;
    if (statvfs("/", &disk) == 0) {
        disk_total = (double)disk.f_blocks * (double)disk.f_frsize;
        disk_available = (double)disk.f_bavail * (double)disk.f_frsize;
    }

    double temp_milli_c = read_first_double("/sys/class/thermal/thermal_zone0/temp");
    double uptime_s = read_first_double("/proc/uptime");
    double cpu_pct = cpu_usage_percent();
    double mem_used_pct = mem_total > 0 ? 100.0 * (mem_total - mem_available) / mem_total : 0.0;
    double disk_used_pct = disk_total > 0 ? 100.0 * (disk_total - disk_available) / disk_total : 0.0;

    printf("{\"metrics\":[");
    printf("{\"name\":\"beacon_system_cpu_usage_percent\",\"kind\":\"gauge\",\"value\":%.2f,\"labels\":{}},", cpu_pct);
    printf("{\"name\":\"beacon_system_memory_used_percent\",\"kind\":\"gauge\",\"value\":%.2f,\"labels\":{}},", mem_used_pct);
    printf("{\"name\":\"beacon_system_memory_total_bytes\",\"kind\":\"gauge\",\"value\":%.0f,\"labels\":{}},", mem_total * 1024.0);
    printf("{\"name\":\"beacon_system_disk_used_percent\",\"kind\":\"gauge\",\"value\":%.2f,\"labels\":{\"mount\":\"/\"}},", disk_used_pct);
    printf("{\"name\":\"beacon_system_disk_total_bytes\",\"kind\":\"gauge\",\"value\":%.0f,\"labels\":{\"mount\":\"/\"}},", disk_total);
    printf("{\"name\":\"beacon_system_temperature_celsius\",\"kind\":\"gauge\",\"value\":%.2f,\"labels\":{}},", temp_milli_c > 1000.0 ? temp_milli_c / 1000.0 : temp_milli_c);
    printf("{\"name\":\"beacon_system_uptime_seconds\",\"kind\":\"gauge\",\"value\":%.0f,\"labels\":{}}", uptime_s);
    printf("]}\n");
    return 0;
}
