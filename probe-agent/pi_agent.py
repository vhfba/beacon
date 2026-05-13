# Run:
# python3 pi_agent.py

import json
import os
import ipaddress
import queue
import subprocess
import threading
import time
from typing import Dict, Any
import requests
import yaml
import zipfile
import shutil


class PluginManager:

    def __init__(
        self,
        plugin_dir="./plugins"
    ):

        self.plugin_dir = plugin_dir

        os.makedirs(
            self.plugin_dir,
            exist_ok=True
        )

        self.plugins = {}

    def load_plugins(self):

        self.plugins = {}

        if not os.path.exists(
            self.plugin_dir
        ):
            return

        for name in os.listdir(
            self.plugin_dir
        ):

            path = os.path.join(
                self.plugin_dir,
                name
            )

            manifest_path = os.path.join(
                path,
                "plugin.json"
            )

            if not os.path.exists(
                manifest_path
            ):
                continue

            with open(manifest_path) as f:

                manifest = json.load(f)

            entrypoint = os.path.join(
                path,
                manifest["entrypoint"]
            )

            self.plugins[
                manifest["id"]
            ] = {

                "manifest": manifest,

                "entrypoint":
                    os.path.abspath(
                        entrypoint
                    )
            }

    def install_plugin_zip(
        self,
        zip_path
    ):

        temp_dir = "/tmp/plugin_extract"

        if os.path.exists(temp_dir):
            shutil.rmtree(temp_dir)

        os.makedirs(temp_dir)

        with zipfile.ZipFile(
            zip_path,
            "r"
        ) as zip_ref:

            zip_ref.extractall(
                temp_dir
            )

        manifest_path = os.path.join(
            temp_dir,
            "plugin.json"
        )

        if not os.path.exists(
            manifest_path
        ):
            raise Exception(
                "plugin.json missing"
            )

        with open(manifest_path) as f:
            manifest = json.load(f)

        plugin_id = manifest["id"]

        final_dir = os.path.join(
            self.plugin_dir,
            plugin_id
        )

        if os.path.exists(final_dir):
            shutil.rmtree(final_dir)

        os.makedirs(final_dir)

        for item in os.listdir(temp_dir):

            shutil.move(
                os.path.join(temp_dir, item),
                os.path.join(final_dir, item)
            )

        entrypoint = os.path.join(
            final_dir,
            manifest["entrypoint"]
        )

        os.chmod(
            entrypoint,
            0o755
        )

        self.load_plugins()

    def download_plugin(
        self,
        url
    ):

        local_zip = "/tmp/plugin.zip"

        response = requests.get(
            url,
            timeout=30
        )

        response.raise_for_status()

        with open(local_zip, "wb") as f:
            f.write(response.content)

        self.install_plugin_zip(
            local_zip
        )

    def run_plugin(
        self,
        plugin_id
    ):

        if plugin_id not in self.plugins:
            raise Exception(
                f"Plugin {plugin_id} missing"
            )

        plugin = self.plugins[
            plugin_id
        ]

        manifest = plugin["manifest"]

        result = subprocess.run(

            [plugin["entrypoint"]],

            capture_output=True,

            text=True,

            timeout=manifest.get(
                "timeout_seconds",
                30
            )
        )

        if result.stderr:
            print(
                f"[{plugin_id}] stderr:"
            )
            print(result.stderr)

        if result.returncode != 0:

            raise Exception(
                f"{plugin_id} failed"
            )

        return json.loads(
            result.stdout
        )


CONFIG_PATH = "./configs/config.yaml"

DEFAULT_CONFIG = {
    "device_id": "raspberrypi-001",
    "graphql_url": "http://localhost:4000/graphql",
    "heartbeat_interval": 30,
    "metrics_interval": 60,
    "wifi_interface": "wlan0",
    "ethernet_interface": "eth0",
    "wifi_credentials": {
        "ssid": "",
        "password": ""
    },
    "ethernet_config": {
        "dhcp": True,

        "static": {
            "address": "172.25.20.151",
            "netmask": "255.255.255.0",
            "gateway": "172.25.20.1",
            "dns": [
                "172.25.11.5",
                "172.25.11.6"
            ]
        }
    },
    "enabled_tests": [
        "wifi",
        "ethernet"
    ]

}


class ConfigManager:
    def __init__(self, path=CONFIG_PATH):
        self.path = path
        self.config = self.load()

    def load(self):
        if not os.path.exists(self.path):
            self.save(DEFAULT_CONFIG)
            return DEFAULT_CONFIG

        with open(self.path, "r") as f:
            return yaml.safe_load(f)

    def save(self, config=None):
        if config:
            self.config = config

        with open(self.path, "w") as f:
            yaml.dump(self.config, f)

    def update(self, data: Dict[str, Any]):
        self.config.update(data)
        self.save()

# Graphql


class GraphQLClient:
    def __init__(self, url):
        self.url = url

    def execute(self, query, variables=None):
        payload = {
            "query": query,
            "variables": variables or {}
        }

        response = requests.post(
            self.url,
            json=payload,
            timeout=15
        )

        response.raise_for_status()

        return response.json()


# Mask

def mask_to_cidr(mask):
    return ipaddress.IPv4Network(
        f"0.0.0.0/{mask}"
    ).prefixlen

# Network


class NetworkManager:
    def __init__(self, config):
        self.config = config

    def configure_ethernet(self):

        iface = self.config["ethernet_interface"]

        eth = self.config["ethernet_config"]

        if eth["dhcp"]:

            subprocess.run([
                "sudo",
                "dhclient",
                "-r",
                iface
            ])

            subprocess.run([
                "sudo",
                "dhclient",
                iface
            ])

            return

        static = eth["static"]

        address = static["address"]
        netmask = static["netmask"]
        gateway = static["gateway"]
        dns_servers = static["dns"]

        cidr = mask_to_cidr(netmask)

        subprocess.run([
            "sudo",
            "ip",
            "addr",
            "flush",
            "dev",
            iface
        ])

        subprocess.run([
            "sudo",
            "ip",
            "addr",
            "add",
            f"{address}/{cidr}",
            "dev",
            iface
        ])

        subprocess.run([
            "sudo",
            "ip",
            "link",
            "set",
            iface,
            "up"
        ])

        subprocess.run([
            "sudo",
            "ip",
            "route",
            "replace",
            "default",
            "via",
            gateway
        ])

        with open("/etc/resolv.conf", "w") as f:
            for dns in dns_servers:
                f.write(f"nameserver {dns}\n")

    def ethernet_connected(self):
        iface = self.config["ethernet_interface"]

        try:
            with open(f"/sys/class/net/{iface}/operstate") as f:
                state = f.read().strip()

            return state == "up"

        except Exception:
            return False

    def connect_ethernet(self):
        iface = self.config["ethernet_interface"]

        try:
            self.configure_ethernet()

            if self.config["ethernet_config"]["dhcp"]:

                subprocess.run(
                    ["sudo", "dhclient", iface],
                    check=False
                )

            return self.ethernet_connected()

        except Exception:
            return False

    def wifi_connected(self):
        try:
            output = subprocess.check_output(
                ["iwgetid"]
            ).decode().strip()

            return len(output) > 0
        except Exception:
            return False

    def connect_wifi(self):
        wifi = self.config["wifi_credentials"]

        ssid = wifi["ssid"]
        password = wifi["password"]

        if not ssid:
            return False

        wpa_conf = f'''
network={{
    ssid="{ssid}"
    psk="{password}"
}}
'''

        with open("/tmp/wpa_supplicant.conf", "w") as f:
            f.write(wpa_conf)

        iface = self.config["wifi_interface"]

        subprocess.run([
            "sudo",
            "pkill",
            "wpa_supplicant"
        ], check=False)

        subprocess.run([
            "sudo",
            "wpa_supplicant",
            "-B",
            "-i",
            iface,
            "-c",
            "/tmp/wpa_supplicant.conf"
        ], check=False)

        subprocess.run([
            "sudo",
            "dhclient",
            iface
        ], check=False)

        time.sleep(5)

        return self.wifi_connected()


class PiAgent:
    def __init__(self):

        self.config_manager = ConfigManager()

        self.config = (
            self.config_manager.config
        )

        self.graphql = GraphQLClient(
            self.config["graphql_url"]
        )

        self.network = NetworkManager(
            self.config
        )

        self.metric_queue = queue.Queue()

        # ADD THIS HERE
        self.plugins = PluginManager()

    def sync_plugins(self):

        query = """
        query Plugins {
            plugins {
                id
                version
                download_url
            }
        }
        """

        try:

            response = self.graphql.execute(
                query
            )

            plugins = response[
                "data"
            ]["plugins"]

            for plugin in plugins:

                plugin_id = plugin["id"]

                local = self.plugins.plugins.get(plugin_id)

                needs_update = (
                    local is None
                    or local["manifest"]["version"]
                    != plugin["version"]
                )

                if needs_update:

                    print(
                        f"Installing {plugin_id}"
                    )

                    self.plugins.download_plugin(
                        plugin["download_url"]
                    )

        except Exception as e:

            print(
                "Plugin sync failed:",
                e
            )

    # Heartbeat

    def send_heartbeat(self):
        mutation = """
        mutation Heartbeat($deviceId: String!) {
            heartbeat(deviceId: $deviceId)
        }
        """

        variables = {
            "deviceId": self.config["device_id"]
        }

        try:
            response = self.graphql.execute(
                mutation,
                variables
            )

            print("Heartbeat sent:", response)

        except Exception as e:
            print("Heartbeat failed:", e)

    # Send Metrics

    def send_metrics(self, payload):
        mutation = """
        mutation SendMetrics($input: MetricInput!) {
            sendMetrics(input: $input)
        }
        """

        variables = {
            "input": payload
        }

        try:
            response = self.graphql.execute(
                mutation,
                variables
            )

            print("Metrics sent:", response)

        except Exception as e:
            print("Metric send failed:", e)
            self.metric_queue.put(payload)

    # Get configs from server

    def fetch_remote_config(self):
        query = """
        query DeviceConfig($deviceId: String!) {
            deviceConfig(deviceId: $deviceId) {
                enabled_tests
                wifi_credentials {
                    ssid
                    password
                }
            }
        }
        """

        variables = {
            "deviceId": self.config["device_id"]
        }

        try:
            response = self.graphql.execute(
                query,
                variables
            )

            data = response["data"]["deviceConfig"]

            self.config["enabled_tests"] = data["enabled_tests"]

            if data.get("wifi_credentials"):
                self.config["wifi_credentials"] = data["wifi_credentials"]

            self.config_manager.save(self.config)

            print("Remote config updated")

        except Exception as e:
            print("Failed fetching config:", e)

    # Network

    def ensure_connectivity(self):
        if self.network.ethernet_connected():
            return True

        if self.network.connect_ethernet():
            print("Ethernet connected")
            return True

        if self.network.wifi_connected():
            return True

        if self.network.connect_wifi():
            print("WiFi connected")
            return True

        return False

    # Tests

    def run_tests(self):

        enabled = self.config[
            "enabled_tests"
        ]

        payload = {

            "device_id":
                self.config[
                    "device_id"
                ],

            "timestamp":
                int(time.time()),

            "metrics": {}
        }

        for plugin_id in enabled:

            try:

                metrics = (
                    self.plugins.run_plugin(
                        plugin_id
                    )
                )

                payload["metrics"][
                    plugin_id
                ] = metrics

            except Exception as e:

                print(
                    f"{plugin_id} failed:",
                    e
                )

        self.send_metrics(payload)

    # Heartbeat loop

    def heartbeat_loop(self):
        while True:
            if self.ensure_connectivity():
                self.send_heartbeat()
                self.fetch_remote_config()
                self.sync_plugins()

            time.sleep(
                self.config["heartbeat_interval"]
            )

    # Metrics loop

    def metrics_loop(self):
        while True:
            if self.ensure_connectivity():
                self.run_tests()

            time.sleep(
                self.config["metrics_interval"]
            )

    def retry_loop(self):

        while True:

            try:

                payload = self.metric_queue.get(
                    timeout=5
                )

                try:

                    self.send_metrics(payload)

                except Exception:

                    self.metric_queue.put(
                        payload
                    )

            except queue.Empty:
                pass

            time.sleep(5)

    def start(self):
        print("Starting Pi Agent")

        threading.Thread(
            target=self.heartbeat_loop,
            daemon=True
        ).start()

        threading.Thread(
            target=self.metrics_loop,
            daemon=True
        ).start()

        threading.Thread(
            target=self.retry_loop,
            daemon=True
        ).start()

        while True:
            time.sleep(1)

# main


if __name__ == "__main__":
    agent = PiAgent()
    agent.start()
