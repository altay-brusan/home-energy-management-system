"""
Network Scanner for EC100 Provisioning Tool
Socket-based network scanning without external dependencies
"""
import socket
import ipaddress
import subprocess
import platform
import logging
import re
from typing import List, Dict, Callable, Optional
from concurrent.futures import ThreadPoolExecutor, as_completed

import config

logger = logging.getLogger(__name__)


class NetworkScanner:
    """Scans network for EC100 devices with SSH port open"""

    def __init__(self):
        self.scanning = False
        self._stop_requested = False

    def scan_network(
        self,
        subnet: str = None,
        port: int = None,
        timeout: float = None,
        max_workers: int = 50,
        progress_callback: Callable[[int, int, str], None] = None
    ) -> List[Dict]:
        """
        Scan network for devices with SSH port open

        Args:
            subnet: CIDR notation subnet (e.g., "192.168.1.0/24")
            port: Port to scan (default: 22)
            timeout: Socket timeout in seconds
            max_workers: Number of concurrent threads
            progress_callback: Callback function(current, total, ip)

        Returns:
            List of dicts with device info
        """
        subnet = subnet or config.DEFAULT_SUBNET
        port = port or config.EC100_SSH_PORT
        timeout = timeout or config.SCAN_TIMEOUT

        self.scanning = True
        self._stop_requested = False
        devices = []

        try:
            network = ipaddress.ip_network(subnet, strict=False)
            hosts = list(network.hosts())
            total = len(hosts)

            logger.info(f"Starting scan of {subnet} ({total} hosts)")

            with ThreadPoolExecutor(max_workers=max_workers) as executor:
                futures = {}
                for i, ip in enumerate(hosts):
                    if self._stop_requested:
                        break
                    ip_str = str(ip)
                    futures[executor.submit(self._check_host, ip_str, port, timeout)] = ip_str

                completed = 0
                for future in as_completed(futures):
                    if self._stop_requested:
                        break

                    ip_str = futures[future]
                    completed += 1

                    if progress_callback:
                        progress_callback(completed, total, ip_str)

                    try:
                        result = future.result()
                        if result:
                            devices.append(result)
                            logger.info(f"Found device: {ip_str}")
                    except Exception as e:
                        logger.debug(f"Error checking {ip_str}: {e}")

        except ValueError as e:
            logger.error(f"Invalid subnet: {e}")

        finally:
            self.scanning = False

        logger.info(f"Scan complete. Found {len(devices)} devices")
        return devices

    def _check_host(self, ip: str, port: int, timeout: float) -> Optional[Dict]:
        """
        Check if host has SSH port open

        Returns:
            Dict with host info if SSH is open, None otherwise
        """
        if self._stop_requested:
            return None

        if not self.check_ssh_port(ip, port, timeout):
            return None

        # Get MAC address
        mac = self.get_mac_address(ip)

        return {
            'ip': ip,
            'mac': mac,
            'port': port,
            'ssh_open': True
        }

    def check_ssh_port(self, ip: str, port: int = None, timeout: float = None) -> bool:
        """
        Check if SSH port is open on a host

        Args:
            ip: IP address to check
            port: Port number (default: 22)
            timeout: Socket timeout in seconds

        Returns:
            True if port is open
        """
        port = port or config.EC100_SSH_PORT
        timeout = timeout or config.SCAN_TIMEOUT

        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(timeout)
            result = sock.connect_ex((ip, port))
            sock.close()
            return result == 0
        except Exception:
            return False

    def get_mac_address(self, ip: str) -> Optional[str]:
        """
        Get MAC address for an IP using ARP

        Args:
            ip: IP address

        Returns:
            MAC address string or None
        """
        try:
            # First, ping to ensure ARP entry exists
            if platform.system().lower() == 'windows':
                # Windows ping
                subprocess.run(
                    ['ping', '-n', '1', '-w', '1000', ip],
                    capture_output=True,
                    timeout=3
                )
                # Read ARP table
                result = subprocess.run(
                    ['arp', '-a', ip],
                    capture_output=True,
                    text=True,
                    timeout=5
                )
                output = result.stdout

                # Parse Windows ARP output
                # Format: "192.168.1.1        aa-bb-cc-dd-ee-ff     dynamic"
                mac_pattern = r'([0-9a-fA-F]{2}[:-]){5}[0-9a-fA-F]{2}'
                match = re.search(mac_pattern, output)
                if match:
                    mac = match.group().replace('-', ':').upper()
                    return mac

            else:
                # Linux/Mac
                subprocess.run(
                    ['ping', '-c', '1', '-W', '1', ip],
                    capture_output=True,
                    timeout=3
                )
                result = subprocess.run(
                    ['arp', '-n', ip],
                    capture_output=True,
                    text=True,
                    timeout=5
                )
                output = result.stdout

                mac_pattern = r'([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}'
                match = re.search(mac_pattern, output)
                if match:
                    return match.group().upper()

        except Exception as e:
            logger.debug(f"Could not get MAC for {ip}: {e}")

        return None

    def stop(self):
        """Stop ongoing scan"""
        self._stop_requested = True
        logger.info("Scan stop requested")

    def scan_single_host(self, ip: str, port: int = None) -> Optional[Dict]:
        """
        Scan a single host

        Args:
            ip: IP address to scan
            port: Port to check

        Returns:
            Dict with host info or None
        """
        port = port or config.EC100_SSH_PORT
        timeout = config.SCAN_TIMEOUT

        return self._check_host(ip, port, timeout)

    def validate_subnet(self, subnet: str) -> bool:
        """
        Validate a subnet string

        Args:
            subnet: CIDR notation subnet

        Returns:
            True if valid
        """
        try:
            ipaddress.ip_network(subnet, strict=False)
            return True
        except ValueError:
            return False

    def get_host_count(self, subnet: str) -> int:
        """
        Get number of hosts in a subnet

        Args:
            subnet: CIDR notation subnet

        Returns:
            Number of hosts
        """
        try:
            network = ipaddress.ip_network(subnet, strict=False)
            return len(list(network.hosts()))
        except ValueError:
            return 0
