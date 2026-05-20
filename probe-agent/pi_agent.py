# Run:
# python3 pi_agent.py

import json
import os
import hashlib
import ipaddress
import html
import platform
import queue
import random
import re
import subprocess
import threading
import time
import socket
import tempfile
import urllib.parse
from datetime import datetime, timezone
from typing import Dict, Any
import requests
import yaml
import zipfile
import shutil


class PluginManager:

    def __init__(
        self,
        plugin_dir=None
    ):

        base_dir = os.path.dirname(os.path.abspath(__file__))
        self.plugin_dir = plugin_dir or os.path.join(base_dir, "plugins")

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

            manifest_path = self._manifest_path(path)

            if manifest_path is None:
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

    def _manifest_path(self, path):
        for filename in ("manifest.json", "plugin.json"):
            candidate = os.path.join(path, filename)
            if os.path.exists(candidate):
                return candidate

        return None

    def install_plugin_zip(
        self,
        zip_path
    ):

        temp_dir = tempfile.mkdtemp(prefix="beacon_plugin_")

        with zipfile.ZipFile(
            zip_path,
            "r"
        ) as zip_ref:

            zip_ref.extractall(
                temp_dir
            )

        manifest_path = self._manifest_path(temp_dir)

        if manifest_path is None:
            raise Exception(
                "manifest.json missing"
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
        shutil.rmtree(temp_dir, ignore_errors=True)

    def download_plugin(
        self,
        url,
        checksum=None,
        api_key=None
    ):

        headers = {}
        if api_key:
            headers["X-Api-Key"] = api_key

        response = requests.get(
            url,
            headers=headers,
            timeout=30
        )

        response.raise_for_status()

        bundle = response.content
        if checksum and is_sha256(checksum):
            actual = hashlib.sha256(bundle).hexdigest()
            if actual.lower() != checksum.lower():
                raise Exception(
                    f"Plugin checksum mismatch: expected {checksum}, got {actual}"
                )

        fd, local_zip = tempfile.mkstemp(
            prefix="beacon_plugin_",
            suffix=".zip"
        )

        with os.fdopen(fd, "wb") as f:
            f.write(response.content)

        self.install_plugin_zip(
            local_zip
        )
        os.remove(local_zip)

    def run_plugin(
        self,
        plugin_id,
        context=None
    ):

        if plugin_id not in self.plugins:
            raise Exception(
                f"Plugin {plugin_id} missing"
            )

        plugin = self.plugins[
            plugin_id
        ]

        manifest = plugin["manifest"]

        env = os.environ.copy()
        env["BEACON_PLUGIN_CONTEXT"] = json.dumps(
            context or {}
        )

        result = subprocess.run(

            [plugin["entrypoint"]],

            input=json.dumps(context or {}),

            capture_output=True,

            text=True,

            env=env,

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


BASE_DIR = os.path.dirname(os.path.abspath(__file__))
CONFIG_PATH = os.path.join(BASE_DIR, "configs", "config.yaml")

DEFAULT_CONFIG = {
    "device_id": "raspberrypi-001",
    "probe_name": "raspberrypi-001",
    "probe_location": "Building A",
    "probe_ssid": "",
    "agent_version": "1.0.0-pi",
    "graphql_url": "http://localhost:5000/graphql",
    "api_key": "",
    "heartbeat_interval": 30,
    "metrics_interval": 5,
    "action_poll_interval": 10,
    "control_poll_interval": 10,
    "network_backend": "auto",
    "allow_mock_network": False,
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
    }

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
    def __init__(self, url, api_key=""):
        self.url = url
        self.api_key = api_key

    def execute(self, query, variables=None):
        payload = {
            "query": query,
            "variables": variables or {}
        }

        headers = {}
        if self.api_key:
            headers["X-Api-Key"] = self.api_key

        response = requests.post(
            self.url,
            json=payload,
            headers=headers,
            timeout=15
        )

        response.raise_for_status()

        data = response.json()
        if data.get("errors"):
            raise Exception(
                "GraphQL errors: "
                + json.dumps(data["errors"])
            )

        return data


# Mask

def mask_to_cidr(mask):
    return ipaddress.IPv4Network(
        f"0.0.0.0/{mask}"
    ).prefixlen


def is_sha256(value):
    return (
        isinstance(value, str)
        and len(value) == 64
        and all(ch in "0123456789abcdefABCDEF" for ch in value)
    )


def resolve_local_ip():
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
            sock.connect(("8.8.8.8", 80))
            return sock.getsockname()[0]
    except OSError:
        return "127.0.0.1"


def utc_now_iso():
    return datetime.now(timezone.utc).isoformat()


def metric_labels(config, plugin_id):
    return [
        {"key": "probe_id", "value": str(config["device_id"])},
        {"key": "site", "value": str(config.get("probe_location", "unknown"))},
        {"key": "test_type", "value": str(plugin_id).upper()},
    ]


def absolute_url(url, graphql_url):
    parsed = urllib.parse.urlparse(url)
    if parsed.scheme and parsed.netloc:
        return url

    base = graphql_url.rsplit("/graphql", 1)[0].rstrip("/")
    return urllib.parse.urljoin(
        base + "/",
        url.lstrip("/")
    )

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

        if not address or not netmask or not gateway:
            return

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
        return bool(self.current_wifi_ssid())

    def connect_wifi(self):
        wifi = self.config["wifi_credentials"]

        ssid = (wifi.get("ssid") or "").strip()
        password = wifi.get("password") or ""

        if not ssid:
            return False

        backend = self.selected_wifi_backend()

        if backend == "mock":
            self.config["probe_ssid"] = ssid
            return True

        if backend == "windows-netsh":
            return self._connect_wifi_windows(ssid, password)

        if backend == "linux-nmcli":
            return self._connect_wifi_nmcli(ssid, password)

        if backend == "linux-wpa-supplicant":
            return self._connect_wifi_wpa_supplicant(ssid, password)

        raise Exception(
            "No supported Wi-Fi backend found. Install nmcli/iwlist on Linux "
            "or use Windows WLAN AutoConfig/netsh. Set allow_mock_network: true "
            "only for demo mode."
        )

    def scan_wifi_networks(self):
        backend = self.selected_wifi_backend()

        if backend == "mock":
            return self._scan_wifi_mock()

        if backend == "windows-netsh":
            return self._scan_wifi_windows()

        if backend == "linux-nmcli":
            return self._scan_wifi_nmcli()

        if backend == "linux-wpa-supplicant":
            return self._scan_wifi_iwlist()

        raise Exception(
            "No supported Wi-Fi backend found. Install nmcli/iwlist on Linux "
            "or use Windows WLAN AutoConfig/netsh. Set allow_mock_network: true "
            "only for demo mode."
        )

    def selected_wifi_backend(self):
        requested = str(
            self.config.get("network_backend", "auto")
        ).strip().lower()

        if requested in ("mock", "simulated"):
            return "mock"

        if requested in ("windows", "windows-netsh", "netsh"):
            return "windows-netsh" if self._is_windows() else None

        if requested in ("nmcli", "linux-nmcli"):
            return "linux-nmcli" if shutil.which("nmcli") else None

        if requested in ("iwlist", "wpa", "linux-wpa-supplicant"):
            return (
                "linux-wpa-supplicant"
                if shutil.which("iwlist") or shutil.which("iwgetid")
                else None
            )

        if self._is_windows() and shutil.which("netsh"):
            return "windows-netsh"

        if shutil.which("nmcli"):
            return "linux-nmcli"

        if shutil.which("iwlist") or shutil.which("iwgetid"):
            return "linux-wpa-supplicant"

        if self.config.get("allow_mock_network"):
            return "mock"

        return None

    def current_wifi_ssid(self):
        backend = self.selected_wifi_backend()

        if backend == "mock":
            return (
                self.config.get("probe_ssid")
                or self.config.get("wifi_credentials", {}).get("ssid")
                or None
            )

        if backend == "windows-netsh":
            return self._current_wifi_ssid_windows()

        if backend == "linux-nmcli":
            return self._current_wifi_ssid_nmcli()

        if backend == "linux-wpa-supplicant":
            return self._current_wifi_ssid_iwgetid()

        return None

    def wifi_backend_name(self):
        return self.selected_wifi_backend() or "unavailable"

    def _is_windows(self):
        return os.name == "nt" or platform.system().lower() == "windows"

    def _run_command(self, args, timeout=20, check=False):
        return subprocess.run(
            args,
            capture_output=True,
            text=True,
            errors="ignore",
            timeout=timeout,
            check=check
        )

    def _dedupe_networks(self, networks):
        by_ssid = {}
        current_ssid = self.current_wifi_ssid()

        for network in networks:
            ssid = (network.get("ssid") or "").strip()
            if not ssid:
                continue

            network["ssid"] = ssid
            network["connected"] = ssid == current_ssid

            existing = by_ssid.get(ssid)
            if (
                existing is None
                or network.get("signalDbm", -100)
                > existing.get("signalDbm", -100)
            ):
                by_ssid[ssid] = network

        result = list(by_ssid.values())
        result.sort(
            key=lambda item: item.get("signalDbm", -100),
            reverse=True
        )
        return result[:20]

    def _signal_percent_to_dbm(self, signal):
        try:
            signal = max(0, min(100, int(signal)))
            return round((signal / 2) - 100, 1)
        except Exception:
            return None

    def _scan_wifi_windows(self):
        result = self._run_command(
            ["netsh", "wlan", "show", "networks", "mode=bssid"],
            timeout=25
        )

        if result.returncode != 0:
            raise Exception(
                result.stderr.strip()
                or result.stdout.strip()
                or "netsh Wi-Fi scan failed"
            )

        networks = []
        current = {}

        for raw_line in result.stdout.splitlines():
            line = raw_line.strip()
            if not line or ":" not in line:
                continue

            key, value = [
                part.strip()
                for part in line.split(":", 1)
            ]
            key_lower = key.lower()

            if re.match(r"^ssid\s+\d+$", key_lower):
                if current.get("ssid"):
                    networks.append(current)
                current = {
                    "ssid": value,
                    "security": "UNKNOWN"
                }
                continue

            if not current:
                continue

            if key_lower.startswith("auth") or key_lower.startswith("autentica"):
                current["security"] = value or "UNKNOWN"
            elif key_lower in ("channel", "canal"):
                try:
                    current["channel"] = int(value)
                except ValueError:
                    pass
            elif key_lower in ("signal", "sinal"):
                percent = int(re.sub(r"[^0-9]", "", value) or "0")
                current["signalPercent"] = percent
                current["signalDbm"] = self._signal_percent_to_dbm(percent)

        if current.get("ssid"):
            networks.append(current)

        return self._dedupe_networks(networks)

    def _current_wifi_ssid_windows(self):
        result = self._run_command(
            ["netsh", "wlan", "show", "interfaces"],
            timeout=10
        )

        if result.returncode != 0:
            return None

        connected = False
        ssid = None

        for raw_line in result.stdout.splitlines():
            line = raw_line.strip()
            if ":" not in line:
                continue

            key, value = [
                part.strip()
                for part in line.split(":", 1)
            ]
            key_lower = key.lower()
            value_lower = value.lower()

            if key_lower in ("state", "estado"):
                connected = (
                    "connected" in value_lower
                    or "ligado" in value_lower
                    or "conectado" in value_lower
                )
            elif key_lower == "ssid":
                ssid = value

        return ssid if connected and ssid else None

    def _connect_wifi_windows(self, ssid, password):
        self._add_windows_wifi_profile(ssid, password)

        result = self._run_command(
            ["netsh", "wlan", "connect", f"name={ssid}", f"ssid={ssid}"],
            timeout=20
        )

        if result.returncode != 0:
            raise Exception(
                result.stderr.strip()
                or result.stdout.strip()
                or "netsh Wi-Fi connect failed"
            )

        for _ in range(12):
            if self._current_wifi_ssid_windows() == ssid:
                self.config["probe_ssid"] = ssid
                return True
            time.sleep(1)

        return False

    def _add_windows_wifi_profile(self, ssid, password):
        safe_ssid = html.escape(ssid)

        if password:
            auth_xml = """
        <authentication>WPA2PSK</authentication>
        <encryption>AES</encryption>
        <useOneX>false</useOneX>"""
            shared_key_xml = f"""
        <sharedKey>
            <keyType>passPhrase</keyType>
            <protected>false</protected>
            <keyMaterial>{html.escape(password)}</keyMaterial>
        </sharedKey>"""
        else:
            auth_xml = """
        <authentication>open</authentication>
        <encryption>none</encryption>
        <useOneX>false</useOneX>"""
            shared_key_xml = ""

        profile = f"""<?xml version="1.0"?>
<WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
    <name>{safe_ssid}</name>
    <SSIDConfig>
        <SSID>
            <name>{safe_ssid}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>{auth_xml}
            </authEncryption>{shared_key_xml}
        </security>
    </MSM>
</WLANProfile>
"""

        fd, profile_path = tempfile.mkstemp(
            prefix="beacon_wifi_",
            suffix=".xml"
        )

        try:
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                f.write(profile)

            result = self._run_command(
                ["netsh", "wlan", "add", "profile", f"filename={profile_path}", "user=current"],
                timeout=20
            )

            if result.returncode != 0:
                raise Exception(
                    result.stderr.strip()
                    or result.stdout.strip()
                    or "netsh Wi-Fi profile creation failed"
                )
        finally:
            try:
                os.remove(profile_path)
            except OSError:
                pass

    def _scan_wifi_nmcli(self):
        iface = self.config["wifi_interface"]
        result = self._run_command(
            [
                "nmcli",
                "-t",
                "-f",
                "SSID,SIGNAL,SECURITY,CHAN",
                "dev",
                "wifi",
                "list",
                "ifname",
                iface,
                "--rescan",
                "yes"
            ],
            timeout=25
        )

        if result.returncode != 0:
            raise Exception(
                result.stderr.strip()
                or result.stdout.strip()
                or "nmcli Wi-Fi scan failed"
            )

        networks = []
        for line in result.stdout.splitlines():
            parts = line.replace("\\:", "\u0000").split(":")
            parts = [part.replace("\u0000", ":") for part in parts]
            if len(parts) < 4:
                continue

            ssid, signal, security, channel = parts[:4]
            network = {
                "ssid": ssid,
                "security": security or "OPEN"
            }

            try:
                percent = int(signal or "0")
                network["signalPercent"] = percent
                network["signalDbm"] = self._signal_percent_to_dbm(percent)
            except ValueError:
                pass

            try:
                network["channel"] = int(channel)
            except ValueError:
                pass

            networks.append(network)

        return self._dedupe_networks(networks)

    def _current_wifi_ssid_nmcli(self):
        result = self._run_command(
            ["nmcli", "-t", "-f", "ACTIVE,SSID", "dev", "wifi"],
            timeout=10
        )

        if result.returncode != 0:
            return None

        for line in result.stdout.splitlines():
            active, _, ssid = line.partition(":")
            if active.lower() == "yes" and ssid:
                return ssid.replace("\\:", ":")

        return None

    def _connect_wifi_nmcli(self, ssid, password):
        iface = self.config["wifi_interface"]
        command = [
            "nmcli",
            "dev",
            "wifi",
            "connect",
            ssid,
            "ifname",
            iface
        ]

        if password:
            command.extend(["password", password])

        result = self._run_command(
            command,
            timeout=45
        )

        if result.returncode != 0:
            raise Exception(
                result.stderr.strip()
                or result.stdout.strip()
                or "nmcli Wi-Fi connect failed"
            )

        self.config["probe_ssid"] = ssid
        return self._current_wifi_ssid_nmcli() == ssid

    def _current_wifi_ssid_iwgetid(self):
        try:
            output = subprocess.check_output(
                ["iwgetid", "-r"],
                stderr=subprocess.STDOUT,
                timeout=10
            ).decode(errors="ignore").strip()

            return output or None
        except Exception:
            return None

    def _connect_wifi_wpa_supplicant(self, ssid, password):
        iface = self.config["wifi_interface"]
        ssid_json = json.dumps(ssid)

        if password:
            network_body = f"    ssid={ssid_json}\n    psk={json.dumps(password)}"
        else:
            network_body = f"    ssid={ssid_json}\n    key_mgmt=NONE"

        wpa_conf = f"network={{\n{network_body}\n}}\n"

        fd, wpa_path = tempfile.mkstemp(
            prefix="beacon_wpa_",
            suffix=".conf"
        )

        try:
            with os.fdopen(fd, "w") as f:
                f.write(wpa_conf)

            subprocess.run(
                ["sudo", "pkill", "wpa_supplicant"],
                check=False
            )

            result = self._run_command(
                ["sudo", "wpa_supplicant", "-B", "-i", iface, "-c", wpa_path],
                timeout=20
            )

            if result.returncode != 0:
                raise Exception(
                    result.stderr.strip()
                    or result.stdout.strip()
                    or "wpa_supplicant failed"
                )

            subprocess.run(
                ["sudo", "dhclient", iface],
                check=False
            )

            for _ in range(12):
                if self._current_wifi_ssid_iwgetid() == ssid:
                    self.config["probe_ssid"] = ssid
                    return True
                time.sleep(1)

            return False
        finally:
            try:
                os.remove(wpa_path)
            except OSError:
                pass

    def _scan_wifi_iwlist(self):
        iface = self.config["wifi_interface"]

        try:
            output = subprocess.check_output(
                ["sudo", "iwlist", iface, "scan"],
                stderr=subprocess.STDOUT,
                timeout=20
            ).decode(errors="ignore")
        except Exception as e:
            raise Exception(f"iwlist Wi-Fi scan failed: {e}")

        networks = []
        current = {}
        for raw_line in output.splitlines():
            line = raw_line.strip()
            if line.startswith("Cell "):
                if current.get("ssid"):
                    networks.append(current)
                current = {"security": "OPEN"}
            elif "ESSID:" in line:
                current["ssid"] = (
                    line.split("ESSID:", 1)[1]
                    .strip()
                    .strip('"')
                )
            elif "Signal level=" in line:
                signal = (
                    line.split("Signal level=", 1)[1]
                    .split(" ", 1)[0]
                )
                try:
                    current["signalDbm"] = float(signal)
                except ValueError:
                    pass
            elif "Channel:" in line:
                try:
                    current["channel"] = int(
                        line.split("Channel:", 1)[1]
                    )
                except ValueError:
                    pass
            elif "Encryption key:on" in line:
                current["security"] = "WPA/WPA2"

        if current.get("ssid"):
            networks.append(current)

        return self._dedupe_networks(networks)

    def _scan_wifi_mock(self):
        base_ssid = (
            self.config.get("probe_ssid")
            or self.config.get("wifi_credentials", {}).get("ssid")
            or "BEACON-WIFI"
        )
        names = [
            base_ssid,
            "PPORTO-Students",
            "PPORTO-Staff",
            "eduroam",
            "BEACON-Lab",
            "Guest-WiFi"
        ]

        networks = []
        for index, ssid in enumerate(names):
            networks.append({
                "ssid": ssid,
                "signalDbm": round(random.uniform(-88.0, -34.0), 1),
                "security": "OPEN" if ssid == "Guest-WiFi" else "WPA2",
                "channel": random.choice([1, 6, 11, 36, 44]),
                "connected": ssid == base_ssid,
                "rank": index + 1
            })

        networks.sort(key=lambda item: item["signalDbm"], reverse=True)
        return networks


class PiAgent:
    def __init__(self):

        self.config_manager = ConfigManager()

        self.config = (
            self.config_manager.config
        )

        self.graphql = GraphQLClient(
            self.config["graphql_url"],
            self.resolve_api_key()
        )

        self.network = NetworkManager(
            self.config
        )

        self.metric_queue = queue.Queue()
        self.enabled_tests = {}
        self.available_plugins = {}
        self.next_scheduled_run = {}
        self.state_lock = threading.Lock()

        self.plugins = PluginManager()
        self.plugins.load_plugins()

    def resolve_api_key(self):
        return (
            os.getenv("CENTRAL_SERVER_PROBE_API_KEY")
            or os.getenv("AUTH_PROBE_API_KEY")
            or os.getenv("PROBE_API_KEY")
            or self.config.get("api_key", "")
        )

    def sync_plugins(self):

        query = """
        query ProbeCfg($probeId: String!) {
            probeConfig(probeId: $probeId) {
                enabledTests {
                    testType
                    intervalSeconds
                    enabled
                }
                availablePlugins {
                id
                version
                    checksum
                    available
                    executionMode
                    bundleUrl
                    bundleDownloadUrl
                }
            }
        }
        """

        try:

            response = self.graphql.execute(
                query,
                {
                    "probeId": self.config["device_id"]
                }
            )

            data = response["data"]["probeConfig"]

            enabled_tests = {
                test["testType"]: {
                    "testType": test["testType"],
                    "intervalSeconds": int(
                        test.get("intervalSeconds") or 30
                    ),
                    "enabled": bool(test.get("enabled", True))
                }
                for test in data.get("enabledTests", [])
                if test.get("enabled", True)
            }

            plugins = data.get("availablePlugins", [])
            plugin_map = {
                plugin["id"]: plugin
                for plugin in plugins
                if plugin.get("available", True)
            }

            with self.state_lock:
                self.enabled_tests = enabled_tests
                self.available_plugins = plugin_map

            for plugin in plugins:
                if not plugin.get("available", True):
                    continue

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

                    download_url = (
                        plugin.get("bundleDownloadUrl")
                        or plugin.get("bundleUrl")
                    )

                    if not download_url:
                        print(f"{plugin_id} has no bundle URL")
                        continue

                    self.plugins.download_plugin(
                        absolute_url(
                            download_url,
                            self.config["graphql_url"]
                        ),
                        plugin.get("checksum"),
                        self.resolve_api_key()
                    )

        except Exception as e:

            print(
                "Plugin sync failed:",
                e
            )

    # Heartbeat

    def send_heartbeat(self):
        mutation = """
        mutation Heartbeat($input: ProbeHeartbeatInputTypeInput!) {
            recordProbeHeartbeat(input: $input) {
                success
                autoRegistered
                message
                runtime {
                    probeId
                    status
                    canEmitMetrics
                    enabledTests
                    site
                    ipAddress
                }
            }
        }
        """

        variables = {
            "input": {
                "probeId": self.config["device_id"],
                "name": self.config.get(
                    "probe_name",
                    self.config["device_id"]
                ),
                "location": self.config.get(
                    "probe_location",
                    "unknown"
                ),
                "ipAddress": resolve_local_ip(),
                "ssid": (
                    self.network.current_wifi_ssid()
                    or self.config.get("probe_ssid")
                    or None
                ),
                "agentVersion": self.config.get(
                    "agent_version",
                    "1.0.0-pi"
                )
            }
        }

        try:
            response = self.graphql.execute(
                mutation,
                variables
            )

            result = response["data"]["recordProbeHeartbeat"]
            if not result.get("success"):
                raise Exception(
                    result.get("message")
                    or "Heartbeat failed"
                )

            print("Heartbeat sent:", response)

        except Exception as e:
            print("Heartbeat failed:", e)

    # Send Metrics

    def build_metric_samples(self, payload):
        samples = []

        for plugin_id, metrics in payload.get("metrics", {}).items():
            labels = metric_labels(
                self.config,
                plugin_id
            )

            if isinstance(metrics, dict) and isinstance(
                metrics.get("metrics"),
                list
            ):
                for sample in metrics["metrics"]:
                    samples.append({
                        "name": sample["name"],
                        "kind": sample.get("kind", "gauge"),
                        "value": float(sample.get("value", 0)),
                        "timestampUtc": utc_now_iso(),
                        "labels": [
                            {
                                "key": str(key),
                                "value": str(value)
                            }
                            for key, value
                            in sample.get("labels", {}).items()
                        ] or labels
                    })

                continue

            if isinstance(metrics, dict):
                for key, value in metrics.items():
                    if isinstance(value, bool):
                        value = 1 if value else 0

                    if not isinstance(value, (int, float)):
                        continue

                    samples.append({
                        "name": (
                            "beacon_pi_"
                            + str(plugin_id).lower()
                            + "_"
                            + str(key).lower()
                        ),
                        "kind": "gauge",
                        "value": float(value),
                        "timestampUtc": utc_now_iso(),
                        "labels": labels
                    })

        return samples

    def send_metrics(self, payload):
        mutation = """
        mutation ReportMetrics($input: ReportProbeMetricsInputTypeInput!) {
            reportProbeMetrics(input: $input) {
                success
                message
                probeId
                acceptedSamples
                receivedAtUtc
            }
        }
        """

        samples = self.build_metric_samples(payload)
        if not samples:
            print("No numeric metric samples to send")
            return

        variables = {
            "input": {
                "probeId": self.config["device_id"],
                "samples": samples
            }
        }

        try:
            response = self.graphql.execute(
                mutation,
                variables
            )

            result = response["data"]["reportProbeMetrics"]
            if not result.get("success"):
                raise Exception(
                    result.get("message")
                    or "Metric send failed"
                )

            print("Metrics sent:", response)

        except Exception as e:
            print("Metric send failed:", e)
            self.metric_queue.put(payload)

    # Get configs from server

    def fetch_remote_config(self):
        query = """
        query ProbeCfg($probeId: String!) {
            probeConfig(probeId: $probeId) {
                enabledTests {
                    testType
                    intervalSeconds
                    enabled
                }
            }
        }
        """

        variables = {
            "probeId": self.config["device_id"]
        }

        try:
            response = self.graphql.execute(
                query,
                variables
            )

            data = response["data"]["probeConfig"]
            enabled_tests = {
                test["testType"]: {
                    "testType": test["testType"],
                    "intervalSeconds": int(
                        test.get("intervalSeconds") or 30
                    ),
                    "enabled": bool(test.get("enabled", True))
                }
                for test in data.get("enabledTests", [])
                if test.get("enabled", True)
            }

            with self.state_lock:
                self.enabled_tests = enabled_tests

            print("Remote config updated")

        except Exception as e:
            print("Failed fetching config:", e)

    # Network

    def ensure_connectivity(self):
        if self.config.get("skip_network_checks") or os.name == "nt":
            return True

        if self.can_reach_central():
            return True

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

    def can_reach_central(self):
        try:
            parsed = urllib.parse.urlparse(
                self.config["graphql_url"]
            )
            host = parsed.hostname
            port = parsed.port or (
                443 if parsed.scheme == "https" else 80
            )

            if not host:
                return False

            with socket.create_connection(
                (host, port),
                timeout=3
            ):
                return True
        except OSError:
            return False

    # Tests

    def run_due_tests(self):
        now = time.time()
        due_tests = []

        with self.state_lock:
            for plugin_id, cfg in self.enabled_tests.items():
                due_at = self.next_scheduled_run.get(plugin_id, 0)
                if now >= due_at:
                    due_tests.append((plugin_id, cfg))
                    self.next_scheduled_run[plugin_id] = (
                        now + cfg["intervalSeconds"]
                    )

        if not due_tests:
            return

        payload = {

            "device_id":
                self.config[
                    "device_id"
                ],

            "timestamp":
                int(time.time()),

            "metrics": {}
        }

        for plugin_id, test_cfg in due_tests:

            try:

                metrics = (
                    self.plugins.run_plugin(
                        plugin_id,
                        {
                            "probeId": self.config["device_id"],
                            "testType": plugin_id,
                            "scheduled": test_cfg,
                            "timestampUtc": utc_now_iso()
                        }
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

    def poll_and_execute_actions(self):
        query = """
        query PendingActions($probeId: String!, $limit: Int) {
            pendingProbeActions(probeId: $probeId, limit: $limit) {
                executionId
                probeId
                pluginId
                status
                requestedAtUtc
            }
        }
        """

        try:
            response = self.graphql.execute(
                query,
                {
                    "probeId": self.config["device_id"],
                    "limit": 10
                }
            )

            for action in response["data"]["pendingProbeActions"]:
                self.execute_action(action)

        except Exception as e:
            print("Action poll failed:", e)

    def update_action_status(
        self,
        execution_id,
        status,
        error_message=None
    ):
        mutation = """
        mutation UpdateAction($input: UpdateProbeActionStatusInputTypeInput!) {
            updateProbeActionStatus(input: $input) {
                success
                message
                execution {
                    executionId
                    status
                }
            }
        }
        """

        response = self.graphql.execute(
            mutation,
            {
                "input": {
                    "probeId": self.config["device_id"],
                    "executionId": execution_id,
                    "status": status,
                    "errorMessage": error_message
                }
            }
        )

        result = response["data"]["updateProbeActionStatus"]
        if not result.get("success"):
            raise Exception(
                result.get("message")
                or f"Failed updating action to {status}"
            )

    def execute_action(self, action):
        execution_id = action["executionId"]
        plugin_id = action["pluginId"]

        try:
            self.update_action_status(
                execution_id,
                "Running"
            )

            result = self.plugins.run_plugin(
                plugin_id,
                {
                    "probeId": self.config["device_id"],
                    "action": action,
                    "timestampUtc": utc_now_iso()
                }
            )

            self.send_metrics({
                "device_id": self.config["device_id"],
                "timestamp": int(time.time()),
                "metrics": {
                    plugin_id: result
                }
            })

            status = result.get("status", "SUCCEEDED")
            if str(status).upper() == "TIMED_OUT":
                self.update_action_status(
                    execution_id,
                    "TimedOut",
                    result.get("errorMessage")
                )
            elif str(status).upper() == "FAILED":
                self.update_action_status(
                    execution_id,
                    "Failed",
                    result.get("errorMessage")
                )
            else:
                self.update_action_status(
                    execution_id,
                    "Succeeded"
                )

        except subprocess.TimeoutExpired as e:
            self.update_action_status(
                execution_id,
                "TimedOut",
                str(e)
            )
        except Exception as e:
            try:
                self.update_action_status(
                    execution_id,
                    "Failed",
                    str(e)
                )
            except Exception as status_error:
                print("Action status update failed:", status_error)

            print(f"Action {execution_id} failed:", e)

    def fetch_pending_control_commands(self):
        query = """
        query PendingControl($probeId: String!, $limit: Int) {
            pendingProbeControlCommands(probeId: $probeId, limit: $limit) {
                commandId
                type
                status
                payloadJson
            }
        }
        """

        response = self.graphql.execute(
            query,
            {
                "probeId": self.config["device_id"],
                "limit": 5
            }
        )

        return response["data"]["pendingProbeControlCommands"]

    def update_control_command_status(
        self,
        command_id,
        status,
        result=None,
        error_message=None
    ):
        mutation = """
        mutation UpdateControl($input: UpdateProbeControlCommandStatusInputTypeInput!) {
            updateProbeControlCommandStatus(input: $input) {
                success
                message
                command {
                    commandId
                    status
                }
            }
        }
        """

        response = self.graphql.execute(
            mutation,
            {
                "input": {
                    "probeId": self.config["device_id"],
                    "commandId": command_id,
                    "status": status,
                    "resultJson": (
                        json.dumps(result)
                        if result is not None
                        else None
                    ),
                    "errorMessage": error_message
                }
            }
        )

        payload = response["data"]["updateProbeControlCommandStatus"]
        if not payload.get("success"):
            raise Exception(
                payload.get("message")
                or "Control command status update failed"
            )

    def is_network_demo_mode(self):
        return self.network.wifi_backend_name() == "mock"

    def execute_control_command(self, command):
        command_id = command["commandId"]
        command_type = str(command["type"]).replace("_", "").upper()
        payload = json.loads(command.get("payloadJson") or "{}")

        print(f"Executing control command {command_id}: {command['type']}")
        self.update_control_command_status(command_id, "Running")

        if command_type == "SCANWIFINETWORKS":
            result = {
                "networks": self.network.scan_wifi_networks(),
                "scannedAtUtc": utc_now_iso(),
                "mode": self.network.wifi_backend_name()
            }
            self.update_control_command_status(
                command_id,
                "Succeeded",
                result=result
            )
            print(f"Wi-Fi scan completed with {len(result['networks'])} network(s)")
            return

        if command_type == "CONNECTWIFI":
            ssid = (payload.get("ssid") or "").strip()
            if not ssid:
                raise Exception("SSID is required")

            self.config["wifi_credentials"] = {
                "ssid": ssid,
                "password": payload.get("password") or ""
            }
            self.config_manager.save(self.config)
            self.network.config = self.config

            connected = self.network.connect_wifi()
            mode = self.network.wifi_backend_name()
            if connected:
                self.config_manager.save(self.config)

            self.update_control_command_status(
                command_id,
                "Succeeded" if connected else "Failed",
                result={
                    "ssid": ssid,
                    "connected": connected,
                    "connectedAtUtc": utc_now_iso(),
                    "mode": mode
                },
                error_message=None if connected else "Wi-Fi connection failed"
            )
            self.send_heartbeat()
            print(f"Wi-Fi connect attempted for SSID {ssid} using {mode}")
            return

        if command_type == "UPDATEPROFILE":
            name = (payload.get("name") or "").strip()
            location = (payload.get("location") or "").strip()

            if name:
                self.config["probe_name"] = name
            if location:
                self.config["probe_location"] = location

            self.config_manager.save(self.config)
            self.update_control_command_status(
                command_id,
                "Succeeded",
                result={
                    "name": self.config.get("probe_name"),
                    "location": self.config.get("probe_location"),
                    "appliedAtUtc": utc_now_iso()
                }
            )
            self.send_heartbeat()
            print("Probe profile updated")
            return

        raise Exception(f"Unsupported control command type {command['type']}")

    def poll_and_execute_control_commands(self):
        try:
            commands = self.fetch_pending_control_commands()
        except Exception as e:
            print("Control command polling failed:", e)
            return

        for command in commands:
            try:
                self.execute_control_command(command)
            except Exception as e:
                try:
                    self.update_control_command_status(
                        command["commandId"],
                        "Failed",
                        error_message=str(e)
                    )
                except Exception as status_error:
                    print("Control status update failed:", status_error)

                print(
                    f"Control command {command.get('commandId')} failed:",
                    e
                )

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
                self.run_due_tests()

            time.sleep(
                self.config["metrics_interval"]
            )

    def action_loop(self):
        while True:
            if self.ensure_connectivity():
                self.poll_and_execute_actions()

            time.sleep(
                self.config.get("action_poll_interval", 10)
            )

    def control_loop(self):
        while True:
            if self.ensure_connectivity():
                self.poll_and_execute_control_commands()

            time.sleep(
                self.config.get("control_poll_interval", 10)
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
            target=self.action_loop,
            daemon=True
        ).start()

        threading.Thread(
            target=self.control_loop,
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
