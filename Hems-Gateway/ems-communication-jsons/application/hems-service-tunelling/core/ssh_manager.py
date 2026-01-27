"""
SSH Manager for EC100 devices
Handles SSH connections and command execution using Paramiko
"""
import paramiko
import time
import logging
from typing import Tuple, Optional, Dict
from io import StringIO

import config

logger = logging.getLogger(__name__)


class SSHManager:
    """Manages SSH connections to EC100 devices"""

    def __init__(self):
        self.client: Optional[paramiko.SSHClient] = None
        self.sftp: Optional[paramiko.SFTPClient] = None
        self.host: str = ""
        self.connected: bool = False

    def connect(
        self,
        host: str,
        username: str = None,
        password: str = None,
        port: int = None,
        timeout: int = None,
        retries: int = None
    ) -> bool:
        """
        Connect to EC100 device via SSH

        Args:
            host: IP address of the device
            username: SSH username (default from config)
            password: SSH password (default from config)
            port: SSH port (default from config)
            timeout: Connection timeout in seconds
            retries: Number of retry attempts

        Returns:
            True if connection successful, False otherwise
        """
        username = username or config.EC100_DEFAULT_USER
        password = password or config.EC100_DEFAULT_PASSWORD
        port = port or config.EC100_SSH_PORT
        timeout = timeout or config.SSH_TIMEOUT
        retries = retries or config.RETRY_COUNT

        self.host = host

        for attempt in range(retries):
            try:
                self.client = paramiko.SSHClient()
                self.client.set_missing_host_key_policy(paramiko.AutoAddPolicy())

                logger.info(f"Connecting to {host}:{port} (attempt {attempt + 1}/{retries})")

                self.client.connect(
                    hostname=host,
                    port=port,
                    username=username,
                    password=password,
                    timeout=timeout,
                    allow_agent=False,
                    look_for_keys=False
                )

                self.connected = True
                logger.info(f"Successfully connected to {host}")
                return True

            except paramiko.AuthenticationException as e:
                logger.error(f"Authentication failed for {host}: {e}")
                self.close()
                return False  # Don't retry on auth failure

            except (paramiko.SSHException, OSError) as e:
                logger.warning(f"Connection attempt {attempt + 1} failed for {host}: {e}")
                self.close()
                if attempt < retries - 1:
                    time.sleep(2)  # Wait before retry
                continue

        logger.error(f"Failed to connect to {host} after {retries} attempts")
        return False

    def execute_command(self, command: str, timeout: int = 30) -> Tuple[str, str, int]:
        """
        Execute command on remote device

        Args:
            command: Command to execute
            timeout: Command timeout in seconds

        Returns:
            Tuple of (stdout, stderr, exit_code)
        """
        if not self.connected or not self.client:
            raise RuntimeError("Not connected to device")

        try:
            logger.debug(f"Executing: {command}")
            stdin, stdout, stderr = self.client.exec_command(command, timeout=timeout)

            exit_code = stdout.channel.recv_exit_status()
            stdout_str = stdout.read().decode('utf-8', errors='ignore').strip()
            stderr_str = stderr.read().decode('utf-8', errors='ignore').strip()

            logger.debug(f"Exit code: {exit_code}")
            if stdout_str:
                logger.debug(f"stdout: {stdout_str[:200]}")
            if stderr_str:
                logger.debug(f"stderr: {stderr_str[:200]}")

            return stdout_str, stderr_str, exit_code

        except Exception as e:
            logger.error(f"Command execution failed: {e}")
            raise

    def upload_file(self, local_path: str, remote_path: str) -> bool:
        """Upload a file to the remote device"""
        if not self.connected or not self.client:
            raise RuntimeError("Not connected to device")

        try:
            if not self.sftp:
                self.sftp = self.client.open_sftp()

            logger.info(f"Uploading {local_path} to {remote_path}")
            self.sftp.put(local_path, remote_path)
            return True

        except Exception as e:
            logger.error(f"File upload failed: {e}")
            return False

    def upload_content(self, content: str, remote_path: str) -> bool:
        """Upload string content directly to a remote file"""
        if not self.connected or not self.client:
            raise RuntimeError("Not connected to device")

        try:
            if not self.sftp:
                self.sftp = self.client.open_sftp()

            logger.info(f"Writing content to {remote_path}")
            with self.sftp.file(remote_path, 'w') as f:
                f.write(content)
            return True

        except Exception as e:
            logger.error(f"Content upload failed: {e}")
            return False

    def download_file(self, remote_path: str, local_path: str) -> bool:
        """Download a file from the remote device"""
        if not self.connected or not self.client:
            raise RuntimeError("Not connected to device")

        try:
            if not self.sftp:
                self.sftp = self.client.open_sftp()

            logger.info(f"Downloading {remote_path} to {local_path}")
            self.sftp.get(remote_path, local_path)
            return True

        except Exception as e:
            logger.error(f"File download failed: {e}")
            return False

    def read_file(self, remote_path: str) -> Optional[str]:
        """Read content of a remote file"""
        if not self.connected or not self.client:
            raise RuntimeError("Not connected to device")

        try:
            if not self.sftp:
                self.sftp = self.client.open_sftp()

            with self.sftp.file(remote_path, 'r') as f:
                return f.read().decode('utf-8', errors='ignore')

        except FileNotFoundError:
            return None
        except Exception as e:
            logger.error(f"File read failed: {e}")
            return None

    def file_exists(self, remote_path: str) -> bool:
        """Check if a file exists on remote device"""
        if not self.connected or not self.client:
            raise RuntimeError("Not connected to device")

        try:
            if not self.sftp:
                self.sftp = self.client.open_sftp()

            self.sftp.stat(remote_path)
            return True
        except FileNotFoundError:
            return False
        except Exception:
            return False

    def generate_ssh_key(self, key_path: str = None, force_regenerate: bool = False) -> Tuple[Optional[str], Optional[str]]:
        """
        Generate SSH key pair on the device

        Args:
            key_path: Path for the key file (default from config)
            force_regenerate: Force regeneration even if key exists

        Returns:
            Tuple of (private_key_path, public_key) or (None, None) on failure
        """
        key_path = key_path or config.EC100_KEY_PATH

        # Check if key already exists and is valid
        if self.file_exists(key_path) and not force_regenerate:
            public_key = self.read_file(f"{key_path}.pub")
            if public_key and public_key.strip():
                logger.info(f"Valid SSH key already exists at {key_path}")
                return key_path, public_key.strip()
            else:
                # Key exists but is empty or invalid
                logger.warning(f"Invalid/empty SSH key at {key_path}, will regenerate...")

        # Create .ssh directory if needed
        self.execute_command("mkdir -p /root/.ssh && chmod 700 /root/.ssh")

        # Remove old keys first (to avoid ssh-keygen overwrite prompt)
        self.execute_command(f"rm -f {key_path} {key_path}.pub")

        # Generate new ed25519 key
        cmd = f'ssh-keygen -t ed25519 -f {key_path} -N "" -q'
        stdout, stderr, exit_code = self.execute_command(cmd)

        if exit_code != 0:
            logger.error(f"Key generation failed: {stderr}")
            return None, None

        # Read public key
        public_key = self.read_file(f"{key_path}.pub")
        if public_key and public_key.strip():
            logger.info("SSH key generated successfully")
            return key_path, public_key.strip()

        logger.error("Failed to read generated public key")
        return None, None

    def get_device_info(self) -> Dict:
        """
        Get device information (hostname, MAC addresses)

        Returns:
            Dict with device info
        """
        info = {
            'hostname': '',
            'mac_addresses': [],
            'primary_mac': '',
            'ip_addresses': []
        }

        # Get hostname
        stdout, _, exit_code = self.execute_command("hostname")
        if exit_code == 0:
            info['hostname'] = stdout.strip()

        # Get MAC addresses from ip link
        stdout, _, exit_code = self.execute_command(
            "ip link show | grep -oP 'link/ether \\K[0-9a-f:]+'"
        )
        if exit_code == 0 and stdout:
            macs = [mac.strip().upper() for mac in stdout.split('\n') if mac.strip()]
            info['mac_addresses'] = macs
            if macs:
                info['primary_mac'] = macs[0]

        # Get IP addresses
        stdout, _, exit_code = self.execute_command(
            "ip -4 addr show | grep -oP 'inet \\K[0-9.]+'"
        )
        if exit_code == 0 and stdout:
            ips = [ip.strip() for ip in stdout.split('\n') if ip.strip() and ip.strip() != '127.0.0.1']
            info['ip_addresses'] = ips

        return info

    def install_package(self, package: str) -> bool:
        """
        Install a package using apt

        Args:
            package: Package name to install

        Returns:
            True if successful
        """
        # Check if already installed
        stdout, _, exit_code = self.execute_command(f"which {package}")
        if exit_code == 0:
            logger.info(f"{package} is already installed")
            return True

        # Update and install
        logger.info(f"Installing {package}...")
        _, stderr, exit_code = self.execute_command(
            f"apt-get update && apt-get install -y {package}",
            timeout=120
        )

        if exit_code != 0:
            logger.error(f"Failed to install {package}: {stderr}")
            return False

        logger.info(f"{package} installed successfully")
        return True

    def close(self):
        """Close SSH connection"""
        if self.sftp:
            try:
                self.sftp.close()
            except Exception:
                pass
            self.sftp = None

        if self.client:
            try:
                self.client.close()
            except Exception:
                pass
            self.client = None

        self.connected = False
        logger.debug(f"Connection to {self.host} closed")

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()
        return False
