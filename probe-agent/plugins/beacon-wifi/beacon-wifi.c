#define _CRT_SECURE_NO_WARNINGS

#include <ctype.h>
#include <errno.h>
#include <math.h>
#include <signal.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#ifdef _WIN32
#include <windows.h>
#include <wlanapi.h>
#include <iphlpapi.h>
#include <objbase.h>
#ifdef _MSC_VER
#pragma comment(lib, "wlanapi.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "iphlpapi.lib")
#endif
#else
#include <netdb.h>
#include <sys/utsname.h>
#include <unistd.h>
#endif

#define MAX_APS 512
#define MAX_SSID 64
#define MAX_SECURITY 32
#define MAX_BSSID 24
#define MAX_BAND 8

static volatile sig_atomic_t g_stop = 0;

typedef struct {
    char ssid[MAX_SSID];
    char bssid[MAX_BSSID];
    int channel;
    char band[MAX_BAND];
    char security[MAX_SECURITY];
    int signal_dbm;
    int quality;
    int connected;
    double rx_rate_mbps;
    double tx_rate_mbps;
} ap_info;

static void on_signal(int sig) {
    (void)sig;
    g_stop = 1;
}

#ifdef _WIN32
static BOOL WINAPI console_handler(DWORD type) {
    if (type == CTRL_C_EVENT || type == CTRL_BREAK_EVENT || type == CTRL_CLOSE_EVENT ||
        type == CTRL_SHUTDOWN_EVENT) {
        g_stop = 1;
        return TRUE;
    }
    return FALSE;
}
#endif

static void log_line(const char *plugin, const char *message) {
    time_t now = time(NULL);
    struct tm tmv;
#ifdef _WIN32
    localtime_s(&tmv, &now);
#else
    localtime_r(&now, &tmv);
#endif
    fprintf(stderr, "[%04d-%02d-%02d %02d:%02d:%02d] [%s] %s\n",
            tmv.tm_year + 1900, tmv.tm_mon + 1, tmv.tm_mday,
            tmv.tm_hour, tmv.tm_min, tmv.tm_sec, plugin, message);
}

static const char *probe_id(void) {
    static char host[256];
    const char *env = getenv("BEACON_PROBE_ID");
    if (env && env[0]) {
        return env;
    }
#ifdef _WIN32
    DWORD len = (DWORD)sizeof(host);
    if (GetComputerNameA(host, &len)) {
        return host;
    }
#else
    if (gethostname(host, sizeof(host)) == 0) {
        host[sizeof(host) - 1] = 0;
        return host;
    }
#endif
    return "unknown-probe";
}

static void json_escape(FILE *out, const char *s) {
    fputc('"', out);
    for (; s && *s; s++) {
        unsigned char c = (unsigned char)*s;
        if (c == '"' || c == '\\') {
            fputc('\\', out);
            fputc(c, out);
        } else if (c == '\b') {
            fputs("\\b", out);
        } else if (c == '\f') {
            fputs("\\f", out);
        } else if (c == '\n') {
            fputs("\\n", out);
        } else if (c == '\r') {
            fputs("\\r", out);
        } else if (c == '\t') {
            fputs("\\t", out);
        } else if (c >= 32 && c < 127) {
            fputc(c, out);
        } else {
            fputc('?', out);
        }
    }
    fputc('"', out);
}

static int clamp_int(int v, int lo, int hi) {
    if (v < lo) return lo;
    if (v > hi) return hi;
    return v;
}

static int quality_from_dbm(int dbm) {
    return clamp_int((dbm + 100) * 2, 0, 100);
}

static int channel_from_frequency(unsigned long mhz) {
    if (mhz >= 2412 && mhz <= 2484) {
        if (mhz == 2484) return 14;
        return (int)((mhz - 2407) / 5);
    }
    if (mhz >= 5000 && mhz <= 5900) return (int)((mhz - 5000) / 5);
    if (mhz >= 5925 && mhz <= 7125) return (int)((mhz - 5950) / 5);
    return 0;
}

static void band_from_channel(int channel, char *out, size_t out_size) {
    if (channel >= 1 && channel <= 14) {
        snprintf(out, out_size, "2.4GHz");
    } else if (channel > 0 && channel < 200) {
        snprintf(out, out_size, "5GHz");
    } else {
        snprintf(out, out_size, "unknown");
    }
}

static int security_is_open(const char *s) {
    return s == NULL || strcmp(s, "OPEN") == 0 || strcmp(s, "NONE") == 0;
}

static void normalize_security(char *dst, size_t dst_size, const char *src) {
    char tmp[128];
    size_t i;
    if (!src || !src[0]) {
        snprintf(dst, dst_size, "UNKNOWN");
        return;
    }
    for (i = 0; src[i] && i + 1 < sizeof(tmp); i++) {
        tmp[i] = (char)toupper((unsigned char)src[i]);
    }
    tmp[i] = 0;
    if (strstr(tmp, "WPA3")) snprintf(dst, dst_size, "WPA3");
    else if (strstr(tmp, "ENTERPRISE") || strstr(tmp, "802.1X")) snprintf(dst, dst_size, "WPA2-ENT");
    else if (strstr(tmp, "WPA2")) snprintf(dst, dst_size, "WPA2-PSK");
    else if (strstr(tmp, "WPA")) snprintf(dst, dst_size, "WPA");
    else if (strstr(tmp, "OPEN") || strstr(tmp, "NONE")) snprintf(dst, dst_size, "OPEN");
    else snprintf(dst, dst_size, "UNKNOWN");
}

#ifdef _WIN32
static void ssid_to_string(DOT11_SSID ssid, char *out, size_t out_size) {
    size_t len = ssid.uSSIDLength < out_size - 1 ? ssid.uSSIDLength : out_size - 1;
    memcpy(out, ssid.ucSSID, len);
    out[len] = 0;
    if (len == 0) snprintf(out, out_size, "hidden");
}

static const char *auth_to_security(DOT11_AUTH_ALGORITHM auth, BOOL security_enabled) {
    if (!security_enabled || auth == DOT11_AUTH_ALGO_80211_OPEN) return "OPEN";
    if (auth == DOT11_AUTH_ALGO_WPA3 || auth == DOT11_AUTH_ALGO_WPA3_SAE) return "WPA3";
    if (auth == DOT11_AUTH_ALGO_RSNA) return "WPA2-ENT";
    if (auth == DOT11_AUTH_ALGO_RSNA_PSK) return "WPA2-PSK";
    if (auth == DOT11_AUTH_ALGO_WPA) return "WPA";
    if (auth == DOT11_AUTH_ALGO_WPA_PSK) return "WPA-PSK";
    return "UNKNOWN";
}

static int find_security(PWLAN_AVAILABLE_NETWORK_LIST nets, const char *ssid, char *out, size_t out_size) {
    if (!nets) return 0;
    for (DWORD i = 0; i < nets->dwNumberOfItems; i++) {
        char candidate[MAX_SSID];
        ssid_to_string(nets->Network[i].dot11Ssid, candidate, sizeof(candidate));
        if (strcmp(candidate, ssid) == 0) {
            normalize_security(out, out_size, auth_to_security(nets->Network[i].dot11DefaultAuthAlgorithm,
                                                               nets->Network[i].bSecurityEnabled));
            return 1;
        }
    }
    return 0;
}

static int signal_from_rssi(LONG rssi) {
    if (rssi > 0) return (int)(rssi / 2) - 100;
    return (int)rssi;
}

static int scan_windows(ap_info *aps, int max_aps, int *connected_index, long *duration_ms) {
    HANDLE client = NULL;
    DWORD negotiated = 0;
    PWLAN_INTERFACE_INFO_LIST ifaces = NULL;
    int count = 0;
    ULONGLONG start = GetTickCount64();
    *connected_index = -1;

    DWORD rc = WlanOpenHandle(2, NULL, &negotiated, &client);
    if (rc != ERROR_SUCCESS) return 0;
    rc = WlanEnumInterfaces(client, NULL, &ifaces);
    if (rc != ERROR_SUCCESS || !ifaces || ifaces->dwNumberOfItems == 0) {
        if (ifaces) WlanFreeMemory(ifaces);
        WlanCloseHandle(client, NULL);
        return 0;
    }

    for (DWORD n = 0; n < ifaces->dwNumberOfItems && count < max_aps && !g_stop; n++) {
        const GUID *guid = &ifaces->InterfaceInfo[n].InterfaceGuid;
        WlanScan(client, guid, NULL, NULL, NULL);
        Sleep(2500);

        WLAN_CONNECTION_ATTRIBUTES *attrs = NULL;
        DWORD attrs_size = 0;
        WLAN_OPCODE_VALUE_TYPE op;
        char connected_ssid[MAX_SSID] = "";
        char connected_bssid[MAX_BSSID] = "";
        if (WlanQueryInterface(client, guid, wlan_intf_opcode_current_connection, NULL,
                               &attrs_size, (PVOID *)&attrs, &op) == ERROR_SUCCESS && attrs) {
            ssid_to_string(attrs->wlanAssociationAttributes.dot11Ssid, connected_ssid, sizeof(connected_ssid));
            snprintf(connected_bssid, sizeof(connected_bssid), "%02x:%02x:%02x:%02x:%02x:%02x",
                     attrs->wlanAssociationAttributes.dot11Bssid[0], attrs->wlanAssociationAttributes.dot11Bssid[1],
                     attrs->wlanAssociationAttributes.dot11Bssid[2], attrs->wlanAssociationAttributes.dot11Bssid[3],
                     attrs->wlanAssociationAttributes.dot11Bssid[4], attrs->wlanAssociationAttributes.dot11Bssid[5]);
        }

        PWLAN_AVAILABLE_NETWORK_LIST nets = NULL;
        WlanGetAvailableNetworkList(client, guid, 0, NULL, &nets);

        PWLAN_BSS_LIST bss = NULL;
        rc = WlanGetNetworkBssList(client, guid, NULL, dot11_BSS_type_any, FALSE, NULL, &bss);
        if (rc == ERROR_SUCCESS && bss) {
            for (DWORD i = 0; i < bss->dwNumberOfItems && count < max_aps; i++) {
                WLAN_BSS_ENTRY *e = &bss->wlanBssEntries[i];
                ap_info *ap = &aps[count];
                memset(ap, 0, sizeof(*ap));
                ssid_to_string(e->dot11Ssid, ap->ssid, sizeof(ap->ssid));
                snprintf(ap->bssid, sizeof(ap->bssid), "%02x:%02x:%02x:%02x:%02x:%02x",
                         e->dot11Bssid[0], e->dot11Bssid[1], e->dot11Bssid[2],
                         e->dot11Bssid[3], e->dot11Bssid[4], e->dot11Bssid[5]);
                ap->channel = channel_from_frequency(e->ulChCenterFrequency / 1000);
                band_from_channel(ap->channel, ap->band, sizeof(ap->band));
                if (!find_security(nets, ap->ssid, ap->security, sizeof(ap->security))) {
                    snprintf(ap->security, sizeof(ap->security), "UNKNOWN");
                }
                ap->signal_dbm = signal_from_rssi(e->lRssi);
                ap->quality = quality_from_dbm(ap->signal_dbm);
                ap->connected = connected_ssid[0] && strcmp(ap->ssid, connected_ssid) == 0 &&
                                connected_bssid[0] && strcmp(ap->bssid, connected_bssid) == 0;
                if (ap->connected && attrs) {
                    ap->rx_rate_mbps = attrs->wlanAssociationAttributes.ulRxRate / 1000000.0;
                    ap->tx_rate_mbps = attrs->wlanAssociationAttributes.ulTxRate / 1000000.0;
                    *connected_index = count;
                }
                count++;
            }
            WlanFreeMemory(bss);
        }
        if (nets) WlanFreeMemory(nets);
        if (attrs) WlanFreeMemory(attrs);
    }

    if (ifaces) WlanFreeMemory(ifaces);
    WlanCloseHandle(client, NULL);
    *duration_ms = (long)(GetTickCount64() - start);
    return count;
}
#else
static int scan_linux(ap_info *aps, int max_aps, int *connected_index, long *duration_ms) {
    (void)aps;
    (void)max_aps;
    *connected_index = -1;
    *duration_ms = 0;
    return 0;
}
#endif

static int ssid_seen_before(ap_info *aps, int upto, const char *ssid) {
    for (int i = 0; i < upto; i++) {
        if (strcmp(aps[i].ssid, ssid) == 0) return 1;
    }
    return 0;
}

static int ap_count_on_channel(ap_info *aps, int count, int channel) {
    int total = 0;
    for (int i = 0; i < count; i++) {
        if (aps[i].channel == channel) total++;
    }
    return total;
}

static int overlap_count_24(ap_info *aps, int count, int channel) {
    int total = 0;
    for (int i = 0; i < count; i++) {
        if (strcmp(aps[i].band, "2.4GHz") == 0 && abs(aps[i].channel - channel) <= 4) total++;
    }
    return total;
}

static void metric_prefix(FILE *out, int *first, const char *name, const char *kind, double value) {
    if (!*first) fputc(',', out);
    *first = 0;
    fprintf(out, "{\"name\":\"%s\",\"kind\":\"%s\",\"value\":%.3f,\"labels\":{", name, kind, value);
    fprintf(out, "\"probe_id\":");
    json_escape(out, probe_id());
}

static void metric_end(FILE *out) {
    fputs("}}", out);
}

static void label(FILE *out, const char *key, const char *value) {
    fprintf(out, ",\"%s\":", key);
    json_escape(out, value);
}

static void label_int(FILE *out, const char *key, int value) {
    fprintf(out, ",\"%s\":\"%d\"", key, value);
}

static void emit_metrics(ap_info *aps, int count, int connected_index, long duration_ms) {
    int first = 1;
    int networks = 0, open_networks = 0, duplicate_ssids = 0;
    int band24 = 0, band5 = 0, band_unknown = 0;
    int strongest = -1000;
    int best_channel = 1;
    int best_score = -1;
    long now = (long)time(NULL);

    for (int i = 0; i < count; i++) {
        if (!ssid_seen_before(aps, i, aps[i].ssid)) {
            networks++;
            if (security_is_open(aps[i].security)) open_networks++;
        } else {
            duplicate_ssids++;
        }
        if (strcmp(aps[i].band, "2.4GHz") == 0) band24++;
        else if (strcmp(aps[i].band, "5GHz") == 0) band5++;
        else band_unknown++;
        if (aps[i].signal_dbm > strongest) strongest = aps[i].signal_dbm;
    }

    for (int ch = 1; ch <= 11; ch++) {
        int overlap = overlap_count_24(aps, count, ch);
        int direct = ap_count_on_channel(aps, count, ch);
        int score = clamp_int(100 - overlap * 12 - direct * 8, 0, 100);
        if (score > best_score) {
            best_score = score;
            best_channel = ch;
        }
    }

    fputs("{\"metrics\":[", stdout);

    metric_prefix(stdout, &first, "beacon_wifi_visible_aps", "gauge", count); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_visible_networks", "gauge", networks); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_open_networks", "gauge", open_networks); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_duplicate_ssid_count", "gauge", duplicate_ssids); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_connected", "gauge", connected_index >= 0 ? 1 : 0); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_strongest_signal_dbm", "gauge", strongest == -1000 ? 0 : strongest); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_24ghz_best_channel", "gauge", best_channel); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_24ghz_overlapping_aps", "gauge", overlap_count_24(aps, count, best_channel)); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_scan_duration_ms", "gauge", duration_ms); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_last_scan_timestamp", "gauge", now); metric_end(stdout);

    for (int i = 0; i < count; i++) {
        metric_prefix(stdout, &first, "beacon_wifi_ap_signal_dbm", "gauge", aps[i].signal_dbm);
        label(stdout, "ssid", aps[i].ssid); label(stdout, "bssid", aps[i].bssid);
        label_int(stdout, "channel", aps[i].channel); label(stdout, "band", aps[i].band);
        label(stdout, "security", aps[i].security); metric_end(stdout);

        metric_prefix(stdout, &first, "beacon_wifi_ap_quality_score", "gauge", aps[i].quality);
        label(stdout, "ssid", aps[i].ssid); label(stdout, "bssid", aps[i].bssid);
        label_int(stdout, "channel", aps[i].channel); label(stdout, "band", aps[i].band);
        label(stdout, "security", aps[i].security); metric_end(stdout);
    }

    if (connected_index >= 0) {
        ap_info *ap = &aps[connected_index];
        metric_prefix(stdout, &first, "beacon_wifi_connected_signal_dbm", "gauge", ap->signal_dbm);
        label(stdout, "ssid", ap->ssid); label(stdout, "bssid", ap->bssid); label_int(stdout, "channel", ap->channel); metric_end(stdout);
        metric_prefix(stdout, &first, "beacon_wifi_connected_rx_rate_mbps", "gauge", ap->rx_rate_mbps);
        label(stdout, "ssid", ap->ssid); metric_end(stdout);
        metric_prefix(stdout, &first, "beacon_wifi_connected_tx_rate_mbps", "gauge", ap->tx_rate_mbps);
        label(stdout, "ssid", ap->ssid); metric_end(stdout);
        metric_prefix(stdout, &first, "beacon_wifi_connected_quality_score", "gauge", ap->quality); metric_end(stdout);
    }

    for (int ch = 1; ch <= 165; ch++) {
        int direct = ap_count_on_channel(aps, count, ch);
        if (direct <= 0 && ch > 11) continue;
        char band[8];
        band_from_channel(ch, band, sizeof(band));
        metric_prefix(stdout, &first, "beacon_wifi_channel_ap_count", "gauge", direct);
        label_int(stdout, "channel", ch); label(stdout, "band", band); metric_end(stdout);
        metric_prefix(stdout, &first, "beacon_wifi_channel_quality_score", "gauge", clamp_int(100 - direct * 12, 0, 100));
        label_int(stdout, "channel", ch); label(stdout, "band", band); metric_end(stdout);
        if (ch <= 11) {
            int overlap = overlap_count_24(aps, count, ch);
            int rec = clamp_int(100 - overlap * 12 - direct * 8, 0, 100);
            metric_prefix(stdout, &first, "beacon_wifi_24ghz_channel_overlap_score", "gauge", clamp_int(overlap * 10, 0, 100));
            label_int(stdout, "channel", ch); metric_end(stdout);
            metric_prefix(stdout, &first, "beacon_wifi_24ghz_channel_recommendation_score", "gauge", rec);
            label_int(stdout, "channel", ch); metric_end(stdout);
            metric_prefix(stdout, &first, "beacon_wifi_24ghz_channel_direct_aps", "gauge", direct);
            label_int(stdout, "channel", ch); metric_end(stdout);
        }
    }

    metric_prefix(stdout, &first, "beacon_wifi_band_ap_count", "gauge", band24); label(stdout, "band", "2.4GHz"); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_band_ap_count", "gauge", band5); label(stdout, "band", "5GHz"); metric_end(stdout);
    metric_prefix(stdout, &first, "beacon_wifi_band_ap_count", "gauge", band_unknown); label(stdout, "band", "unknown"); metric_end(stdout);

    const char *secs[] = {"OPEN", "WPA", "WPA2-PSK", "WPA2-ENT", "WPA3", "UNKNOWN"};
    for (size_t s = 0; s < sizeof(secs) / sizeof(secs[0]); s++) {
        int c = 0;
        for (int i = 0; i < count; i++) if (strcmp(aps[i].security, secs[s]) == 0) c++;
        metric_prefix(stdout, &first, "beacon_wifi_security_ap_count", "gauge", c);
        label(stdout, "security", secs[s]); metric_end(stdout);
    }

    fputs("]}\n", stdout);
}

int main(void) {
    ap_info aps[MAX_APS];
    int connected_index = -1;
    long duration_ms = 0;
    int count;
    char msg[128];
    signal(SIGINT, on_signal);
    signal(SIGTERM, on_signal);
#ifdef _WIN32
    SetConsoleCtrlHandler(console_handler, TRUE);
    count = scan_windows(aps, MAX_APS, &connected_index, &duration_ms);
#else
    count = scan_linux(aps, MAX_APS, &connected_index, &duration_ms);
#endif
    snprintf(msg, sizeof(msg), "Scan complete: %d APs", count);
    log_line("beacon-wifi", msg);
    emit_metrics(aps, count, connected_index, duration_ms);
    return 0;
}
