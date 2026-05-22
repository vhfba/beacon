#define _CRT_SECURE_NO_WARNINGS

#include <ctype.h>
#include <signal.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#include <iphlpapi.h>
#include <icmpapi.h>
#ifdef _MSC_VER
#pragma comment(lib, "iphlpapi.lib")
#pragma comment(lib, "ws2_32.lib")
#endif
#else
#include <arpa/inet.h>
#include <dirent.h>
#include <netdb.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <unistd.h>
#endif

#define MAX_IFACES 64

static volatile sig_atomic_t g_stop = 0;

typedef struct {
    char name[128];
    char mac[32];
    int link_up;
    unsigned long long speed_mbps;
    int duplex;
    unsigned long long rx_bytes;
    unsigned long long tx_bytes;
    unsigned long long rx_errors;
    unsigned long long tx_errors;
    unsigned long long rx_dropped;
    unsigned int mtu;
} iface_info;

static void on_signal(int sig) {
    (void)sig;
    g_stop = 1;
}

#ifdef _WIN32
static BOOL WINAPI console_handler(DWORD type) {
    if (type == CTRL_C_EVENT || type == CTRL_BREAK_EVENT || type == CTRL_CLOSE_EVENT || type == CTRL_SHUTDOWN_EVENT) {
        g_stop = 1;
        return TRUE;
    }
    return FALSE;
}
#endif

static const char *probe_id(void) {
    static char host[256];
    const char *env = getenv("BEACON_PROBE_ID");
    if (env && env[0]) return env;
#ifdef _WIN32
    DWORD len = (DWORD)sizeof(host);
    if (GetComputerNameA(host, &len)) return host;
#else
    if (gethostname(host, sizeof(host)) == 0) return host;
#endif
    return "unknown-probe";
}

static void log_line(const char *message) {
    time_t now = time(NULL);
    struct tm tmv;
#ifdef _WIN32
    localtime_s(&tmv, &now);
#else
    localtime_r(&now, &tmv);
#endif
    fprintf(stderr, "[%04d-%02d-%02d %02d:%02d:%02d] [beacon-ethernet] %s\n",
            tmv.tm_year + 1900, tmv.tm_mon + 1, tmv.tm_mday, tmv.tm_hour, tmv.tm_min, tmv.tm_sec, message);
}

static void json_escape(FILE *out, const char *s) {
    fputc('"', out);
    for (; s && *s; s++) {
        unsigned char c = (unsigned char)*s;
        if (c == '"' || c == '\\') { fputc('\\', out); fputc(c, out); }
        else if (c >= 32 && c < 127) fputc(c, out);
        else fputc('?', out);
    }
    fputc('"', out);
}

static long long millis_now(void) {
#ifdef _WIN32
    return (long long)GetTickCount64();
#else
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (long long)tv.tv_sec * 1000 + tv.tv_usec / 1000;
#endif
}

static int tcp_latency_ms(const char *host, const char *port, double *latency) {
    struct addrinfo hints;
    struct addrinfo *res = NULL;
    memset(&hints, 0, sizeof(hints));
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_family = AF_UNSPEC;
    long long start = millis_now();
    if (getaddrinfo(host, port, &hints, &res) != 0) return 0;
    for (struct addrinfo *p = res; p; p = p->ai_next) {
        int s = (int)socket(p->ai_family, p->ai_socktype, p->ai_protocol);
        if (s < 0) continue;
        if (connect(s, p->ai_addr, (int)p->ai_addrlen) == 0) {
            *latency = (double)(millis_now() - start);
#ifdef _WIN32
            closesocket(s);
#else
            close(s);
#endif
            freeaddrinfo(res);
            return 1;
        }
#ifdef _WIN32
        closesocket(s);
#else
        close(s);
#endif
    }
    freeaddrinfo(res);
    return 0;
}

static double dns_resolve_ms(void) {
    struct addrinfo hints;
    struct addrinfo *res = NULL;
    memset(&hints, 0, sizeof(hints));
    hints.ai_socktype = SOCK_STREAM;
    long long start = millis_now();
    int ok = getaddrinfo("www.cloudflare.com", "443", &hints, &res);
    double elapsed = (double)(millis_now() - start);
    if (res) freeaddrinfo(res);
    return ok == 0 ? elapsed : -1.0;
}

#ifdef _WIN32
static void mac_to_text(const BYTE *addr, ULONG len, char *out, size_t out_size) {
    if (len < 6) {
        snprintf(out, out_size, "unknown");
        return;
    }
    snprintf(out, out_size, "%02x:%02x:%02x:%02x:%02x:%02x", addr[0], addr[1], addr[2], addr[3], addr[4], addr[5]);
}

static int collect_windows(iface_info *items, int max_items) {
    PMIB_IF_TABLE2 table = NULL;
    int count = 0;
    if (GetIfTable2(&table) != NO_ERROR || !table) return 0;
    for (ULONG i = 0; i < table->NumEntries && count < max_items && !g_stop; i++) {
        MIB_IF_ROW2 *r = &table->Table[i];
        if (r->Type == IF_TYPE_SOFTWARE_LOOPBACK) continue;
        iface_info *it = &items[count];
        memset(it, 0, sizeof(*it));
        WideCharToMultiByte(CP_UTF8, 0, r->Alias[0] ? r->Alias : r->Description, -1, it->name, sizeof(it->name), NULL, NULL);
        mac_to_text(r->PhysicalAddress, r->PhysicalAddressLength, it->mac, sizeof(it->mac));
        it->link_up = r->OperStatus == IfOperStatusUp ? 1 : 0;
        it->speed_mbps = r->TransmitLinkSpeed / 1000000ULL;
        it->duplex = 1;
        it->rx_bytes = r->InOctets;
        it->tx_bytes = r->OutOctets;
        it->rx_errors = r->InErrors;
        it->tx_errors = r->OutErrors;
        it->rx_dropped = r->InDiscards;
        it->mtu = r->Mtu;
        count++;
    }
    FreeMibTable(table);
    return count;
}

static int gateway_latency(double *latency) {
    PMIB_IPFORWARD_TABLE2 routes = NULL;
    char gateway[INET_ADDRSTRLEN] = "";
    int found = 0;
    if (GetIpForwardTable2(AF_INET, &routes) != NO_ERROR || !routes) return 0;
    for (ULONG i = 0; i < routes->NumEntries; i++) {
        MIB_IPFORWARD_ROW2 *r = &routes->Table[i];
        if (r->DestinationPrefix.PrefixLength == 0 && r->NextHop.si_family == AF_INET) {
            inet_ntop(AF_INET, &r->NextHop.Ipv4.sin_addr, gateway, sizeof(gateway));
            found = 1;
            break;
        }
    }
    FreeMibTable(routes);
    return found ? tcp_latency_ms(gateway, "80", latency) : 0;
}
#else
static unsigned long long read_ull_file(const char *path) {
    FILE *f = fopen(path, "r");
    unsigned long long v = 0;
    if (f) {
        fscanf(f, "%llu", &v);
        fclose(f);
    }
    return v;
}

static int collect_linux(iface_info *items, int max_items) {
    DIR *dir = opendir("/sys/class/net");
    int count = 0;
    if (!dir) return 0;
    struct dirent *de;
    while ((de = readdir(dir)) != NULL && count < max_items) {
        if (de->d_name[0] == '.' || strcmp(de->d_name, "lo") == 0) continue;
        iface_info *it = &items[count];
        char path[512], state[32] = "";
        memset(it, 0, sizeof(*it));
        snprintf(it->name, sizeof(it->name), "%s", de->d_name);
        snprintf(path, sizeof(path), "/sys/class/net/%s/address", de->d_name);
        FILE *f = fopen(path, "r");
        if (f) { fscanf(f, "%31s", it->mac); fclose(f); }
        snprintf(path, sizeof(path), "/sys/class/net/%s/operstate", de->d_name);
        f = fopen(path, "r");
        if (f) { fscanf(f, "%31s", state); fclose(f); }
        it->link_up = strcmp(state, "up") == 0;
        snprintf(path, sizeof(path), "/sys/class/net/%s/speed", de->d_name);
        it->speed_mbps = read_ull_file(path);
        it->duplex = 1;
        snprintf(path, sizeof(path), "/sys/class/net/%s/mtu", de->d_name);
        it->mtu = (unsigned int)read_ull_file(path);
        snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/rx_bytes", de->d_name); it->rx_bytes = read_ull_file(path);
        snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/tx_bytes", de->d_name); it->tx_bytes = read_ull_file(path);
        snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/rx_errors", de->d_name); it->rx_errors = read_ull_file(path);
        snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/tx_errors", de->d_name); it->tx_errors = read_ull_file(path);
        snprintf(path, sizeof(path), "/sys/class/net/%s/statistics/rx_dropped", de->d_name); it->rx_dropped = read_ull_file(path);
        count++;
    }
    closedir(dir);
    return count;
}

static int gateway_latency(double *latency) {
    return tcp_latency_ms("1.1.1.1", "80", latency);
}
#endif

static void metric_start(int *first, const char *name, const char *kind, double value) {
    if (!*first) putchar(',');
    *first = 0;
    printf("{\"name\":\"%s\",\"kind\":\"%s\",\"value\":%.3f,\"labels\":{\"probe_id\":", name, kind, value);
    json_escape(stdout, probe_id());
}

static void metric_end(void) {
    fputs("}}", stdout);
}

static void add_label(const char *key, const char *value) {
    printf(",\"%s\":", key);
    json_escape(stdout, value);
}

static void emit_metrics(iface_info *items, int count) {
    int first = 1;
    double gw_lat = -1.0, inet_lat = -1.0, dns_ms = dns_resolve_ms();
    int gw_ok = gateway_latency(&gw_lat);
    int inet_ok = tcp_latency_ms("1.1.1.1", "443", &inet_lat);
    unsigned long long total_errors = 0;
    for (int i = 0; i < count; i++) total_errors += items[i].rx_errors + items[i].tx_errors;

    fputs("{\"metrics\":[", stdout);
    metric_start(&first, "beacon_eth_gateway_reachable", "gauge", gw_ok); metric_end();
    metric_start(&first, "beacon_eth_gateway_latency_ms", "gauge", gw_lat); metric_end();
    metric_start(&first, "beacon_eth_internet_reachable", "gauge", inet_ok); metric_end();
    metric_start(&first, "beacon_eth_internet_latency_ms", "gauge", inet_lat); metric_end();
    metric_start(&first, "beacon_eth_dns_resolve_ms", "gauge", dns_ms); metric_end();
    metric_start(&first, "beacon_eth_errors_total", "counter", (double)total_errors); metric_end();

    for (int i = 0; i < count; i++) {
        iface_info *it = &items[i];
        const char *names[] = {
            "beacon_eth_link_up", "beacon_eth_link_speed_mbps", "beacon_eth_duplex",
            "beacon_eth_rx_bytes_total", "beacon_eth_tx_bytes_total", "beacon_eth_rx_errors_total",
            "beacon_eth_tx_errors_total", "beacon_eth_rx_dropped_total", "beacon_eth_mtu_bytes"
        };
        double vals[] = {
            it->link_up, (double)it->speed_mbps, it->duplex, (double)it->rx_bytes, (double)it->tx_bytes,
            (double)it->rx_errors, (double)it->tx_errors, (double)it->rx_dropped, it->mtu
        };
        const char *kinds[] = {"gauge", "gauge", "gauge", "counter", "counter", "counter", "counter", "counter", "gauge"};
        for (int n = 0; n < 9; n++) {
            metric_start(&first, names[n], kinds[n], vals[n]);
            add_label("interface", it->name);
            add_label("mac", it->mac);
            metric_end();
        }
    }
    fputs("]}\n", stdout);
}

int main(void) {
    iface_info items[MAX_IFACES];
    int count;
    char msg[128];
    signal(SIGINT, on_signal);
    signal(SIGTERM, on_signal);
#ifdef _WIN32
    WSADATA wsa;
    WSAStartup(MAKEWORD(2, 2), &wsa);
    SetConsoleCtrlHandler(console_handler, TRUE);
    count = collect_windows(items, MAX_IFACES);
#else
    count = collect_linux(items, MAX_IFACES);
#endif
    snprintf(msg, sizeof(msg), "Scan complete: %d interfaces", count);
    log_line(msg);
    emit_metrics(items, count);
#ifdef _WIN32
    WSACleanup();
#endif
    return 0;
}
