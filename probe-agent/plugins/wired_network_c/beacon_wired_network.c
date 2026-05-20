// gcc beacon_wired_network.c -o beacon_wired_network

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int read_int_file(const char *path, int fallback)
{
    FILE *fp = fopen(path, "r");
    if (!fp) return fallback;
    int value = fallback;
    fscanf(fp, "%d", &value);
    fclose(fp);
    return value;
}

static unsigned long long read_ull_file(const char *path)
{
    FILE *fp = fopen(path, "r");
    if (!fp) return 0;
    unsigned long long value = 0;
    fscanf(fp, "%llu", &value);
    fclose(fp);
    return value;
}

static int operstate_up(const char *iface)
{
    char path[256];
    char state[32] = "";
    snprintf(path, sizeof(path), "/sys/class/net/%s/operstate", iface);
    FILE *fp = fopen(path, "r");
    if (!fp) return 0;
    fgets(state, sizeof(state), fp);
    fclose(fp);
    return strncmp(state, "up", 2) == 0 ? 1 : 0;
}

int main(void)
{
    const char *iface = getenv("BEACON_ETHERNET_INTERFACE");
    if (!iface || !*iface) iface = "eth0";

    char path[256];
    snprintf(path, sizeof(path), "/sys/class/net/%s/carrier", iface);
    int carrier = read_int_file(path, 0);

    snprintf(path, sizeof(path), "/sys/class/net/%s/speed", iface);
    int speed = read_int_file(path, 0);

    snprintf(path, sizeof(path), "/sys/class/net/%s/mtu", iface);
    int mtu = read_int_file(path, 0);

    snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/rx_bytes", iface);
    unsigned long long rx_bytes = read_ull_file(path);
    snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/tx_bytes", iface);
    unsigned long long tx_bytes = read_ull_file(path);
    snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/rx_errors", iface);
    unsigned long long rx_errors = read_ull_file(path);
    snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/tx_errors", iface);
    unsigned long long tx_errors = read_ull_file(path);

    printf("{\"metrics\":[");
    printf("{\"name\":\"beacon_wired_link_up\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}},", operstate_up(iface), iface);
    printf("{\"name\":\"beacon_wired_carrier\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}},", carrier, iface);
    printf("{\"name\":\"beacon_wired_speed_mbps\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}},", speed > 0 ? speed : 0, iface);
    printf("{\"name\":\"beacon_wired_mtu_bytes\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}},", mtu, iface);
    printf("{\"name\":\"beacon_wired_rx_bytes\",\"kind\":\"counter\",\"value\":%llu,\"labels\":{\"interface\":\"%s\"}},", rx_bytes, iface);
    printf("{\"name\":\"beacon_wired_tx_bytes\",\"kind\":\"counter\",\"value\":%llu,\"labels\":{\"interface\":\"%s\"}},", tx_bytes, iface);
    printf("{\"name\":\"beacon_wired_rx_errors\",\"kind\":\"counter\",\"value\":%llu,\"labels\":{\"interface\":\"%s\"}},", rx_errors, iface);
    printf("{\"name\":\"beacon_wired_tx_errors\",\"kind\":\"counter\",\"value\":%llu,\"labels\":{\"interface\":\"%s\"}}", tx_errors, iface);
    printf("]}\n");
    return 0;
}
