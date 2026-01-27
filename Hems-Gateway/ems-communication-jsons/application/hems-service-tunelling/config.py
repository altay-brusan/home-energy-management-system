"""
EC100 Provisioning Tool - Configuration
"""
import os

# Application Info
APP_NAME = "EC100 Provisioning Tool"
APP_VERSION = "1.0.0"

# VPS Settings (Defaults)
VPS_IP = "REDACTED_HOST"
VPS_SSH_PORT = 2222
VPS_USER = "tunnel"
VPS_KEY_PATH = ""

# EC100 Default Settings
EC100_DEFAULT_USER = "root"
EC100_DEFAULT_PASSWORD = "***REMOVED***"
EC100_SSH_PORT = 22
EC100_KEY_PATH = "/root/.ssh/tunnel_key"

# Port Range for Tunnels
PORT_RANGE_START = 10001
PORT_RANGE_END = 11000

# Network Settings
DEFAULT_SUBNET = "192.168.1.0/24"
SCAN_TIMEOUT = 5
SSH_TIMEOUT = 10
RETRY_COUNT = 3

# Paths
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
DB_PATH = os.path.join(BASE_DIR, "database", "devices.db")
LOG_PATH = os.path.join(BASE_DIR, "logs", "provisioning.log")

# systemd Service Template (autossh yerine ssh - internet gerektirmez)
SERVICE_TEMPLATE = """[Unit]
Description=Reverse SSH Tunnel to VPS
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
ExecStart=/usr/bin/ssh -N -o "ServerAliveInterval=30" -o "ServerAliveCountMax=3" -o "ExitOnForwardFailure=yes" -o "StrictHostKeyChecking=accept-new" -o "BatchMode=yes" -i {key_path} -R {port}:localhost:22 -p {vps_port} {vps_user}@{vps_ip}
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
"""
