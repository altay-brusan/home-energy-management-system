"""
Database Manager for EC100 Provisioning Tool
Handles all SQLite database operations
"""
import sqlite3
import os
from datetime import datetime
from typing import List, Dict, Optional
from contextlib import contextmanager

import config
from .models import Device, ProvisionLog


class DatabaseManager:
    """Manages SQLite database operations"""

    def __init__(self, db_path: str = None):
        self.db_path = db_path or config.DB_PATH
        # Ensure directory exists
        os.makedirs(os.path.dirname(self.db_path), exist_ok=True)

    @contextmanager
    def get_connection(self):
        """Context manager for database connections"""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        try:
            yield conn
            conn.commit()
        except Exception:
            conn.rollback()
            raise
        finally:
            conn.close()

    def init_database(self):
        """Initialize database tables"""
        with self.get_connection() as conn:
            cursor = conn.cursor()

            # Create devices table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS devices (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    device_name VARCHAR(100) NOT NULL,
                    serial_number VARCHAR(50) UNIQUE,
                    mac_address VARCHAR(17),
                    tunnel_port INTEGER UNIQUE NOT NULL,
                    location VARCHAR(200),
                    notes TEXT,
                    public_key TEXT,
                    status VARCHAR(20) DEFAULT 'offline',
                    last_seen DATETIME,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                )
            """)

            # Create provision_logs table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS provision_logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    device_id INTEGER,
                    action VARCHAR(50),
                    status VARCHAR(20),
                    message TEXT,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (device_id) REFERENCES devices(id)
                )
            """)

            # Create settings table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS settings (
                    key VARCHAR(50) PRIMARY KEY,
                    value TEXT
                )
            """)

            # Insert default settings if not exists
            default_settings = [
                ('vps_ip', config.VPS_IP),
                ('vps_ssh_port', str(config.VPS_SSH_PORT)),
                ('vps_user', config.VPS_USER),
                ('vps_key_path', config.VPS_KEY_PATH),
                ('ec100_default_user', config.EC100_DEFAULT_USER),
                ('ec100_default_password', config.EC100_DEFAULT_PASSWORD),
                ('ec100_ssh_port', str(config.EC100_SSH_PORT)),
                ('port_range_start', str(config.PORT_RANGE_START)),
                ('port_range_end', str(config.PORT_RANGE_END)),
                ('scan_subnet', config.DEFAULT_SUBNET),
            ]

            for key, value in default_settings:
                cursor.execute(
                    "INSERT OR IGNORE INTO settings (key, value) VALUES (?, ?)",
                    (key, value)
                )

    # ==================== Device Operations ====================

    def add_device(self, device: Device) -> int:
        """Add a new device, returns device ID"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("""
                INSERT INTO devices
                (device_name, serial_number, mac_address, tunnel_port, location,
                 notes, public_key, status, last_seen)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                device.device_name,
                device.serial_number,
                device.mac_address,
                device.tunnel_port,
                device.location,
                device.notes,
                device.public_key,
                device.status,
                device.last_seen
            ))
            return cursor.lastrowid

    def update_device(self, device_id: int, data: Dict) -> bool:
        """Update device information"""
        if not data:
            return False

        # Build update query dynamically
        fields = []
        values = []
        for key, value in data.items():
            if key != 'id':
                fields.append(f"{key} = ?")
                values.append(value)

        fields.append("updated_at = ?")
        values.append(datetime.now().isoformat())
        values.append(device_id)

        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                f"UPDATE devices SET {', '.join(fields)} WHERE id = ?",
                values
            )
            return cursor.rowcount > 0

    def delete_device(self, device_id: int) -> bool:
        """Delete a device"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            # Delete related logs first
            cursor.execute("DELETE FROM provision_logs WHERE device_id = ?", (device_id,))
            cursor.execute("DELETE FROM devices WHERE id = ?", (device_id,))
            return cursor.rowcount > 0

    def get_device(self, device_id: int) -> Optional[Device]:
        """Get device by ID"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM devices WHERE id = ?", (device_id,))
            row = cursor.fetchone()
            if row:
                return self._row_to_device(row)
            return None

    def get_device_by_port(self, port: int) -> Optional[Device]:
        """Get device by tunnel port"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM devices WHERE tunnel_port = ?", (port,))
            row = cursor.fetchone()
            if row:
                return self._row_to_device(row)
            return None

    def get_device_by_mac(self, mac_address: str) -> Optional[Device]:
        """Get device by MAC address"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM devices WHERE mac_address = ?", (mac_address,))
            row = cursor.fetchone()
            if row:
                return self._row_to_device(row)
            return None

    def get_all_devices(self) -> List[Device]:
        """Get all devices"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM devices ORDER BY tunnel_port")
            return [self._row_to_device(row) for row in cursor.fetchall()]

    def search_devices(self, query: str) -> List[Device]:
        """Search devices by name, location, or serial number"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            search_pattern = f"%{query}%"
            cursor.execute("""
                SELECT * FROM devices
                WHERE device_name LIKE ?
                   OR location LIKE ?
                   OR serial_number LIKE ?
                   OR mac_address LIKE ?
                ORDER BY tunnel_port
            """, (search_pattern, search_pattern, search_pattern, search_pattern))
            return [self._row_to_device(row) for row in cursor.fetchall()]

    def get_devices_by_status(self, status: str) -> List[Device]:
        """Get devices by status"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM devices WHERE status = ? ORDER BY tunnel_port", (status,))
            return [self._row_to_device(row) for row in cursor.fetchall()]

    def get_next_port(self) -> int:
        """Get next available port number"""
        with self.get_connection() as conn:
            cursor = conn.cursor()

            # Get port range from settings
            cursor.execute("SELECT value FROM settings WHERE key = 'port_range_start'")
            start_row = cursor.fetchone()
            start_port = int(start_row['value']) if start_row else config.PORT_RANGE_START

            cursor.execute("SELECT value FROM settings WHERE key = 'port_range_end'")
            end_row = cursor.fetchone()
            end_port = int(end_row['value']) if end_row else config.PORT_RANGE_END

            # Get all used ports
            cursor.execute("SELECT tunnel_port FROM devices ORDER BY tunnel_port")
            used_ports = {row['tunnel_port'] for row in cursor.fetchall()}

            # Find first available port
            for port in range(start_port, end_port + 1):
                if port not in used_ports:
                    return port

            raise ValueError("No available ports in range")

    def get_device_count(self) -> int:
        """Get total device count"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT COUNT(*) as count FROM devices")
            return cursor.fetchone()['count']

    def _row_to_device(self, row: sqlite3.Row) -> Device:
        """Convert database row to Device object"""
        return Device(
            id=row['id'],
            device_name=row['device_name'],
            serial_number=row['serial_number'],
            mac_address=row['mac_address'],
            tunnel_port=row['tunnel_port'],
            location=row['location'],
            notes=row['notes'],
            public_key=row['public_key'],
            status=row['status'],
            last_seen=datetime.fromisoformat(row['last_seen']) if row['last_seen'] else None,
            created_at=datetime.fromisoformat(row['created_at']) if row['created_at'] else None,
            updated_at=datetime.fromisoformat(row['updated_at']) if row['updated_at'] else None,
        )

    # ==================== Settings Operations ====================

    def get_setting(self, key: str) -> Optional[str]:
        """Get setting value by key"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT value FROM settings WHERE key = ?", (key,))
            row = cursor.fetchone()
            return row['value'] if row else None

    def set_setting(self, key: str, value: str) -> bool:
        """Set setting value"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "INSERT OR REPLACE INTO settings (key, value) VALUES (?, ?)",
                (key, value)
            )
            return cursor.rowcount > 0

    def get_all_settings(self) -> Dict[str, str]:
        """Get all settings as dictionary"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT key, value FROM settings")
            return {row['key']: row['value'] for row in cursor.fetchall()}

    # ==================== Log Operations ====================

    def add_log(self, device_id: Optional[int], action: str, status: str, message: str = None):
        """Add provision log entry"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("""
                INSERT INTO provision_logs (device_id, action, status, message)
                VALUES (?, ?, ?, ?)
            """, (device_id, action, status, message))

    def get_logs(self, device_id: int = None, limit: int = 100) -> List[ProvisionLog]:
        """Get provision logs, optionally filtered by device"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            if device_id:
                cursor.execute("""
                    SELECT * FROM provision_logs
                    WHERE device_id = ?
                    ORDER BY created_at DESC
                    LIMIT ?
                """, (device_id, limit))
            else:
                cursor.execute("""
                    SELECT * FROM provision_logs
                    ORDER BY created_at DESC
                    LIMIT ?
                """, (limit,))

            return [self._row_to_log(row) for row in cursor.fetchall()]

    def get_logs_with_device_info(self, limit: int = 100) -> List[Dict]:
        """Get logs with device names for display"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT l.*, d.device_name
                FROM provision_logs l
                LEFT JOIN devices d ON l.device_id = d.id
                ORDER BY l.created_at DESC
                LIMIT ?
            """, (limit,))

            logs = []
            for row in cursor.fetchall():
                log_dict = dict(row)
                logs.append(log_dict)
            return logs

    def clear_logs(self):
        """Clear all logs"""
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("DELETE FROM provision_logs")

    def _row_to_log(self, row: sqlite3.Row) -> ProvisionLog:
        """Convert database row to ProvisionLog object"""
        return ProvisionLog(
            id=row['id'],
            device_id=row['device_id'],
            action=row['action'],
            status=row['status'],
            message=row['message'],
            created_at=datetime.fromisoformat(row['created_at']) if row['created_at'] else None,
        )
