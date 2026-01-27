"""
Scan Widget for EC100 Provisioning Tool
Network scanning and device provisioning interface
"""
import logging
from typing import List, Dict

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QGroupBox, QLabel,
    QLineEdit, QPushButton, QTableWidget, QTableWidgetItem,
    QProgressBar, QCheckBox, QHeaderView, QMessageBox,
    QSplitter, QTextEdit, QFormLayout, QSpinBox
)
from PyQt6.QtCore import Qt, QThread, pyqtSignal
from PyQt6.QtGui import QColor

import config
from database.db_manager import DatabaseManager
from core.scanner import NetworkScanner
from core.provisioner import Provisioner

logger = logging.getLogger(__name__)


class ScanWorker(QThread):
    """Worker thread for network scanning"""
    progress = pyqtSignal(int, int, str)
    finished = pyqtSignal(list)
    error = pyqtSignal(str)

    def __init__(self, subnet: str):
        super().__init__()
        self.subnet = subnet
        self.scanner = NetworkScanner()

    def run(self):
        try:
            devices = self.scanner.scan_network(
                subnet=self.subnet,
                progress_callback=lambda c, t, ip: self.progress.emit(c, t, ip)
            )
            self.finished.emit(devices)
        except Exception as e:
            self.error.emit(str(e))

    def stop(self):
        self.scanner.stop()


class ProvisionWorker(QThread):
    """Worker thread for device provisioning"""
    progress = pyqtSignal(str, int, int, str)
    device_done = pyqtSignal(str, bool, str)
    finished = pyqtSignal(dict)

    def __init__(self, db: DatabaseManager, devices: List[Dict]):
        super().__init__()
        self.db = db
        self.devices = devices
        self.provisioner = Provisioner(db)
        self.provisioner.progress_updated.connect(self.progress.emit)
        self.provisioner.device_completed.connect(self.device_done.emit)

    def run(self):
        result = self.provisioner.provision_batch(self.devices)
        self.finished.emit(result)

    def stop(self):
        self.provisioner.stop()


class ScanWidget(QWidget):
    """Widget for scanning and provisioning devices"""

    device_provisioned = pyqtSignal()

    def __init__(self, db: DatabaseManager):
        super().__init__()
        self.db = db
        self.scan_worker = None
        self.provision_worker = None
        self.found_devices = []

        self.setup_ui()

    def setup_ui(self):
        """Setup UI components"""
        layout = QVBoxLayout(self)

        # Create splitter for resizable sections
        splitter = QSplitter(Qt.Orientation.Vertical)
        layout.addWidget(splitter)

        # Top section - Scanning
        top_widget = QWidget()
        top_layout = QVBoxLayout(top_widget)
        top_layout.setContentsMargins(0, 0, 0, 0)

        # Scan controls group
        scan_group = QGroupBox("Ağ Tarama")
        scan_layout = QVBoxLayout(scan_group)

        # Subnet input row
        subnet_row = QHBoxLayout()
        subnet_row.addWidget(QLabel("Subnet:"))
        self.subnet_input = QLineEdit()
        self.subnet_input.setPlaceholderText("192.168.1.0/24")
        self.subnet_input.setText(self.db.get_setting('scan_subnet') or config.DEFAULT_SUBNET)
        subnet_row.addWidget(self.subnet_input)

        self.scan_btn = QPushButton("Tara")
        self.scan_btn.clicked.connect(self.start_scan)
        subnet_row.addWidget(self.scan_btn)

        self.stop_scan_btn = QPushButton("Durdur")
        self.stop_scan_btn.clicked.connect(self.stop_scan)
        self.stop_scan_btn.setEnabled(False)
        subnet_row.addWidget(self.stop_scan_btn)

        scan_layout.addLayout(subnet_row)

        # Scan progress
        self.scan_progress = QProgressBar()
        self.scan_progress.setVisible(False)
        scan_layout.addWidget(self.scan_progress)

        self.scan_status = QLabel("")
        scan_layout.addWidget(self.scan_status)

        top_layout.addWidget(scan_group)

        # Devices table group
        devices_group = QGroupBox("Bulunan Cihazlar")
        devices_layout = QVBoxLayout(devices_group)

        # Table
        self.devices_table = QTableWidget()
        self.devices_table.setColumnCount(4)
        self.devices_table.setHorizontalHeaderLabels(["", "IP Adresi", "MAC Adresi", "Durum"])
        self.devices_table.horizontalHeader().setSectionResizeMode(0, QHeaderView.ResizeMode.Fixed)
        self.devices_table.horizontalHeader().setSectionResizeMode(1, QHeaderView.ResizeMode.Stretch)
        self.devices_table.horizontalHeader().setSectionResizeMode(2, QHeaderView.ResizeMode.Stretch)
        self.devices_table.horizontalHeader().setSectionResizeMode(3, QHeaderView.ResizeMode.Stretch)
        self.devices_table.setColumnWidth(0, 40)
        self.devices_table.setSelectionBehavior(QTableWidget.SelectionBehavior.SelectRows)
        self.devices_table.setAlternatingRowColors(True)
        self.devices_table.itemSelectionChanged.connect(self.on_device_selected)
        devices_layout.addWidget(self.devices_table)

        # Selection buttons
        btn_row = QHBoxLayout()
        self.select_all_btn = QPushButton("Tümünü Seç")
        self.select_all_btn.clicked.connect(self.select_all)
        btn_row.addWidget(self.select_all_btn)

        self.deselect_all_btn = QPushButton("Seçimi Kaldır")
        self.deselect_all_btn.clicked.connect(self.deselect_all)
        btn_row.addWidget(self.deselect_all_btn)

        btn_row.addStretch()

        self.selected_count_label = QLabel("Seçili: 0 cihaz")
        btn_row.addWidget(self.selected_count_label)

        devices_layout.addLayout(btn_row)

        top_layout.addWidget(devices_group)
        splitter.addWidget(top_widget)

        # Bottom section - Provisioning
        bottom_widget = QWidget()
        bottom_layout = QVBoxLayout(bottom_widget)
        bottom_layout.setContentsMargins(0, 0, 0, 0)

        # Device info group
        info_group = QGroupBox("Cihaz Bilgileri (Seçili cihaz için)")
        info_layout = QFormLayout(info_group)

        self.ip_label = QLabel("-")
        info_layout.addRow("IP Adresi:", self.ip_label)

        self.mac_label = QLabel("-")
        info_layout.addRow("MAC Adresi:", self.mac_label)

        self.device_name_input = QLineEdit()
        self.device_name_input.setPlaceholderText("EC100-Fabrika-A")
        info_layout.addRow("Cihaz Adı:", self.device_name_input)

        self.ec100_serial_input = QLineEdit()
        self.ec100_serial_input.setPlaceholderText("EC100 seri numarası")
        info_layout.addRow("EC100 Seri No:", self.ec100_serial_input)

        self.netzr_serial_input = QLineEdit()
        self.netzr_serial_input.setPlaceholderText("NetZr seri numarası")
        info_layout.addRow("NetZr Cihaz Seri No:", self.netzr_serial_input)

        port_row = QHBoxLayout()
        self.port_input = QSpinBox()
        self.port_input.setRange(config.PORT_RANGE_START, config.PORT_RANGE_END)
        self.port_input.setValue(self.db.get_next_port())
        port_row.addWidget(self.port_input)
        port_row.addWidget(QLabel("(Otomatik atandı)"))
        port_row.addStretch()
        info_layout.addRow("Port:", port_row)

        bottom_layout.addWidget(info_group)

        # Provision button
        self.provision_btn = QPushButton("Kurulumu Başlat")
        self.provision_btn.setObjectName("successButton")
        self.provision_btn.clicked.connect(self.start_provision)
        self.provision_btn.setMinimumHeight(40)
        bottom_layout.addWidget(self.provision_btn)

        # Progress group
        progress_group = QGroupBox("Kurulum İlerlemesi")
        progress_layout = QVBoxLayout(progress_group)

        self.provision_device_label = QLabel("")
        progress_layout.addWidget(self.provision_device_label)

        self.provision_progress = QProgressBar()
        self.provision_progress.setMaximum(10)
        progress_layout.addWidget(self.provision_progress)

        # Step indicators
        self.step_text = QTextEdit()
        self.step_text.setReadOnly(True)
        self.step_text.setMaximumHeight(150)
        progress_layout.addWidget(self.step_text)

        bottom_layout.addWidget(progress_group)
        splitter.addWidget(bottom_widget)

        # Set splitter sizes
        splitter.setSizes([400, 300])

    def start_scan(self):
        """Start network scan"""
        subnet = self.subnet_input.text().strip()
        if not subnet:
            QMessageBox.warning(self, "Uyarı", "Lütfen subnet girin")
            return

        # Validate subnet
        scanner = NetworkScanner()
        if not scanner.validate_subnet(subnet):
            QMessageBox.warning(self, "Uyarı", "Geçersiz subnet formatı")
            return

        self.scan_btn.setEnabled(False)
        self.stop_scan_btn.setEnabled(True)
        self.scan_progress.setVisible(True)
        self.scan_progress.setValue(0)
        self.devices_table.setRowCount(0)
        self.found_devices = []
        self.scan_status.setText("Tarama başlatılıyor...")

        self.scan_worker = ScanWorker(subnet)
        self.scan_worker.progress.connect(self.on_scan_progress)
        self.scan_worker.finished.connect(self.on_scan_finished)
        self.scan_worker.error.connect(self.on_scan_error)
        self.scan_worker.start()

    def stop_scan(self):
        """Stop network scan"""
        if self.scan_worker:
            self.scan_worker.stop()
            self.scan_status.setText("Tarama durduruluyor...")

    def on_scan_progress(self, current: int, total: int, ip: str):
        """Handle scan progress update"""
        self.scan_progress.setMaximum(total)
        self.scan_progress.setValue(current)
        self.scan_status.setText(f"Taranan: {ip} ({current}/{total})")

    def on_scan_finished(self, devices: List[Dict]):
        """Handle scan completion"""
        self.scan_btn.setEnabled(True)
        self.stop_scan_btn.setEnabled(False)
        self.scan_progress.setVisible(False)

        self.found_devices = devices
        self.populate_devices_table(devices)

        self.scan_status.setText(f"Tarama tamamlandı. {len(devices)} cihaz bulundu.")

    def on_scan_error(self, error: str):
        """Handle scan error"""
        self.scan_btn.setEnabled(True)
        self.stop_scan_btn.setEnabled(False)
        self.scan_progress.setVisible(False)
        self.scan_status.setText(f"Hata: {error}")
        QMessageBox.critical(self, "Tarama Hatası", error)

    def populate_devices_table(self, devices: List[Dict]):
        """Populate devices table with scan results"""
        self.devices_table.setRowCount(len(devices))

        for row, device in enumerate(devices):
            # Checkbox
            checkbox = QCheckBox()
            checkbox.stateChanged.connect(self.update_selection_count)
            self.devices_table.setCellWidget(row, 0, checkbox)

            # IP
            ip_item = QTableWidgetItem(device.get('ip', ''))
            ip_item.setFlags(ip_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 1, ip_item)

            # MAC
            mac = device.get('mac', '-')
            mac_item = QTableWidgetItem(mac if mac else '-')
            mac_item.setFlags(mac_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 2, mac_item)

            # Status - check if already provisioned
            mac_upper = mac.upper() if mac else None
            existing = self.db.get_device_by_mac(mac_upper) if mac_upper else None

            if existing:
                status_item = QTableWidgetItem(f"Zaten kurulu (Port: {existing.tunnel_port})")
                status_item.setForeground(QColor("#ce9178"))
                checkbox.setEnabled(False)
            else:
                status_item = QTableWidgetItem("SSH Açık")
                status_item.setForeground(QColor("#4ec9b0"))

            status_item.setFlags(status_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 3, status_item)

    def select_all(self):
        """Select all enabled devices"""
        for row in range(self.devices_table.rowCount()):
            checkbox = self.devices_table.cellWidget(row, 0)
            if checkbox and checkbox.isEnabled():
                checkbox.setChecked(True)

    def deselect_all(self):
        """Deselect all devices"""
        for row in range(self.devices_table.rowCount()):
            checkbox = self.devices_table.cellWidget(row, 0)
            if checkbox:
                checkbox.setChecked(False)

    def update_selection_count(self):
        """Update selection count label"""
        count = self.get_selected_count()
        self.selected_count_label.setText(f"Seçili: {count} cihaz")

    def get_selected_count(self) -> int:
        """Get number of selected devices"""
        count = 0
        for row in range(self.devices_table.rowCount()):
            checkbox = self.devices_table.cellWidget(row, 0)
            if checkbox and checkbox.isChecked():
                count += 1
        return count

    def get_selected_devices(self) -> List[Dict]:
        """Get list of selected devices"""
        selected = []
        for row in range(self.devices_table.rowCount()):
            checkbox = self.devices_table.cellWidget(row, 0)
            if checkbox and checkbox.isChecked():
                selected.append(self.found_devices[row])
        return selected

    def on_device_selected(self):
        """Handle device selection in table"""
        rows = self.devices_table.selectionModel().selectedRows()
        if rows:
            row = rows[0].row()
            if row < len(self.found_devices):
                device = self.found_devices[row]
                self.ip_label.setText(device.get('ip', '-'))
                self.mac_label.setText(device.get('mac', '-') or '-')

                # Auto-generate device name
                ip = device.get('ip', '')
                self.device_name_input.setText(f"EC100-{ip.split('.')[-1]}")

                # Update port
                try:
                    self.port_input.setValue(self.db.get_next_port())
                except ValueError:
                    pass

    def start_provision(self):
        """Start provisioning selected devices"""
        selected = self.get_selected_devices()

        if not selected:
            # If no checkbox selected, try to provision currently selected row
            rows = self.devices_table.selectionModel().selectedRows()
            if rows:
                row = rows[0].row()
                if row < len(self.found_devices):
                    selected = [self.found_devices[row]]

        if not selected:
            QMessageBox.warning(self, "Uyarı", "Lütfen kurulum yapılacak cihaz seçin")
            return

        # Validate device name
        device_name = self.device_name_input.text().strip()
        if not device_name:
            QMessageBox.warning(self, "Uyarı", "Lütfen cihaz adı girin")
            return

        # Check VPS key
        vps_key = self.db.get_setting('vps_key_path')
        if not vps_key:
            QMessageBox.warning(
                self, "Uyarı",
                "VPS SSH key yapılandırılmamış.\nLütfen Ayarlar sekmesinden VPS key yolunu belirleyin."
            )
            return

        # Prepare devices for provisioning
        devices_to_provision = []
        base_port = self.port_input.value()

        for i, device in enumerate(selected):
            devices_to_provision.append({
                'ip': device.get('ip'),
                'device_name': f"{device_name}" if len(selected) == 1 else f"{device_name}-{i + 1}",
                'location': self.netzr_serial_input.text().strip(),  # NetZr Seri No
                'serial_number': self.ec100_serial_input.text().strip(),  # EC100 Seri No
                'port': base_port + i if len(selected) > 1 else base_port
            })

        # Confirm
        msg = f"{len(devices_to_provision)} cihaza kurulum yapılacak. Devam etmek istiyor musunuz?"
        reply = QMessageBox.question(
            self, "Onay", msg,
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )

        if reply != QMessageBox.StandardButton.Yes:
            return

        # Start provisioning
        self.provision_btn.setEnabled(False)
        self.scan_btn.setEnabled(False)
        self.provision_progress.setValue(0)
        self.step_text.clear()

        self.provision_worker = ProvisionWorker(self.db, devices_to_provision)
        self.provision_worker.progress.connect(self.on_provision_progress)
        self.provision_worker.device_done.connect(self.on_device_done)
        self.provision_worker.finished.connect(self.on_provision_finished)
        self.provision_worker.start()

    def on_provision_progress(self, ip: str, step: int, total: int, message: str):
        """Handle provision progress"""
        self.provision_device_label.setText(f"Cihaz: {ip}")
        self.provision_progress.setMaximum(total)
        self.provision_progress.setValue(step)

        # Add step to log
        status_icon = "⏳" if step < total else "✅"
        self.step_text.append(f"{status_icon} {message}")

        # Auto-scroll
        scrollbar = self.step_text.verticalScrollBar()
        scrollbar.setValue(scrollbar.maximum())

    def on_device_done(self, ip: str, success: bool, message: str):
        """Handle device provisioning completion"""
        icon = "✅" if success else "❌"
        self.step_text.append(f"\n{icon} [{ip}] {message}\n")

        # Update table status
        for row in range(self.devices_table.rowCount()):
            ip_item = self.devices_table.item(row, 1)
            if ip_item and ip_item.text() == ip:
                status_item = self.devices_table.item(row, 3)
                if success:
                    status_item.setText("Kurulum tamamlandı")
                    status_item.setForeground(QColor("#4ec9b0"))
                else:
                    status_item.setText("Kurulum başarısız")
                    status_item.setForeground(QColor("#f14c4c"))
                break

    def on_provision_finished(self, result: Dict):
        """Handle provisioning completion"""
        self.provision_btn.setEnabled(True)
        self.scan_btn.setEnabled(True)

        total = result.get('total', 0)
        success = result.get('success', 0)
        failed = result.get('failed', 0)

        self.provision_device_label.setText(f"Tamamlandı: {success}/{total} başarılı")

        if failed > 0:
            QMessageBox.warning(
                self, "Kurulum Tamamlandı",
                f"Toplam: {total}\nBaşarılı: {success}\nBaşarısız: {failed}"
            )
        else:
            QMessageBox.information(
                self, "Kurulum Tamamlandı",
                f"Tüm cihazlar başarıyla kuruldu ({success}/{total})"
            )

        # Emit signal to refresh inventory
        if success > 0:
            self.device_provisioned.emit()

        # Update next port
        try:
            self.port_input.setValue(self.db.get_next_port())
        except ValueError:
            pass
