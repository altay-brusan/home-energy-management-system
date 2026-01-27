"""
Provisioner for EC100 Provisioning Tool
Main provisioning logic for setting up reverse SSH tunnels
"""
import logging
import time
from typing import Dict, List, Callable, Optional
from dataclasses import dataclass
from enum import Enum

from PyQt6.QtCore import QObject, pyqtSignal

import config
from .ssh_manager import SSHManager
from .vps_manager import VPSManager
from database.db_manager import DatabaseManager
from database.models import Device

logger = logging.getLogger(__name__)


class ProvisionStep(Enum):
    """Provisioning steps"""
    CONNECT_EC100 = 1
    GET_DEVICE_INFO = 2
    GENERATE_KEY = 3
    CONNECT_VPS = 4
    ADD_KEY_TO_VPS = 5
    CREATE_SERVICE = 6
    START_SERVICE = 7
    VERIFY_TUNNEL = 8
    SAVE_TO_DB = 9


STEP_MESSAGES = {
    ProvisionStep.CONNECT_EC100: "EC100'e bağlanılıyor...",
    ProvisionStep.GET_DEVICE_INFO: "Cihaz bilgileri alınıyor...",
    ProvisionStep.GENERATE_KEY: "SSH key oluşturuluyor...",
    ProvisionStep.CONNECT_VPS: "VPS'e bağlanılıyor...",
    ProvisionStep.ADD_KEY_TO_VPS: "Key VPS'e ekleniyor...",
    ProvisionStep.CREATE_SERVICE: "Servis dosyası oluşturuluyor...",
    ProvisionStep.START_SERVICE: "Servis başlatılıyor...",
    ProvisionStep.VERIFY_TUNNEL: "Tunnel doğrulanıyor...",
    ProvisionStep.SAVE_TO_DB: "Veritabanına kaydediliyor...",
}


@dataclass
class ProvisionResult:
    """Result of a provision operation"""
    success: bool
    message: str
    device_id: Optional[int] = None
    error_step: Optional[ProvisionStep] = None


class Provisioner(QObject):
    """
    Main provisioner class for EC100 devices

    Signals:
        progress_updated: (ip, step, total_steps, message)
        device_completed: (ip, success, message)
        batch_completed: (total, success_count, failed_count)
    """

    # Signals for UI updates
    progress_updated = pyqtSignal(str, int, int, str)
    device_completed = pyqtSignal(str, bool, str)
    batch_completed = pyqtSignal(int, int, int)

    def __init__(self, db: DatabaseManager = None):
        super().__init__()
        self.db = db or DatabaseManager()
        self._stop_requested = False
        self.total_steps = len(ProvisionStep)

    def provision_device(
        self,
        ip: str,
        device_name: str,
        location: str = "",
        serial_number: str = "",
        port: int = None,
        notes: str = ""
    ) -> ProvisionResult:
        """
        Provision a single EC100 device

        Args:
            ip: Device IP address
            device_name: Name for the device
            location: Device location
            serial_number: Device serial number
            port: Tunnel port (auto-assigned if None)
            notes: Additional notes

        Returns:
            ProvisionResult with status and details
        """
        self._stop_requested = False

        # Get settings from database
        settings = self.db.get_all_settings()

        # Auto-assign port if not specified
        if port is None:
            port = self.db.get_next_port()

        ssh = SSHManager()
        vps = VPSManager()
        current_step = None

        try:
            # Step 1: Connect to EC100
            current_step = ProvisionStep.CONNECT_EC100
            self._emit_progress(ip, current_step)

            if not ssh.connect(
                host=ip,
                username=settings.get('ec100_default_user', config.EC100_DEFAULT_USER),
                password=settings.get('ec100_default_password', config.EC100_DEFAULT_PASSWORD),
                port=int(settings.get('ec100_ssh_port', config.EC100_SSH_PORT))
            ):
                return self._fail(ip, current_step, "EC100'e SSH bağlantısı kurulamadı")

            # Step 2: Get device info
            current_step = ProvisionStep.GET_DEVICE_INFO
            self._emit_progress(ip, current_step)

            device_info = ssh.get_device_info()
            mac_address = device_info.get('primary_mac', '')

            # Check if device already provisioned (by MAC)
            existing_device = None
            if mac_address:
                existing_device = self.db.get_device_by_mac(mac_address)
                if existing_device:
                    logger.info(f"Cihaz zaten kayıtlı, yeniden kurulum yapılacak (Port: {existing_device.tunnel_port}, MAC: {mac_address})")
                    # Use existing port for re-provisioning
                    port = existing_device.tunnel_port

            # Step 3: Generate SSH key
            current_step = ProvisionStep.GENERATE_KEY
            self._emit_progress(ip, current_step)

            # Force regenerate if re-provisioning existing device
            force_regen = existing_device is not None
            key_path, public_key = ssh.generate_ssh_key(config.EC100_KEY_PATH, force_regenerate=force_regen)
            if not public_key:
                return self._fail(ip, current_step, "SSH key oluşturulamadı")

            # Step 4: Connect to VPS
            current_step = ProvisionStep.CONNECT_VPS
            self._emit_progress(ip, current_step)

            vps_key_path = settings.get('vps_key_path', config.VPS_KEY_PATH)
            if not vps_key_path:
                return self._fail(ip, current_step, "VPS SSH key yolu yapılandırılmamış")

            if not vps.connect(
                ip=settings.get('vps_ip', config.VPS_IP),
                port=int(settings.get('vps_ssh_port', config.VPS_SSH_PORT)),
                username=settings.get('vps_user', config.VPS_USER),
                key_path=vps_key_path
            ):
                return self._fail(ip, current_step, "VPS'e bağlanılamadı")

            # Step 5: Add key to VPS
            current_step = ProvisionStep.ADD_KEY_TO_VPS
            self._emit_progress(ip, current_step)

            # If re-provisioning, remove old key from VPS first
            if existing_device and existing_device.public_key:
                logger.info("Removing old key from VPS...")
                vps.remove_authorized_key(existing_device.public_key)

            # Add device info as comment to the key
            # Format: ssh-ed25519 AAAA...key... EC100:seri_no NetZr:seri_no Port:10001
            key_parts = public_key.strip().split()
            if len(key_parts) >= 2:
                key_type = key_parts[0]
                key_data = key_parts[1]
                comment = f"EC100:{serial_number or 'N/A'} NetZr:{location or 'N/A'} Port:{port}"
                public_key_with_comment = f"{key_type} {key_data} {comment}"
            else:
                public_key_with_comment = public_key

            if not vps.add_authorized_key(public_key_with_comment):
                return self._fail(ip, current_step, "Key VPS'e eklenemedi")

            # Step 6: Create systemd service
            current_step = ProvisionStep.CREATE_SERVICE
            self._emit_progress(ip, current_step)

            service_content = config.SERVICE_TEMPLATE.format(
                key_path=config.EC100_KEY_PATH,
                port=port,
                vps_port=settings.get('vps_ssh_port', config.VPS_SSH_PORT),
                vps_user=settings.get('vps_user', config.VPS_USER),
                vps_ip=settings.get('vps_ip', config.VPS_IP)
            )

            service_path = "/etc/systemd/system/reverse-tunnel.service"
            if not ssh.upload_content(service_content, service_path):
                return self._fail(ip, current_step, "Servis dosyası oluşturulamadı")

            # Step 7: Start and enable service
            current_step = ProvisionStep.START_SERVICE
            self._emit_progress(ip, current_step)

            # Reload systemd and enable service
            commands = [
                "systemctl daemon-reload",
                "systemctl enable reverse-tunnel.service",
                "systemctl restart reverse-tunnel.service"
            ]

            for cmd in commands:
                stdout, stderr, exit_code = ssh.execute_command(cmd)
                if exit_code != 0:
                    logger.warning(f"Command '{cmd}' returned {exit_code}: {stderr}")

            # Wait for tunnel to establish
            time.sleep(3)

            # Step 8: Verify tunnel
            current_step = ProvisionStep.VERIFY_TUNNEL
            self._emit_progress(ip, current_step)

            tunnel_active = False
            for attempt in range(3):
                if vps.check_tunnel_active(port):
                    tunnel_active = True
                    break
                time.sleep(2)

            if not tunnel_active:
                # Check service status for debugging
                stdout, _, _ = ssh.execute_command("systemctl status reverse-tunnel.service")
                logger.warning(f"Service status: {stdout}")
                return self._fail(ip, current_step, "Tunnel doğrulanamadı - servis aktif olmayabilir")

            # Step 9: Save to database
            current_step = ProvisionStep.SAVE_TO_DB
            self._emit_progress(ip, current_step)

            if existing_device:
                # Update existing device
                device_id = existing_device.id
                self.db.update_device(device_id, {
                    'device_name': device_name,
                    'serial_number': serial_number or existing_device.serial_number,
                    'location': location or existing_device.location,
                    'notes': notes or existing_device.notes,
                    'public_key': public_key,
                    'status': 'online'
                })
                self.db.add_log(device_id, 'reprovision', 'success', f"Cihaz yeniden kuruldu: {ip}")
            else:
                # Add new device
                device = Device(
                    device_name=device_name,
                    serial_number=serial_number or None,
                    mac_address=mac_address or None,
                    tunnel_port=port,
                    location=location or None,
                    notes=notes or None,
                    public_key=public_key,
                    status='online'
                )
                device_id = self.db.add_device(device)
                self.db.add_log(device_id, 'provision', 'success', f"Cihaz başarıyla kuruldu: {ip}")

            self.device_completed.emit(ip, True, f"Kurulum tamamlandı (Port: {port})")

            return ProvisionResult(
                success=True,
                message=f"Kurulum başarılı. Tunnel Port: {port}",
                device_id=device_id
            )

        except Exception as e:
            logger.exception(f"Provision failed for {ip}")
            return self._fail(ip, current_step, str(e))

        finally:
            ssh.close()
            vps.close()

    def provision_batch(
        self,
        devices: List[Dict],
        progress_callback: Callable[[str, int, int, str], None] = None
    ) -> Dict:
        """
        Provision multiple devices

        Args:
            devices: List of device dicts with keys: ip, device_name, location, serial_number
            progress_callback: Optional callback function

        Returns:
            Dict with results summary
        """
        self._stop_requested = False

        total = len(devices)
        success_count = 0
        failed_count = 0
        results = []

        for i, device_info in enumerate(devices):
            if self._stop_requested:
                logger.info("Batch provision stopped by user")
                break

            ip = device_info.get('ip')
            logger.info(f"Provisioning device {i + 1}/{total}: {ip}")

            result = self.provision_device(
                ip=ip,
                device_name=device_info.get('device_name', f'EC100-{ip}'),
                location=device_info.get('location', ''),
                serial_number=device_info.get('serial_number', ''),
                port=device_info.get('port'),
                notes=device_info.get('notes', '')
            )

            if result.success:
                success_count += 1
            else:
                failed_count += 1

            results.append({
                'ip': ip,
                'success': result.success,
                'message': result.message,
                'device_id': result.device_id
            })

        self.batch_completed.emit(total, success_count, failed_count)

        return {
            'total': total,
            'success': success_count,
            'failed': failed_count,
            'results': results
        }

    def verify_tunnel(self, port: int) -> bool:
        """
        Verify if a tunnel is active

        Args:
            port: Tunnel port to check

        Returns:
            True if tunnel is active
        """
        settings = self.db.get_all_settings()
        vps_key_path = settings.get('vps_key_path', config.VPS_KEY_PATH)

        if not vps_key_path:
            return False

        vps = VPSManager()
        try:
            if vps.connect(
                ip=settings.get('vps_ip', config.VPS_IP),
                port=int(settings.get('vps_ssh_port', config.VPS_SSH_PORT)),
                username=settings.get('vps_user', config.VPS_USER),
                key_path=vps_key_path
            ):
                return vps.check_tunnel_active(port)
        finally:
            vps.close()

        return False

    def remove_device(self, device_id: int) -> bool:
        """
        Remove a device from the system

        Args:
            device_id: ID of device to remove

        Returns:
            True if successful
        """
        device = self.db.get_device(device_id)
        if not device:
            return False

        # Try to remove key from VPS
        if device.public_key:
            settings = self.db.get_all_settings()
            vps_key_path = settings.get('vps_key_path', config.VPS_KEY_PATH)

            if vps_key_path:
                vps = VPSManager()
                try:
                    if vps.connect(
                        ip=settings.get('vps_ip', config.VPS_IP),
                        port=int(settings.get('vps_ssh_port', config.VPS_SSH_PORT)),
                        username=settings.get('vps_user', config.VPS_USER),
                        key_path=vps_key_path
                    ):
                        vps.remove_authorized_key(device.public_key)
                finally:
                    vps.close()

        # Delete from database
        self.db.add_log(device_id, 'delete', 'success', f"Cihaz silindi: {device.device_name}")
        return self.db.delete_device(device_id)

    def update_device_status(self, device_id: int) -> str:
        """
        Update device status by checking tunnel

        Returns:
            New status string
        """
        device = self.db.get_device(device_id)
        if not device:
            return 'unknown'

        if self.verify_tunnel(device.tunnel_port):
            status = 'online'
        else:
            status = 'offline'

        from datetime import datetime
        self.db.update_device(device_id, {
            'status': status,
            'last_seen': datetime.now().isoformat() if status == 'online' else device.last_seen
        })

        return status

    def stop(self):
        """Stop ongoing provision operations"""
        self._stop_requested = True
        logger.info("Provision stop requested")

    def _emit_progress(self, ip: str, step: ProvisionStep):
        """Emit progress signal"""
        message = STEP_MESSAGES.get(step, "İşlem yapılıyor...")
        self.progress_updated.emit(ip, step.value, self.total_steps, message)
        logger.info(f"[{ip}] Step {step.value}/{self.total_steps}: {message}")

    def _fail(self, ip: str, step: ProvisionStep, message: str) -> ProvisionResult:
        """Handle provision failure"""
        full_message = f"Hata ({STEP_MESSAGES.get(step, 'Bilinmeyen')}): {message}"
        logger.error(f"[{ip}] {full_message}")

        self.device_completed.emit(ip, False, full_message)
        self.db.add_log(None, 'provision', 'failed', f"{ip}: {full_message}")

        return ProvisionResult(
            success=False,
            message=full_message,
            error_step=step
        )
