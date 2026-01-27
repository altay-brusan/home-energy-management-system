"""
Settings Widget for EC100 Provisioning Tool
Application settings interface
"""
import os
import logging

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QGroupBox, QLabel,
    QLineEdit, QPushButton, QSpinBox, QFileDialog, QMessageBox,
    QFormLayout
)
from PyQt6.QtCore import pyqtSignal, QThread

import config
from database.db_manager import DatabaseManager
from core.vps_manager import VPSManager

logger = logging.getLogger(__name__)


class VPSTestWorker(QThread):
    """Worker to test VPS connection"""
    finished = pyqtSignal(bool, str)

    def __init__(self, ip: str, port: int, user: str, key_path: str):
        super().__init__()
        self.ip = ip
        self.port = port
        self.user = user
        self.key_path = key_path

    def run(self):
        vps = VPSManager()
        try:
            if vps.connect(
                ip=self.ip,
                port=self.port,
                username=self.user,
                key_path=self.key_path
            ):
                # Try a simple command
                stdout, _, exit_code = vps.execute_command("echo 'test'")
                if exit_code == 0:
                    self.finished.emit(True, "Bağlantı başarılı!")
                else:
                    self.finished.emit(False, "Bağlantı kuruldu ancak komut çalıştırılamadı")
            else:
                self.finished.emit(False, "Bağlantı kurulamadı")
        except Exception as e:
            self.finished.emit(False, str(e))
        finally:
            vps.close()


class SettingsWidget(QWidget):
    """Widget for application settings"""

    settings_saved = pyqtSignal()

    def __init__(self, db: DatabaseManager):
        super().__init__()
        self.db = db
        self.test_worker = None

        self.setup_ui()
        self.load_settings()

    def setup_ui(self):
        """Setup UI components"""
        layout = QVBoxLayout(self)

        # VPS Settings
        vps_group = QGroupBox("VPS Ayarları")
        vps_layout = QFormLayout(vps_group)

        self.vps_ip_input = QLineEdit()
        self.vps_ip_input.setPlaceholderText("REDACTED_HOST")
        vps_layout.addRow("IP Adresi:", self.vps_ip_input)

        self.vps_port_input = QSpinBox()
        self.vps_port_input.setRange(1, 65535)
        self.vps_port_input.setValue(2222)
        vps_layout.addRow("SSH Portu:", self.vps_port_input)

        self.vps_user_input = QLineEdit()
        self.vps_user_input.setPlaceholderText("tunnel")
        vps_layout.addRow("Kullanıcı:", self.vps_user_input)

        key_row = QHBoxLayout()
        self.vps_key_input = QLineEdit()
        self.vps_key_input.setPlaceholderText("C:\\Users\\...\\id_ed25519")
        key_row.addWidget(self.vps_key_input)
        self.browse_key_btn = QPushButton("Gözat")
        self.browse_key_btn.clicked.connect(self.browse_key)
        key_row.addWidget(self.browse_key_btn)
        vps_layout.addRow("SSH Key:", key_row)

        # Test button
        test_row = QHBoxLayout()
        self.test_btn = QPushButton("Bağlantıyı Test Et")
        self.test_btn.clicked.connect(self.test_vps_connection)
        test_row.addWidget(self.test_btn)
        self.test_status = QLabel("")
        test_row.addWidget(self.test_status)
        test_row.addStretch()
        vps_layout.addRow("", test_row)

        layout.addWidget(vps_group)

        # EC100 Settings
        ec100_group = QGroupBox("EC100 Varsayılan Ayarları")
        ec100_layout = QFormLayout(ec100_group)

        self.ec100_user_input = QLineEdit()
        self.ec100_user_input.setPlaceholderText("root")
        ec100_layout.addRow("Varsayılan Kullanıcı:", self.ec100_user_input)

        password_row = QHBoxLayout()
        self.ec100_password_input = QLineEdit()
        self.ec100_password_input.setEchoMode(QLineEdit.EchoMode.Password)
        self.ec100_password_input.setPlaceholderText("••••••••")
        password_row.addWidget(self.ec100_password_input)
        self.show_password_btn = QPushButton("Göster")
        self.show_password_btn.clicked.connect(self.toggle_password)
        password_row.addWidget(self.show_password_btn)
        ec100_layout.addRow("Varsayılan Şifre:", password_row)

        self.ec100_port_input = QSpinBox()
        self.ec100_port_input.setRange(1, 65535)
        self.ec100_port_input.setValue(22)
        ec100_layout.addRow("SSH Portu:", self.ec100_port_input)

        layout.addWidget(ec100_group)

        # Port Range Settings
        port_group = QGroupBox("Port Aralığı")
        port_layout = QFormLayout(port_group)

        self.port_start_input = QSpinBox()
        self.port_start_input.setRange(1024, 65535)
        self.port_start_input.setValue(10001)
        port_layout.addRow("Başlangıç Portu:", self.port_start_input)

        self.port_end_input = QSpinBox()
        self.port_end_input.setRange(1024, 65535)
        self.port_end_input.setValue(11000)
        port_layout.addRow("Bitiş Portu:", self.port_end_input)

        # Port info
        self.port_info_label = QLabel("")
        port_layout.addRow("", self.port_info_label)

        self.port_start_input.valueChanged.connect(self.update_port_info)
        self.port_end_input.valueChanged.connect(self.update_port_info)

        layout.addWidget(port_group)

        # Network Settings
        network_group = QGroupBox("Ağ Tarama Ayarları")
        network_layout = QFormLayout(network_group)

        self.subnet_input = QLineEdit()
        self.subnet_input.setPlaceholderText("192.168.1.0/24")
        network_layout.addRow("Varsayılan Subnet:", self.subnet_input)

        layout.addWidget(network_group)

        # Buttons
        btn_row = QHBoxLayout()
        btn_row.addStretch()

        self.reset_btn = QPushButton("Varsayılana Dön")
        self.reset_btn.clicked.connect(self.reset_to_defaults)
        btn_row.addWidget(self.reset_btn)

        self.save_btn = QPushButton("Kaydet")
        self.save_btn.setObjectName("successButton")
        self.save_btn.clicked.connect(self.save_settings)
        btn_row.addWidget(self.save_btn)

        layout.addLayout(btn_row)

        # Spacer
        layout.addStretch()

    def load_settings(self):
        """Load settings from database"""
        settings = self.db.get_all_settings()

        self.vps_ip_input.setText(settings.get('vps_ip', config.VPS_IP))
        self.vps_port_input.setValue(int(settings.get('vps_ssh_port', config.VPS_SSH_PORT)))
        self.vps_user_input.setText(settings.get('vps_user', config.VPS_USER))
        self.vps_key_input.setText(settings.get('vps_key_path', ''))

        self.ec100_user_input.setText(settings.get('ec100_default_user', config.EC100_DEFAULT_USER))
        self.ec100_password_input.setText(settings.get('ec100_default_password', config.EC100_DEFAULT_PASSWORD))
        self.ec100_port_input.setValue(int(settings.get('ec100_ssh_port', config.EC100_SSH_PORT)))

        self.port_start_input.setValue(int(settings.get('port_range_start', config.PORT_RANGE_START)))
        self.port_end_input.setValue(int(settings.get('port_range_end', config.PORT_RANGE_END)))

        self.subnet_input.setText(settings.get('scan_subnet', config.DEFAULT_SUBNET))

        self.update_port_info()

    def save_settings(self):
        """Save settings to database"""
        # Validate
        if not self.vps_ip_input.text().strip():
            QMessageBox.warning(self, "Uyarı", "VPS IP adresi gerekli")
            return

        if not self.vps_key_input.text().strip():
            QMessageBox.warning(self, "Uyarı", "VPS SSH key yolu gerekli")
            return

        key_path = self.vps_key_input.text().strip()
        if not os.path.exists(key_path):
            QMessageBox.warning(self, "Uyarı", f"SSH key dosyası bulunamadı:\n{key_path}")
            return

        if self.port_start_input.value() >= self.port_end_input.value():
            QMessageBox.warning(self, "Uyarı", "Başlangıç portu, bitiş portundan küçük olmalı")
            return

        # Save
        self.db.set_setting('vps_ip', self.vps_ip_input.text().strip())
        self.db.set_setting('vps_ssh_port', str(self.vps_port_input.value()))
        self.db.set_setting('vps_user', self.vps_user_input.text().strip())
        self.db.set_setting('vps_key_path', key_path)

        self.db.set_setting('ec100_default_user', self.ec100_user_input.text().strip())
        self.db.set_setting('ec100_default_password', self.ec100_password_input.text())
        self.db.set_setting('ec100_ssh_port', str(self.ec100_port_input.value()))

        self.db.set_setting('port_range_start', str(self.port_start_input.value()))
        self.db.set_setting('port_range_end', str(self.port_end_input.value()))

        self.db.set_setting('scan_subnet', self.subnet_input.text().strip())

        QMessageBox.information(self, "Başarılı", "Ayarlar kaydedildi")
        self.settings_saved.emit()

    def reset_to_defaults(self):
        """Reset to default values"""
        reply = QMessageBox.question(
            self, "Onay",
            "Tüm ayarlar varsayılan değerlere döndürülecek. Devam?",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )

        if reply != QMessageBox.StandardButton.Yes:
            return

        self.vps_ip_input.setText(config.VPS_IP)
        self.vps_port_input.setValue(config.VPS_SSH_PORT)
        self.vps_user_input.setText(config.VPS_USER)
        self.vps_key_input.clear()

        self.ec100_user_input.setText(config.EC100_DEFAULT_USER)
        self.ec100_password_input.setText(config.EC100_DEFAULT_PASSWORD)
        self.ec100_port_input.setValue(config.EC100_SSH_PORT)

        self.port_start_input.setValue(config.PORT_RANGE_START)
        self.port_end_input.setValue(config.PORT_RANGE_END)

        self.subnet_input.setText(config.DEFAULT_SUBNET)

    def browse_key(self):
        """Browse for SSH key file"""
        filename, _ = QFileDialog.getOpenFileName(
            self,
            "SSH Key Seç",
            os.path.expanduser("~/.ssh"),
            "All Files (*)"
        )

        if filename:
            self.vps_key_input.setText(filename)

    def toggle_password(self):
        """Toggle password visibility"""
        if self.ec100_password_input.echoMode() == QLineEdit.EchoMode.Password:
            self.ec100_password_input.setEchoMode(QLineEdit.EchoMode.Normal)
            self.show_password_btn.setText("Gizle")
        else:
            self.ec100_password_input.setEchoMode(QLineEdit.EchoMode.Password)
            self.show_password_btn.setText("Göster")

    def update_port_info(self):
        """Update port range info label"""
        start = self.port_start_input.value()
        end = self.port_end_input.value()
        available = max(0, end - start)

        # Get used count
        device_count = self.db.get_device_count()

        self.port_info_label.setText(
            f"Kullanılabilir: {available} port | Kullanılan: {device_count}"
        )

    def test_vps_connection(self):
        """Test VPS connection"""
        key_path = self.vps_key_input.text().strip()
        if not key_path:
            QMessageBox.warning(self, "Uyarı", "SSH key yolu gerekli")
            return

        if not os.path.exists(key_path):
            QMessageBox.warning(self, "Uyarı", f"SSH key dosyası bulunamadı:\n{key_path}")
            return

        self.test_btn.setEnabled(False)
        self.test_btn.setText("Test ediliyor...")
        self.test_status.setText("")
        self.test_status.setStyleSheet("")

        self.test_worker = VPSTestWorker(
            ip=self.vps_ip_input.text().strip(),
            port=self.vps_port_input.value(),
            user=self.vps_user_input.text().strip(),
            key_path=key_path
        )
        self.test_worker.finished.connect(self.on_test_finished)
        self.test_worker.start()

    def on_test_finished(self, success: bool, message: str):
        """Handle test completion"""
        self.test_btn.setEnabled(True)
        self.test_btn.setText("Bağlantıyı Test Et")

        if success:
            self.test_status.setText("Bağlı")
            self.test_status.setStyleSheet("color: #4ec9b0;")
        else:
            self.test_status.setText("Hata")
            self.test_status.setStyleSheet("color: #f14c4c;")
            QMessageBox.warning(self, "Bağlantı Hatası", message)
