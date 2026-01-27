# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

EC100 Provisioning Tool is a PyQt6 desktop application for automating reverse SSH tunnel setup on EC100 industrial computers. It manages 1000+ devices with centralized inventory and one-click provisioning.

## Commands

```bash
# Install dependencies
pip install -r requirements.txt

# Run the application
python main.py

# Build standalone executable
pyinstaller --onefile --windowed --name="EC100-Provisioning-Tool" main.py
```

## Architecture

### Layer Structure

```
main.py              # Entry point, initializes DB and MainWindow
config.py            # Constants: VPS settings, EC100 defaults, port ranges, service template
├── core/            # Business logic (non-UI)
├── database/        # SQLite persistence
└── ui/              # PyQt6 widgets
```

### Core Module Responsibilities

- **ssh_manager.py** (`SSHManager`): Paramiko-based SSH to EC100 devices. Handles command execution, file upload/download, SSH key generation, device info retrieval.
- **vps_manager.py** (`VPSManager`): Paramiko-based SSH to VPS. Manages authorized_keys, checks tunnel status via `ss -tln`.
- **scanner.py** (`NetworkScanner`): Socket-based network scan with ThreadPoolExecutor. Finds devices with open SSH port, retrieves MAC via ARP.
- **provisioner.py** (`Provisioner`): Orchestrates the 9-step provisioning flow. Emits PyQt signals for progress updates. Uses QObject for signal/slot integration.

### Database (SQLite)

- **db_manager.py** (`DatabaseManager`): Context-managed connections, CRUD for devices/settings/logs.
- **models.py**: Dataclasses `Device` and `ProvisionLog` with `to_dict`/`from_dict` methods.

Tables: `devices` (tunnel_port unique), `provision_logs`, `settings` (key-value store).

### UI Widgets

- **main_window.py**: Tab container (Scan, Inventory, Settings, Logs), status bar with VPS connection indicator.
- **scan_widget.py**: Network scanning + provisioning trigger.
- **inventory_widget.py**: Device list with search/filter, CSV export.
- **settings_widget.py**: VPS/EC100 configuration forms.
- **logs_widget.py**: Provision log viewer.
- **styles.qss**: Qt stylesheet for theming.

### Provisioning Flow (9 steps)

1. Connect to EC100 via SSH (password auth)
2. Get device info (hostname, MAC)
3. Generate ed25519 SSH key on EC100
4. Connect to VPS via SSH (key auth)
5. Add EC100's public key to VPS authorized_keys
6. Create systemd service file on EC100
7. Enable and start reverse-tunnel.service
8. Verify tunnel via VPS (check port listening)
9. Save device to database

### Key Patterns

- **Threading**: SSH operations run in QThread to avoid blocking UI. Provisioner emits `progress_updated`, `device_completed`, `batch_completed` signals.
- **Settings**: Stored in SQLite `settings` table, loaded at runtime with fallback to `config.py` defaults.
- **Tunnel Ports**: Auto-assigned from range 10001-11000, tracked via `devices.tunnel_port` (unique constraint).

## Configuration Constants (config.py)

| Setting | Default Value |
|---------|---------------|
| VPS SSH Port | 2222 |
| EC100 SSH Port | 22 |
| Tunnel Port Range | 10001-11000 |
| SSH Timeout | 10s |
| Scan Timeout | 5s |
| Retry Count | 3 |
