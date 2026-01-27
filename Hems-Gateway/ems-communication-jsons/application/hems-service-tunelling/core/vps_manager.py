"""
VPS Manager for EC100 Provisioning Tool
Handles SSH connections to VPS for tunnel management
"""
import paramiko
import logging
from typing import Optional, List

import config

logger = logging.getLogger(__name__)


class VPSManager:
    """Manages SSH connections to VPS for tunnel operations"""

    def __init__(self):
        self.client: Optional[paramiko.SSHClient] = None
        self.connected: bool = False
        self.vps_ip: str = ""
        self.vps_port: int = 0
        self.vps_user: str = ""

    def connect(
        self,
        ip: str = None,
        port: int = None,
        username: str = None,
        key_path: str = None,
        timeout: int = None
    ) -> bool:
        """
        Connect to VPS using SSH key

        Args:
            ip: VPS IP address
            port: SSH port
            username: SSH username
            key_path: Path to SSH private key
            timeout: Connection timeout

        Returns:
            True if connection successful
        """
        self.vps_ip = ip or config.VPS_IP
        self.vps_port = port or config.VPS_SSH_PORT
        self.vps_user = username or config.VPS_USER
        key_path = key_path or config.VPS_KEY_PATH
        timeout = timeout or config.SSH_TIMEOUT

        if not key_path:
            logger.error("VPS SSH key path not configured")
            return False

        try:
            self.client = paramiko.SSHClient()
            self.client.set_missing_host_key_policy(paramiko.AutoAddPolicy())

            logger.info(f"Connecting to VPS {self.vps_ip}:{self.vps_port}")

            # Load private key
            try:
                private_key = paramiko.Ed25519Key.from_private_key_file(key_path)
            except Exception:
                try:
                    private_key = paramiko.RSAKey.from_private_key_file(key_path)
                except Exception:
                    private_key = paramiko.ECDSAKey.from_private_key_file(key_path)

            self.client.connect(
                hostname=self.vps_ip,
                port=self.vps_port,
                username=self.vps_user,
                pkey=private_key,
                timeout=timeout,
                allow_agent=False,
                look_for_keys=False
            )

            self.connected = True
            logger.info(f"Successfully connected to VPS {self.vps_ip}")
            return True

        except paramiko.AuthenticationException as e:
            logger.error(f"VPS authentication failed: {e}")
            self.close()
            return False

        except Exception as e:
            logger.error(f"VPS connection failed: {e}")
            self.close()
            return False

    def execute_command(self, command: str, timeout: int = 30) -> tuple:
        """Execute command on VPS"""
        if not self.connected or not self.client:
            raise RuntimeError("Not connected to VPS")

        try:
            stdin, stdout, stderr = self.client.exec_command(command, timeout=timeout)
            exit_code = stdout.channel.recv_exit_status()
            stdout_str = stdout.read().decode('utf-8', errors='ignore').strip()
            stderr_str = stderr.read().decode('utf-8', errors='ignore').strip()
            return stdout_str, stderr_str, exit_code
        except Exception as e:
            logger.error(f"VPS command failed: {e}")
            raise

    def add_authorized_key(self, public_key: str) -> bool:
        """
        Add EC100's public key to VPS authorized_keys

        Args:
            public_key: The public key to add

        Returns:
            True if successful
        """
        if not self.connected:
            raise RuntimeError("Not connected to VPS")

        try:
            # Ensure .ssh directory exists
            self.execute_command("mkdir -p ~/.ssh && chmod 700 ~/.ssh")

            # Check if key already exists
            stdout, _, _ = self.execute_command("cat ~/.ssh/authorized_keys 2>/dev/null || echo ''")

            if public_key.strip() in stdout:
                logger.info("Public key already exists in authorized_keys")
                return True

            # Append the new key
            escaped_key = public_key.strip().replace("'", "'\\''")
            cmd = f"echo '{escaped_key}' >> ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys"
            _, stderr, exit_code = self.execute_command(cmd)

            if exit_code != 0:
                logger.error(f"Failed to add authorized key: {stderr}")
                return False

            logger.info("Public key added to VPS authorized_keys")
            return True

        except Exception as e:
            logger.error(f"Failed to add authorized key: {e}")
            return False

    def remove_authorized_key(self, public_key: str) -> bool:
        """
        Remove a public key from VPS authorized_keys

        Args:
            public_key: The public key to remove

        Returns:
            True if successful
        """
        if not self.connected:
            raise RuntimeError("Not connected to VPS")

        try:
            # Extract the key part (without comment) for matching
            key_parts = public_key.strip().split()
            if len(key_parts) >= 2:
                key_to_match = key_parts[1]  # The actual key data
            else:
                key_to_match = public_key.strip()

            # Remove lines containing this key
            escaped_key = key_to_match.replace("/", "\\/")
            cmd = f"sed -i '/{escaped_key}/d' ~/.ssh/authorized_keys"
            _, stderr, exit_code = self.execute_command(cmd)

            if exit_code != 0:
                logger.error(f"Failed to remove authorized key: {stderr}")
                return False

            logger.info("Public key removed from VPS authorized_keys")
            return True

        except Exception as e:
            logger.error(f"Failed to remove authorized key: {e}")
            return False

    def check_tunnel_active(self, port: int) -> bool:
        """
        Check if a tunnel is active on the specified port

        Args:
            port: The tunnel port to check

        Returns:
            True if tunnel is active
        """
        if not self.connected:
            raise RuntimeError("Not connected to VPS")

        try:
            # Check if port is listening
            # Use \s (whitespace) after port to avoid matching 10001 in 100010
            cmd = f"ss -tln | grep -qE ':{port}\\s' && echo 'active' || echo 'inactive'"
            stdout, stderr, exit_code = self.execute_command(cmd)

            is_active = stdout.strip() == 'active'
            logger.info(f"Tunnel on port {port}: {'active' if is_active else 'inactive'}")
            return is_active

        except Exception as e:
            logger.error(f"Failed to check tunnel status for port {port}: {e}")
            return False

    def get_active_tunnels(self) -> List[int]:
        """
        Get list of active tunnel ports

        Returns:
            List of port numbers with active tunnels
        """
        if not self.connected:
            raise RuntimeError("Not connected to VPS")

        try:
            # Get listening ports in our range
            start_port = config.PORT_RANGE_START
            end_port = config.PORT_RANGE_END

            cmd = "ss -tln | grep LISTEN | awk '{print $4}' | grep -oP ':\\K[0-9]+$'"
            stdout, _, _ = self.execute_command(cmd)

            active_ports = []
            for line in stdout.split('\n'):
                if line.strip():
                    try:
                        port = int(line.strip())
                        if start_port <= port <= end_port:
                            active_ports.append(port)
                    except ValueError:
                        continue

            logger.info(f"Found {len(active_ports)} active tunnels")
            return sorted(active_ports)

        except Exception as e:
            logger.error(f"Failed to get active tunnels: {e}")
            return []

    def test_tunnel_connection(self, port: int, timeout: int = 5) -> bool:
        """
        Test if we can connect through the tunnel

        Args:
            port: Tunnel port to test
            timeout: Connection timeout

        Returns:
            True if connection successful
        """
        if not self.connected:
            raise RuntimeError("Not connected to VPS")

        try:
            # Try to connect to the tunnel port and check SSH banner
            cmd = f"timeout {timeout} bash -c 'echo | nc -w {timeout} localhost {port}' 2>/dev/null | head -1"
            stdout, _, exit_code = self.execute_command(cmd, timeout=timeout + 5)

            # SSH servers typically respond with "SSH-" banner
            if "SSH-" in stdout:
                logger.info(f"Tunnel on port {port} is working (SSH banner received)")
                return True

            logger.warning(f"Tunnel on port {port} - no SSH banner received")
            return False

        except Exception as e:
            logger.error(f"Tunnel test failed: {e}")
            return False

    def close(self):
        """Close VPS connection"""
        if self.client:
            try:
                self.client.close()
            except Exception:
                pass
            self.client = None

        self.connected = False
        logger.debug("VPS connection closed")

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()
        return False
