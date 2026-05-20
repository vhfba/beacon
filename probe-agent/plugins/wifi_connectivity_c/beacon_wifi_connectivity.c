// gcc beacon_wifi_connectivity.c -o beacon_wifi_connectivity

#include <arpa/inet.h>
#include <errno.h>
#include <netdb.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <unistd.h>

static long now_ms(void)
{
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (tv.tv_sec * 1000L) + (tv.tv_usec / 1000L);
}

static void trim(char *text)
{
    size_t len = strlen(text);
    while (len > 0 && (text[len - 1] == '\n' || text[len - 1] == '\r' || text[len - 1] == ' ')) {
        text[len - 1] = '\0';
        len--;
    }
}

static int default_gateway(char *out, size_t out_size)
{
    FILE *fp = popen("ip route show default 2>/dev/null | awk '/default/ {print $3; exit}'", "r");
    if (!fp) return 0;
    if (!fgets(out, out_size, fp)) {
        pclose(fp);
        return 0;
    }
    pclose(fp);
    trim(out);
    return out[0] != '\0';
}

static void ping_stats(const char *target, double *loss_percent, double *avg_ms)
{
    char cmd[256];
    snprintf(cmd, sizeof(cmd), "ping -c 3 -W 2 %s 2>/dev/null", target);
    FILE *fp = popen(cmd, "r");
    if (!fp) return;

    char line[512];
    while (fgets(line, sizeof(line), fp)) {
        double loss = 100.0;
        if (strstr(line, "packet loss") && sscanf(line, "%*d packets transmitted, %*d received, %lf%% packet loss", &loss) == 1) {
            *loss_percent = loss;
        }

        double min = 0, avg = 0, max = 0, mdev = 0;
        if ((strstr(line, "rtt ") || strstr(line, "round-trip ")) &&
            sscanf(line, "%*[^=]= %lf/%lf/%lf/%lf", &min, &avg, &max, &mdev) == 4) {
            *avg_ms = avg;
        }
    }

    pclose(fp);
}

static double dns_latency_ms(const char *hostname, int *success)
{
    struct addrinfo hints;
    struct addrinfo *result = NULL;
    memset(&hints, 0, sizeof(hints));
    hints.ai_socktype = SOCK_STREAM;

    long start = now_ms();
    int rc = getaddrinfo(hostname, NULL, &hints, &result);
    long elapsed = now_ms() - start;

    if (result) freeaddrinfo(result);
    *success = rc == 0 ? 1 : 0;
    return (double)elapsed;
}

static double tcp_connect_ms(const char *host, const char *port, int *success)
{
    struct addrinfo hints;
    struct addrinfo *result = NULL;
    memset(&hints, 0, sizeof(hints));
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_family = AF_UNSPEC;

    long start = now_ms();
    int rc = getaddrinfo(host, port, &hints, &result);
    if (rc != 0) {
        *success = 0;
        return (double)(now_ms() - start);
    }

    int connected = 0;
    for (struct addrinfo *rp = result; rp != NULL; rp = rp->ai_next) {
        int fd = socket(rp->ai_family, rp->ai_socktype, rp->ai_protocol);
        if (fd < 0) continue;
        struct timeval timeout = {.tv_sec = 3, .tv_usec = 0};
        setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &timeout, sizeof(timeout));
        setsockopt(fd, SOL_SOCKET, SO_SNDTIMEO, &timeout, sizeof(timeout));
        if (connect(fd, rp->ai_addr, rp->ai_addrlen) == 0) connected = 1;
        close(fd);
        if (connected) break;
    }

    freeaddrinfo(result);
    *success = connected;
    return (double)(now_ms() - start);
}

int main(void)
{
    char gateway[128] = "";
    const char *dns_host = getenv("BEACON_DNS_TEST_HOST");
    const char *tcp_host = getenv("BEACON_TCP_TEST_HOST");
    const char *tcp_port = getenv("BEACON_TCP_TEST_PORT");

    if (!dns_host || !*dns_host) dns_host = "example.org";
    if (!tcp_host || !*tcp_host) tcp_host = "1.1.1.1";
    if (!tcp_port || !*tcp_port) tcp_port = "443";

    double loss = 100.0;
    double avg = 0.0;
    if (default_gateway(gateway, sizeof(gateway))) {
        ping_stats(gateway, &loss, &avg);
    }

    int dns_ok = 0;
    int tcp_ok = 0;
    double dns_ms = dns_latency_ms(dns_host, &dns_ok);
    double tcp_ms = tcp_connect_ms(tcp_host, tcp_port, &tcp_ok);

    printf("{\"metrics\":[");
    printf("{\"name\":\"beacon_connectivity_gateway_packet_loss_percent\",\"kind\":\"gauge\",\"value\":%.2f,\"labels\":{\"target\":\"%s\"}},", loss, gateway[0] ? gateway : "unknown");
    printf("{\"name\":\"beacon_connectivity_gateway_latency_ms\",\"kind\":\"gauge\",\"value\":%.2f,\"labels\":{\"target\":\"%s\"}},", avg, gateway[0] ? gateway : "unknown");
    printf("{\"name\":\"beacon_connectivity_dns_success\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"target\":\"%s\"}},", dns_ok, dns_host);
    printf("{\"name\":\"beacon_connectivity_dns_latency_ms\",\"kind\":\"gauge\",\"value\":%.2f,\"labels\":{\"target\":\"%s\"}},", dns_ms, dns_host);
    printf("{\"name\":\"beacon_connectivity_tcp_success\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"target\":\"%s\",\"port\":\"%s\"}},", tcp_ok, tcp_host, tcp_port);
    printf("{\"name\":\"beacon_connectivity_tcp_latency_ms\",\"kind\":\"gauge\",\"value\":%.2f,\"labels\":{\"target\":\"%s\",\"port\":\"%s\"}}", tcp_ms, tcp_host, tcp_port);
    printf("]}\n");
    return 0;
}
