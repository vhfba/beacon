// gcc beacon_wifi_scan.c -o beacon_wifi_scan

#include <ctype.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define MAX_CHANNELS 196
#define MAX_SSIDS 256

struct ssid_seen {
    char ssid[96];
};

struct channel_count {
    int channel;
    int count;
};

static void trim(char *text)
{
    char *start = text;
    while (*start && isspace((unsigned char)*start)) start++;
    if (start != text) memmove(text, start, strlen(start) + 1);

    size_t len = strlen(text);
    while (len > 0 && isspace((unsigned char)text[len - 1])) {
        text[len - 1] = '\0';
        len--;
    }
}

static int freq_to_channel(int freq)
{
    if (freq == 2484) return 14;
    if (freq >= 2412 && freq <= 2472) return (freq - 2407) / 5;
    if (freq >= 5000 && freq <= 5900) return (freq - 5000) / 5;
    if (freq >= 5925 && freq <= 7125) return (freq - 5950) / 5;
    return 0;
}

static const char *band_for_frequency(int freq)
{
    if (freq >= 2400 && freq < 2500) return "2.4GHz";
    if (freq >= 5000 && freq < 5925) return "5GHz";
    if (freq >= 5925 && freq < 7125) return "6GHz";
    return "unknown";
}

static int ssid_exists(struct ssid_seen *items, int count, const char *ssid)
{
    for (int i = 0; i < count; i++) {
        if (strcmp(items[i].ssid, ssid) == 0) return 1;
    }
    return 0;
}

static void add_channel(struct channel_count *items, int *count, int channel)
{
    if (channel <= 0) return;
    for (int i = 0; i < *count; i++) {
        if (items[i].channel == channel) {
            items[i].count++;
            return;
        }
    }
    if (*count < MAX_CHANNELS) {
        items[*count].channel = channel;
        items[*count].count = 1;
        (*count)++;
    }
}

static int parse_connected_signal(const char *iface, int *signal_dbm, int *frequency)
{
    char cmd[160];
    snprintf(cmd, sizeof(cmd), "iw dev %s link 2>/dev/null", iface);

    FILE *fp = popen(cmd, "r");
    if (!fp) return 0;

    char line[256];
    int found = 0;
    while (fgets(line, sizeof(line), fp)) {
        trim(line);
        if (sscanf(line, "signal: %d dBm", signal_dbm) == 1) found = 1;
        if (sscanf(line, "freq: %d", frequency) == 1) found = 1;
    }

    pclose(fp);
    return found;
}

int main(void)
{
    const char *iface = getenv("BEACON_WIFI_INTERFACE");
    if (!iface || !*iface) iface = "wlan0";

    char cmd[192];
    snprintf(cmd, sizeof(cmd), "iw dev %s scan 2>/dev/null", iface);

    FILE *fp = popen(cmd, "r");
    int ap_count = 0;
    int ssid_count = 0;
    int channel_count = 0;
    int current_freq = 0;
    int strongest = -1000;
    struct ssid_seen ssids[MAX_SSIDS];
    struct channel_count channels[MAX_CHANNELS];

    memset(ssids, 0, sizeof(ssids));
    memset(channels, 0, sizeof(channels));

    if (fp) {
        char line[512];
        while (fgets(line, sizeof(line), fp)) {
            trim(line);

            if (strncmp(line, "BSS ", 4) == 0) {
                ap_count++;
                current_freq = 0;
            } else if (sscanf(line, "freq: %d", &current_freq) == 1) {
                add_channel(channels, &channel_count, freq_to_channel(current_freq));
            } else if (strncmp(line, "SSID:", 5) == 0) {
                char ssid[96];
                snprintf(ssid, sizeof(ssid), "%s", line + 5);
                trim(ssid);
                if (ssid[0] && !ssid_exists(ssids, ssid_count, ssid) && ssid_count < MAX_SSIDS) {
                    snprintf(ssids[ssid_count].ssid, sizeof(ssids[ssid_count].ssid), "%s", ssid);
                    ssid_count++;
                }
            } else {
                int signal = 0;
                if (sscanf(line, "signal: %d", &signal) == 1 && signal > strongest) {
                    strongest = signal;
                }
            }
        }
        pclose(fp);
    }

    int connected_signal = 0;
    int connected_freq = 0;
    int connected = parse_connected_signal(iface, &connected_signal, &connected_freq);

    printf("{\"metrics\":[");
    printf("{\"name\":\"beacon_wifi_visible_aps\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}},", ap_count, iface);
    printf("{\"name\":\"beacon_wifi_visible_networks\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}},", ssid_count, iface);
    printf("{\"name\":\"beacon_wifi_strongest_signal_dbm\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}}", strongest == -1000 ? 0 : strongest, iface);

    if (connected) {
        printf(",{\"name\":\"beacon_wifi_connected\",\"kind\":\"gauge\",\"value\":1,\"labels\":{\"interface\":\"%s\",\"band\":\"%s\"}}", iface, band_for_frequency(connected_freq));
        printf(",{\"name\":\"beacon_wifi_connected_signal_dbm\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}}", connected_signal, iface);
        printf(",{\"name\":\"beacon_wifi_connected_frequency_mhz\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\"}}", connected_freq, iface);
    } else {
        printf(",{\"name\":\"beacon_wifi_connected\",\"kind\":\"gauge\",\"value\":0,\"labels\":{\"interface\":\"%s\",\"band\":\"unknown\"}}", iface);
    }

    for (int i = 0; i < channel_count; i++) {
        printf(",{\"name\":\"beacon_wifi_channel_ap_count\",\"kind\":\"gauge\",\"value\":%d,\"labels\":{\"interface\":\"%s\",\"channel\":\"%d\"}}",
               channels[i].count, iface, channels[i].channel);
    }

    printf("]}\n");
    return 0;
}
