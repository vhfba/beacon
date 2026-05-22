#define _CRT_SECURE_NO_WARNINGS

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
#ifdef _MSC_VER
#pragma comment(lib, "ws2_32.lib")
#endif
#else
#include <netdb.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <unistd.h>
#endif

#define BUFFER_SIZE 16384

static volatile sig_atomic_t g_stop = 0;

typedef struct {
    const char *host;
    const char *port;
    const char *path;
    const char *name;
} server_config;

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

static long long millis_now(void) {
#ifdef _WIN32
    return (long long)GetTickCount64();
#else
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (long long)tv.tv_sec * 1000 + tv.tv_usec / 1000;
#endif
}

static void log_line(const char *message) {
    time_t now = time(NULL);
    struct tm tmv;
#ifdef _WIN32
    localtime_s(&tmv, &now);
#else
    localtime_r(&now, &tmv);
#endif
    fprintf(stderr, "[%04d-%02d-%02d %02d:%02d:%02d] [beacon-speedtest] %s\n",
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

static void close_socket_compat(int s) {
#ifdef _WIN32
    closesocket(s);
#else
    close(s);
#endif
}

static int connect_tcp(const char *host, const char *port, double *latency_ms) {
    struct addrinfo hints;
    struct addrinfo *res = NULL;
    memset(&hints, 0, sizeof(hints));
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_family = AF_UNSPEC;
    long long start = millis_now();
    if (getaddrinfo(host, port, &hints, &res) != 0) return -1;
    for (struct addrinfo *p = res; p; p = p->ai_next) {
        int s = (int)socket(p->ai_family, p->ai_socktype, p->ai_protocol);
        if (s < 0) continue;
        if (connect(s, p->ai_addr, (int)p->ai_addrlen) == 0) {
            if (latency_ms) *latency_ms = (double)(millis_now() - start);
            freeaddrinfo(res);
            return s;
        }
        close_socket_compat(s);
    }
    freeaddrinfo(res);
    return -1;
}

static int send_all(int s, const char *buf, int len) {
    int sent = 0;
    while (sent < len && !g_stop) {
        int n = send(s, buf + sent, len - sent, 0);
        if (n <= 0) return 0;
        sent += n;
    }
    return 1;
}

static double http_download_mbps(server_config server, double *latency_ms, double *duration_s) {
    int s = connect_tcp(server.host, server.port, latency_ms);
    char req[512];
    char buf[BUFFER_SIZE];
    long long bytes = 0;
    long long start, end;
    int header_done = 0;
    if (s < 0) return -1.0;
    snprintf(req, sizeof(req),
             "GET %s HTTP/1.1\r\nHost: %s\r\nUser-Agent: BEACON-speedtest/1.0\r\nConnection: close\r\n\r\n",
             server.path, server.host);
    if (!send_all(s, req, (int)strlen(req))) {
        close_socket_compat(s);
        return -1.0;
    }
    start = millis_now();
    while (!g_stop) {
        int n = recv(s, buf, sizeof(buf), 0);
        if (n <= 0) break;
        if (!header_done) {
            char *body = NULL;
            for (int i = 3; i < n; i++) {
                if (buf[i - 3] == '\r' && buf[i - 2] == '\n' && buf[i - 1] == '\r' && buf[i] == '\n') {
                    body = buf + i + 1;
                    bytes += n - (int)(body - buf);
                    header_done = 1;
                    break;
                }
            }
        } else {
            bytes += n;
        }
    }
    end = millis_now();
    close_socket_compat(s);
    *duration_s = (end - start) / 1000.0;
    if (*duration_s <= 0.0) return 0.0;
    return (bytes * 8.0) / (*duration_s * 1000000.0);
}

static double http_upload_mbps(server_config server, double *duration_s) {
    int s = connect_tcp(server.host, server.port, NULL);
    char header[512];
    char chunk[BUFFER_SIZE];
    const int upload_bytes = 1024 * 1024;
    int sent_bytes = 0;
    long long start, end;
    if (s < 0) return -1.0;
    memset(chunk, 'B', sizeof(chunk));
    snprintf(header, sizeof(header),
             "POST /post HTTP/1.1\r\nHost: %s\r\nUser-Agent: BEACON-speedtest/1.0\r\nContent-Type: application/octet-stream\r\nContent-Length: %d\r\nConnection: close\r\n\r\n",
             server.host, upload_bytes);
    if (!send_all(s, header, (int)strlen(header))) {
        close_socket_compat(s);
        return -1.0;
    }
    start = millis_now();
    while (sent_bytes < upload_bytes && !g_stop) {
        int want = upload_bytes - sent_bytes;
        if (want > (int)sizeof(chunk)) want = (int)sizeof(chunk);
        if (!send_all(s, chunk, want)) break;
        sent_bytes += want;
    }
    end = millis_now();
    recv(s, chunk, sizeof(chunk), 0);
    close_socket_compat(s);
    *duration_s = (end - start) / 1000.0;
    if (*duration_s <= 0.0) return 0.0;
    return (sent_bytes * 8.0) / (*duration_s * 1000000.0);
}

static double jitter_ms(server_config server, int probes, double *packet_loss_percent) {
    double prev = -1.0;
    double total_delta = 0.0;
    int ok = 0;
    for (int i = 0; i < probes && !g_stop; i++) {
        double lat = 0.0;
        int s = connect_tcp(server.host, server.port, &lat);
        if (s >= 0) {
            close_socket_compat(s);
            if (prev >= 0.0) total_delta += lat > prev ? lat - prev : prev - lat;
            prev = lat;
            ok++;
        }
    }
    *packet_loss_percent = probes > 0 ? ((probes - ok) * 100.0 / probes) : 100.0;
    return ok > 1 ? total_delta / (ok - 1) : 0.0;
}

static void metric_start(int *first, const char *name, const char *kind, double value, const char *server_name) {
    if (!*first) putchar(',');
    *first = 0;
    printf("{\"name\":\"%s\",\"kind\":\"%s\",\"value\":%.3f,\"labels\":{\"probe_id\":", name, kind, value);
    json_escape(stdout, probe_id());
    if (server_name) {
        fputs(",\"server\":", stdout);
        json_escape(stdout, server_name);
    }
    fputs("}}", stdout);
}

int main(void) {
    server_config server = {
        getenv("BEACON_SPEEDTEST_HOST") ? getenv("BEACON_SPEEDTEST_HOST") : "speed.cloudflare.com",
        getenv("BEACON_SPEEDTEST_PORT") ? getenv("BEACON_SPEEDTEST_PORT") : "80",
        getenv("BEACON_SPEEDTEST_PATH") ? getenv("BEACON_SPEEDTEST_PATH") : "/__down?bytes=5000000",
        getenv("BEACON_SPEEDTEST_NAME") ? getenv("BEACON_SPEEDTEST_NAME") : "cloudflare"
    };
    double latency = -1.0, down_s = 0.0, up_s = 0.0, packet_loss = 100.0;
    long long start = millis_now();
    int first = 1;
    signal(SIGINT, on_signal);
    signal(SIGTERM, on_signal);
#ifdef _WIN32
    WSADATA wsa;
    WSAStartup(MAKEWORD(2, 2), &wsa);
    SetConsoleCtrlHandler(console_handler, TRUE);
#endif
    double download = http_download_mbps(server, &latency, &down_s);
    double upload = http_upload_mbps(server, &up_s);
    double jit = jitter_ms(server, 5, &packet_loss);
    double duration = (millis_now() - start) / 1000.0;

    log_line("Speed test complete");
    fputs("{\"metrics\":[", stdout);
    metric_start(&first, "beacon_speed_download_mbps", "gauge", download, server.name);
    metric_start(&first, "beacon_speed_upload_mbps", "gauge", upload, server.name);
    metric_start(&first, "beacon_speed_latency_ms", "gauge", latency, server.name);
    metric_start(&first, "beacon_speed_jitter_ms", "gauge", jit, server.name);
    metric_start(&first, "beacon_speed_packet_loss_percent", "gauge", packet_loss, server.name);
    metric_start(&first, "beacon_speed_last_test_timestamp", "gauge", (double)time(NULL), NULL);
    metric_start(&first, "beacon_speed_test_duration_s", "gauge", duration, NULL);
    fputs("]}\n", stdout);
#ifdef _WIN32
    WSACleanup();
#endif
    return 0;
}
