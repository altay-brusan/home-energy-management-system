"""
Main Window for EC100 Provisioning Tool
"""
import os
import logging
from PyQt6.QtWidgets import (
    QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QTabWidget, QStatusBar, QLabel, QMessageBox
)
from PyQt6.QtCore import Qt, QTimer
from PyQt6.QtGui import QIcon

import config
from database.db_manager import DatabaseManager
from core.vps_manager import VPSManager
from .scan_widget import ScanWidget
from .inventory_widget import InventoryWidget
from .settings_widget import SettingsWidget
from .logs_widget import LogsWidget

logger = logging.getLogger(__name__)


class MainWindow(QMainWindow):
    """Main application window"""

    def __init__(self):
        super().__init__()
        self.db = DatabaseManager()

        self.setup_logging()
        self.setup_ui()
        self.load_stylesheet()
        self.setup_status_bar()
        self.start_status_timer()

    def setup_logging(self):
        """Setup file logging"""
        os.makedirs(os.path.dirname(config.LOG_PATH), exist_ok=True)

        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s [%(levelname)s] %(name)s: %(message)s',
            handlers=[
                logging.FileHandler(config.LOG_PATH, encoding='utf-8'),
                logging.StreamHandler()
            ]
        )

    def setup_ui(self):
        """Setup main UI components"""
        self.setWindowTitle(f"{config.APP_NAME} v{config.APP_VERSION}")
        self.setMinimumSize(1200, 800)

        # Central widget
        central_widget = QWidget()
        self.setCentralWidget(central_widget)

        layout = QVBoxLayout(central_widget)
        layout.setContentsMargins(10, 10, 10, 10)

        # Tab widget
        self.tabs = QTabWidget()
        layout.addWidget(self.tabs)

        # Create tabs
        self.scan_widget = ScanWidget(self.db)
        self.inventory_widget = InventoryWidget(self.db)
        self.settings_widget = SettingsWidget(self.db)
        self.logs_widget = LogsWidget(self.db)

        self.tabs.addTab(self.scan_widget, "Tara ve Kur")
        self.tabs.addTab(self.inventory_widget, "Envanter")
        self.tabs.addTab(self.settings_widget, "Ayarlar")
        self.tabs.addTab(self.logs_widget, "Loglar")

        # Connect signals
        self.scan_widget.device_provisioned.connect(self.on_device_provisioned)
        self.settings_widget.settings_saved.connect(self.on_settings_saved)

        # Tab changed signal
        self.tabs.currentChanged.connect(self.on_tab_changed)

    def load_stylesheet(self):
        """Load QSS stylesheet"""
        style_path = os.path.join(os.path.dirname(__file__), 'styles.qss')
        try:
            with open(style_path, 'r', encoding='utf-8') as f:
                self.setStyleSheet(f.read())
        except Exception as e:
            logger.warning(f"Could not load stylesheet: {e}")

    def setup_status_bar(self):
        """Setup status bar"""
        self.status_bar = QStatusBar()
        self.setStatusBar(self.status_bar)

        # VPS status label
        self.vps_status_label = QLabel("VPS: Kontrol ediliyor...")
        self.status_bar.addWidget(self.vps_status_label)

        # Separator
        separator = QLabel("|")
        self.status_bar.addWidget(separator)

        # Device count label
        self.device_count_label = QLabel("Cihaz: 0")
        self.status_bar.addWidget(self.device_count_label)

        # Separator
        separator2 = QLabel("|")
        self.status_bar.addWidget(separator2)

        # Version label (right side)
        self.version_label = QLabel(f"v{config.APP_VERSION}")
        self.status_bar.addPermanentWidget(self.version_label)

        self.update_status_bar()

    def start_status_timer(self):
        """Start timer for status updates"""
        self.status_timer = QTimer()
        self.status_timer.timeout.connect(self.update_status_bar)
        self.status_timer.start(30000)  # Every 30 seconds

    def update_status_bar(self):
        """Update status bar information"""
        # Update device count
        count = self.db.get_device_count()
        self.device_count_label.setText(f"Cihaz: {count}")

        # Check VPS connection in background
        QTimer.singleShot(0, self.check_vps_status)

    def check_vps_status(self):
        """Check VPS connection status"""
        settings = self.db.get_all_settings()
        vps_key_path = settings.get('vps_key_path', '')

        if not vps_key_path:
            self.vps_status_label.setText("VPS: Key yapılandırılmamış")
            self.vps_status_label.setStyleSheet("color: #ce9178;")
            return

        vps = VPSManager()
        try:
            if vps.connect(
                ip=settings.get('vps_ip', config.VPS_IP),
                port=int(settings.get('vps_ssh_port', config.VPS_SSH_PORT)),
                username=settings.get('vps_user', config.VPS_USER),
                key_path=vps_key_path
            ):
                self.vps_status_label.setText(f"VPS: {settings.get('vps_ip')} Bağlı")
                self.vps_status_label.setStyleSheet("color: #4ec9b0;")
            else:
                self.vps_status_label.setText("VPS: Bağlantı hatası")
                self.vps_status_label.setStyleSheet("color: #f14c4c;")
        except Exception as e:
            logger.debug(f"VPS status check failed: {e}")
            self.vps_status_label.setText("VPS: Bağlantı hatası")
            self.vps_status_label.setStyleSheet("color: #f14c4c;")
        finally:
            vps.close()

    def on_device_provisioned(self):
        """Handle device provisioned event"""
        self.update_status_bar()
        self.inventory_widget.refresh_devices()

    def on_settings_saved(self):
        """Handle settings saved event"""
        self.update_status_bar()

    def on_tab_changed(self, index):
        """Handle tab change"""
        if index == 1:  # Inventory tab
            self.inventory_widget.refresh_devices()
        elif index == 3:  # Logs tab
            self.logs_widget.refresh_logs()

    def closeEvent(self, event):
        """Handle window close"""
        reply = QMessageBox.question(
            self,
            'Çıkış',
            'Uygulamadan çıkmak istediğinize emin misiniz?',
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No,
            QMessageBox.StandardButton.No
        )

        if reply == QMessageBox.StandardButton.Yes:
            event.accept()
        else:
            event.ignore()
