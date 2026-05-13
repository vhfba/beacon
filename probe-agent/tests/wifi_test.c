// gcc wifi_test.c -o wifi_test -lnl-3 -lnl-genl-3

// Standard C libraries
#include <stdint.h>
#include <stdio.h>
#include <ctype.h>
#include <errno.h>
#include <inttypes.h>
#include <unistd.h>
// Netlink libraries
#include <netlink/handlers.h>
#include <netlink/netlink.h>
#include <netlink/socket.h>
#include <netlink/genl/genl.h>
#include <netlink/genl/ctrl.h>
#include <netlink/attr.h>
#include <netlink/msg.h>
#include <linux/genetlink.h>
#include <linux/netlink.h>
// nl80211 libraries
#include <linux/nl80211.h>
// Network Interface library
#include <net/if.h>

#define MAX_RESULTS 256
#define PORT 9105

struct wifi_data
{
    int frequency;
    char bssid[20];
    char ssid[20];
    int mbm;
    int msAgo;
    int chanWidth;
    int chanNumber;
    // char parentBssid[20];
};

static struct wifi_data dataResult[MAX_RESULTS];
static int result_count = 0;

struct trigger_results
{
    int done;
    int aborted;
};

static int error_handler(struct sockaddr_nl *nla, struct nlmsgerr *err, void *arg)
{
    // Callback for errors.
    printf("error_handler() called.\n");
    int *ret = arg;
    *ret = err->error;
    return NL_STOP;
}

static int finish_handler(struct nl_msg *msg, void *arg)
{
    // Callback for NL_CB_FINISH.
    int *ret = arg;
    *ret = 0;
    return NL_SKIP;
}

static int ack_handler(struct nl_msg *msg, void *arg)
{
    // Callback for NL_CB_ACK.
    int *ret = arg;
    *ret = 0;
    return NL_STOP;
}

static int no_seq_check(struct nl_msg *msg, void *arg)
{
    // Callback for NL_CB_SEQ_CHECK.
    return NL_OK;
}

void get_bssid(char *mac_addr, unsigned char *arg)
{
    sprintf(mac_addr, "%02x:%02x:%02x:%02x:%02x:%02x",
            arg[0], arg[1], arg[2],
            arg[3], arg[4], arg[5]);
}

void get_ssid(char *out, unsigned char *ie, int ielen)
{
    while (ielen >= 2 && ielen >= ie[1])
    {
        if (ie[0] == 0 && ie[1] <= 32)
        {
            int len = ie[1];
            unsigned char *data = ie + 2;
            int pos = 0;

            for (int i = 0; i < len; i++)
            {
                if (isprint(data[i]) && data[i] != '\\')
                    out[pos++] = data[i];
                else
                    out[pos++] = '?';
            }
            out[pos] = '\0';
            return;
        }
        ielen -= ie[1] + 2;
        ie += ie[1] + 2;
    }
    strcpy(out, "hidden");
}

static int callback_trigger(struct nl_msg *msg, void *arg)
{
    // Called by the kernel when the scan is done or has been aborted.
    struct genlmsghdr *gnlh = nlmsg_data(nlmsg_hdr(msg));
    struct trigger_results *results = arg;

    // printf("Got something.\n");
    // printf("%d\n", arg);
    // nl_msg_dump(msg, stdout);

    if (gnlh->cmd == NL80211_CMD_SCAN_ABORTED)
    {
        // printf("Got NL80211_CMD_SCAN_ABORTED.\n");
        results->done = 1;
        results->aborted = 1;
    }
    else if (gnlh->cmd == NL80211_CMD_NEW_SCAN_RESULTS)
    {
        // printf("Got NL80211_CMD_NEW_SCAN_RESULTS.\n");
        results->done = 1;
        results->aborted = 0;
    } // else probably an uninteresting multicast message.

    return NL_SKIP;
}

static int callback_dump(struct nl_msg *msg, void *arg)
{
    // Called by the kernel with a dump of the successful scan's data. Called for each SSID.
    struct genlmsghdr *gnlh = nlmsg_data(nlmsg_hdr(msg));
    struct nlattr *tb[NL80211_ATTR_MAX + 1];
    struct nlattr *bss[NL80211_BSS_MAX + 1];
    static struct nla_policy bss_policy[NL80211_BSS_MAX + 1] = {
        [NL80211_BSS_FREQUENCY] = {.type = NLA_U32},
        [NL80211_BSS_BSSID] = {},
        [NL80211_BSS_INFORMATION_ELEMENTS] = {},
        [NL80211_BSS_SIGNAL_MBM] = {.type = NLA_U32},
        [NL80211_BSS_SEEN_MS_AGO] = {.type = NLA_U32},
        //[NL80211_BSS_PARENT_BSSID] = { .type = NLA_U64 },
    };

    // Parsing
    nla_parse(tb, NL80211_ATTR_MAX, genlmsg_attrdata(gnlh, 0), genlmsg_attrlen(gnlh, 0), NULL);
    // Error checking
    if (!tb[NL80211_ATTR_BSS])
    {
        printf("bss info missing!\n");
        return NL_SKIP;
    }
    if (nla_parse_nested(bss, NL80211_BSS_MAX, tb[NL80211_ATTR_BSS], bss_policy))
    {
        printf("failed to parse nested attributes!\n");
        return NL_SKIP;
    }
    if (!bss[NL80211_BSS_BSSID])
    {
        printf("failed to parse nested attributes! | bss info missing! \n");
        return NL_SKIP;
    }
    if (!bss[NL80211_BSS_INFORMATION_ELEMENTS])
    {
        printf("failed to parse nested attributes! | information elements info missing! \n");
        return NL_SKIP;
    }

    if (result_count >= MAX_RESULTS)
        return NL_SKIP;

    /*
        [NL80211_BSS_FREQUENCY] = { .type = NLA_U32 },
        [NL80211_BSS_BSSID] = { },
        [NL80211_BSS_INFORMATION_ELEMENTS] = { },
        [NL80211_BSS_SIGNAL_MBM] = { .type = NLA_U32 },
        [NL80211_BSS_SEEN_MS_AGO] = { .type = NLA_U32 },
        [NL80211_BSS_CHAN_WIDTH] = { .type = NLA_U32 },
        [NL80211_BSS_PARENT_BSSID] = { .type = NLA_U64 },
    */

    // Save data in variables

    if (bss[NL80211_BSS_FREQUENCY])
        dataResult[result_count].frequency = nla_get_u32(bss[NL80211_BSS_FREQUENCY]);
    get_bssid(dataResult[result_count].bssid, nla_data(bss[NL80211_BSS_BSSID]));
    get_ssid(dataResult[result_count].ssid, nla_data(bss[NL80211_BSS_INFORMATION_ELEMENTS]), nla_len(bss[NL80211_BSS_INFORMATION_ELEMENTS]));
    if (bss[NL80211_BSS_SIGNAL_MBM])
        dataResult[result_count].mbm = nla_get_u32(bss[NL80211_BSS_SIGNAL_MBM]);
    if (bss[NL80211_BSS_SEEN_MS_AGO])
        dataResult[result_count].msAgo = nla_get_u32(bss[NL80211_BSS_SEEN_MS_AGO]);
    result_count++;
    return NL_SKIP;
}

int do_scan_trigger(struct nl_sock *socket, int if_index, int driver_id)
{

    // result_count = 0;

    // Starts the scan and waits for it to finish. Does not return until the scan is done or has been aborted.
    struct trigger_results results = {.done = 0, .aborted = 0};
    struct nl_msg *msg;
    struct nl_cb *cb;
    struct nl_msg *ssids_to_scan;
    int err;
    int ret;
    int mcid = genl_ctrl_resolve_grp(socket, "nl80211", "scan");
    nl_socket_add_membership(socket, mcid); // Without this, callback_trigger() won't be called.

    // Allocate the messages and callback handler.
    msg = nlmsg_alloc();
    if (!msg)
    {
        printf("ERROR: Failed to allocate netlink message for msg.\n");
        return -ENOMEM;
    }
    ssids_to_scan = nlmsg_alloc();
    if (!ssids_to_scan)
    {
        printf("ERROR: Failed to allocate netlink message for ssids_to_scan.\n");
        nlmsg_free(msg);
        return -ENOMEM;
    }
    cb = nl_cb_alloc(NL_CB_DEFAULT);
    if (!cb)
    {
        printf("ERROR: Failed to allocate netlink callbacks.\n");
        nlmsg_free(msg);
        nlmsg_free(ssids_to_scan);
        return -ENOMEM;
    }

    // Setup the messages and callback handler.
    genlmsg_put(msg, 0, 0, driver_id, 0, 0, NL80211_CMD_TRIGGER_SCAN, 0); // Setup which command to run.
    nla_put_u32(msg, NL80211_ATTR_IFINDEX, if_index);                     // Add message attribute, which interface to use.
    nla_put_u32(msg, NL80211_ATTR_SCAN_FLAGS, NL80211_SCAN_FLAG_FLUSH);
    const char *target_ssid = "eduroam";
    nla_put_nested(msg, NL80211_ATTR_SCAN_SSIDS, ssids_to_scan);
    nla_put(ssids_to_scan, 1, 0, ""); // Scan all SSIDs.
    // nla_put(ssids_to_scan, 1, strlen(target_ssid), target_ssid);
    // nla_put_nested(msg, NL80211_ATTR_SCAN_SSIDS, ssids_to_scan);  // Add message attribute, which SSIDs to scan for.
    /*
     *   Like with normal scans, if SSIDs (%NL80211_ATTR_SCAN_SSIDS)
     *	are passed, they are used in the probe requests.  For
     *	broadcast, a broadcast SSID must be passed (ie. an empty
     *	string).  If no SSID is passed, no probe requests are sent and
     *	a passive scan is performed.
     */
    nlmsg_free(ssids_to_scan);                                            // Copied to `msg` above, no longer need this.
    nl_cb_set(cb, NL_CB_VALID, NL_CB_CUSTOM, callback_trigger, &results); // Add the callback.
    nl_cb_err(cb, NL_CB_CUSTOM, error_handler, &err);
    nl_cb_set(cb, NL_CB_FINISH, NL_CB_CUSTOM, finish_handler, &err);
    nl_cb_set(cb, NL_CB_ACK, NL_CB_CUSTOM, ack_handler, &err);
    nl_cb_set(cb, NL_CB_SEQ_CHECK, NL_CB_CUSTOM, no_seq_check, NULL); // No sequence checking for multicast messages.

    // Send NL80211_CMD_TRIGGER_SCAN to start the scan. The kernel may reply with NL80211_CMD_NEW_SCAN_RESULTS on
    // success or NL80211_CMD_SCAN_ABORTED if another scan was started by another process.
    err = 1;
    ret = nl_send_auto(socket, msg); // Send the message.
    // printf("NL80211_CMD_TRIGGER_SCAN sent %d bytes to the kernel.\n", ret);
    // printf("Waiting for scan to complete...\n");
    while (err > 0)
        ret = nl_recvmsgs(socket, cb); // First wait for ack_handler(). This helps with basic errors.
    if (err < 0)
    {
        printf("WARNING: err has a value of %d.\n", err);
    }
    if (ret < 0)
    {
        printf("ERROR: nl_recvmsgs() returned %d (%s).\n", ret, nl_geterror(-ret));
        return ret;
    }
    while (!results.done)
        nl_recvmsgs(socket, cb); // Now wait until the scan is done or aborted.
    if (results.aborted)
    {
        printf("ERROR: Kernel aborted scan.\n");
        return 1;
    }
    // printf("Scan is done.\n");

    // Cleanup.
    nlmsg_free(msg);
    nl_cb_put(cb);
    nl_socket_drop_membership(socket, mcid); // No longer need this.
    return 0;
}

void metrics()
{
    printf("{\"wifi_networks\":[");
    for (int i = 0; i < result_count; i++)
    {

        printf(
            "{\"ssid\":\"%s\","
            "\"bssid\":\"%s\","
            "\"frequency\":%d,"
            "\"signal\":%d,"
            "\"msAgo\":%d}",
            dataResult[i].ssid,
            dataResult[i].bssid,
            dataResult[i].frequency,
            dataResult[i].mbm,
            dataResult[i].msAgo);

        if (i < result_count - 1)
            printf(",");
    }
    printf("]}\n");
}

void perform_scan(struct nl_sock *socket, int if_index, int driver_id)
{
    result_count = 0;

    // Now get info for all SSIDs detected.
    struct nl_msg *msg = nlmsg_alloc();                                          // Allocate a message.
    genlmsg_put(msg, 0, 0, driver_id, 0, NLM_F_DUMP, NL80211_CMD_GET_SCAN, 0);   // Setup which command to run.
    nla_put_u32(msg, NL80211_ATTR_IFINDEX, if_index);                            // Add message attribute, which interface to use.
    nl_socket_modify_cb(socket, NL_CB_VALID, NL_CB_CUSTOM, callback_dump, NULL); // Add the callback.
    int ret = nl_send_auto(socket, msg);                                         // Send the message.
    // printf("NL80211_CMD_GET_SCAN sent %d bytes to the kernel.\n", ret);
    ret = nl_recvmsgs_default(socket); // Retrieve the kernel's answer. callback_dump() prints SSIDs to stdout.
    nlmsg_free(msg);
    /*if (ret < 0) {
        printf("ERROR: nl_recvmsgs_default() returned %d (%s).\n", ret, nl_geterror(-ret));
        return ret;
    }*/
}

int main()
{
    int if_index = if_nametoindex("wlan0"); // Use this wireless interface for scanning.

    // Open socket to kernel.
    struct nl_sock *socket = nl_socket_alloc();           // Allocate new netlink socket in memory.
    genl_connect(socket);                                 // Create file descriptor and bind socket.
    int driver_id = genl_ctrl_resolve(socket, "nl80211"); // Find the nl80211 driver ID.

    // Issue NL80211_CMD_TRIGGER_SCAN to the kernel and wait for it to finish.
    int err = do_scan_trigger(socket, if_index, driver_id);
    if (err != 0)
    {
        fprintf(stderr, "do_scan_trigger() failed with %d.\n", err);
        return err;
    }

    perform_scan(socket, if_index, driver_id);

    metrics();

    return 0;
}
