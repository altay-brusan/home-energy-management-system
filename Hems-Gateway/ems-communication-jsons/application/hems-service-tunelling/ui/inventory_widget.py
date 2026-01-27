"""
Inventory Widget for EC100 Provisioning Tool
Device management and inventory interface
"""
import csv
import logging
from datetime import datetime

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QGroupBox, QLabel,
    QLineEdit, QPushButton, QTableWidget, QTableWidgetItem,
    QHeaderView, QComboBox, QTextEdit, QMessageBox,
    QSplitter, QFileDialog, QApplication
)
from PyQt6.QtCore import Qt, QThread, pyqtSignal
from PyQt6.QtGui import QColor

import config
from database.db_manager import DatabaseManager
from core.provisioner import Provisioner

logger = logging.getLogger(__name__)


class StatusCheckWorker(QThread):
    """Worker to check device status"""
    status_updated = pyqtSignal(int, str)  # device_id, status
    finished = pyqtSignal()

    def __init__(self, db: DatabaseManager, device_ids: list):
        super().__init__()
        self.db = db
        self.device_ids = device_ids
        self.provisioner = Provisioner(db)

    def run(self):
        for device_id in self.device_ids:
            try:
                status = self.provisioner.update_device_status(device_id)
                self.status_updated.emit(device_id, status)
            except Exception as e:
                logger.debug(f"Status check failed for device {device_id}: {e}")
        self.finished.emit()


class InventoryWidget(QWidget):
    """Widget for device inventory management"""

    def __init__(self, db: DatabaseManager):
        super().__init__()
        self.db = db
        self.devices = []
        self.status_worker = None

        self.setup_ui()
        self.refresh_devices()

    def setup_ui(self):
        """Setup UI components"""
        layout = QVBoxLayout(self)

        # Create splitter
        splitter = QSplitter(Qt.Orientation.Vertical)
        layout.addWidget(splitter)

        # Top section - Search and table
        top_widget = QWidget()
        top_layout = QVBoxLayout(top_widget)
        top_layout.setContentsMargins(0, 0, 0, 0)

        # Search and filter group
        filter_group = QGroupBox("Arama ve Filtre")
        filter_layout = QHBoxLayout(filter_group)

        self.search_input = QLineEdit()
        self.search_input.setPlaceholderText("Ara... (Ad, Lokasyon, Seri No, MAC)")
        self.search_input.textChanged.connect(self.on_search_changed)
        filter_layout.addWidget(self.search_input)

        filter_layout.addWidget(QLabel("Durum:"))
        self.status_filter = QComboBox()
        self.status_filter.addItems(["Tümü", "Online", "Offline", "Hata"])
        self.status_filter.currentTextChanged.connect(self.on_filter_changed)
        filter_layout.addWidget(self.status_filter)

        filter_layout.addStretch()

        self.export_btn = QPushButton("CSV Dışa Aktar")
        self.export_btn.clicked.connect(self.export_csv)
        filter_layout.addWidget(self.export_btn)

        self.refresh_btn = QPushButton("Yenile")
        self.refresh_btn.clicked.connect(self.refresh_devices)
        filter_layout.addWidget(self.refresh_btn)

        self.check_status_btn = QPushButton("Durumları Kontrol Et")
        self.check_status_btn.clicked.connect(self.check_all_status)
        filter_layout.addWidget(self.check_status_btn)

        top_layout.addWidget(filter_group)

        # Devices table
        table_group = QGroupBox("Cihaz Listesi")
        table_layout = QVBoxLayout(table_group)

        self.count_label = QLabel("Toplam: 0")
        table_layout.addWidget(self.count_label)

        self.devices_table = QTableWidget()
        self.devices_table.setColumnCount(8)
        self.devices_table.setHorizontalHeaderLabels([
            "#", "Port", "Cihaz Adı", "EC100 Seri No", "NetZr Seri No", "MAC Adresi", "Durum", "Son Görülme"
        ])
        self.devices_table.horizontalHeader().setSectionResizeMode(0, QHeaderView.ResizeMode.Fixed)
        self.devices_table.horizontalHeader().setSectionResizeMode(1, QHeaderView.ResizeMode.Fixed)
        self.devices_table.horizontalHeader().setSectionResizeMode(2, QHeaderView.ResizeMode.Stretch)
        self.devices_table.horizontalHeader().setSectionResizeMode(3, QHeaderView.ResizeMode.ResizeToContents)
        self.devices_table.horizontalHeader().setSectionResizeMode(4, QHeaderView.ResizeMode.ResizeToContents)
        self.devices_table.horizontalHeader().setSectionResizeMode(5, QHeaderView.ResizeMode.ResizeToContents)
        self.devices_table.horizontalHeader().setSectionResizeMode(6, QHeaderView.ResizeMode.Fixed)
        self.devices_table.horizontalHeader().setSectionResizeMode(7, QHeaderView.ResizeMode.Fixed)
        self.devices_table.setColumnWidth(0, 40)
        self.devices_table.setColumnWidth(1, 60)
        self.devices_table.setColumnWidth(6, 70)
        self.devices_table.setColumnWidth(7, 100)
        self.devices_table.setSelectionBehavior(QTableWidget.SelectionBehavior.SelectRows)
        self.devices_table.setAlternatingRowColors(True)
        self.devices_table.itemSelectionChanged.connect(self.on_device_selected)
        table_layout.addWidget(self.devices_table)

        top_layout.addWidget(table_group)
        splitter.addWidget(top_widget)

        # Bottom section - Device details
        bottom_widget = QWidget()
        bottom_layout = QVBoxLayout(bottom_widget)
        bottom_layout.setContentsMargins(0, 0, 0, 0)

        # Detail group
        detail_group = QGroupBox("Cihaz Detayı")
        detail_layout = QVBoxLayout(detail_group)

        # Info row 1
        info_row1 = QHBoxLayout()
        self.detail_port = QLabel("Port: -")
        info_row1.addWidget(self.detail_port)
        self.detail_serial = QLabel("EC100 Seri No: -")
        info_row1.addWidget(self.detail_serial)
        info_row1.addStretch()
        detail_layout.addLayout(info_row1)

        # Info row 2
        info_row2 = QHBoxLayout()
        self.detail_mac = QLabel("MAC: -")
        info_row2.addWidget(self.detail_mac)
        self.detail_created = QLabel("Oluşturma: -")
        info_row2.addWidget(self.detail_created)
        info_row2.addStretch()
        detail_layout.addLayout(info_row2)

        # SSH command
        ssh_row = QHBoxLayout()
        ssh_row.addWidget(QLabel("SSH Komutu:"))
        self.ssh_command = QLineEdit()
        self.ssh_command.setReadOnly(True)
        self.ssh_command.setPlaceholderText("Cihaz seçin")
        ssh_row.addWidget(self.ssh_command)
        self.copy_ssh_btn = QPushButton("Kopyala")
        self.copy_ssh_btn.clicked.connect(self.copy_ssh_command)
        ssh_row.addWidget(self.copy_ssh_btn)
        detail_layout.addLayout(ssh_row)

        # Action buttons
        btn_row = QHBoxLayout()

        self.edit_btn = QPushButton("Düzenle")
        self.edit_btn.clicked.connect(self.edit_device)
        self.edit_btn.setEnabled(False)
        btn_row.addWidget(self.edit_btn)

        self.reconnect_btn = QPushButton("Durumu Kontrol Et")
        self.reconnect_btn.clicked.connect(self.check_selected_status)
        self.reconnect_btn.setEnabled(False)
        btn_row.addWidget(self.reconnect_btn)

        self.delete_btn = QPushButton("Sil")
        self.delete_btn.setObjectName("dangerButton")
        self.delete_btn.clicked.connect(self.delete_device)
        self.delete_btn.setEnabled(False)
        btn_row.addWidget(self.delete_btn)

        btn_row.addStretch()
        detail_layout.addLayout(btn_row)

        # Notes
        detail_layout.addWidget(QLabel("Notlar:"))
        self.notes_text = QTextEdit()
        self.notes_text.setMaximumHeight(80)
        self.notes_text.setReadOnly(True)
        detail_layout.addWidget(self.notes_text)

        bottom_layout.addWidget(detail_group)
        splitter.addWidget(bottom_widget)

        splitter.setSizes([500, 200])

    def refresh_devices(self):
        """Refresh device list from database"""
        self.devices = self.db.get_all_devices()
        self.populate_table(self.devices)

    def populate_table(self, devices):
        """Populate table with devices"""
        self.devices_table.setRowCount(len(devices))
        self.count_label.setText(f"Toplam: {len(devices)}")

        for row, device in enumerate(devices):
            # Row number
            num_item = QTableWidgetItem(str(row + 1))
            num_item.setFlags(num_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            num_item.setData(Qt.ItemDataRole.UserRole, device.id)
            self.devices_table.setItem(row, 0, num_item)

            # Port
            port_item = QTableWidgetItem(str(device.tunnel_port))
            port_item.setFlags(port_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 1, port_item)

            # Device name
            name_item = QTableWidgetItem(device.device_name)
            name_item.setFlags(name_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 2, name_item)

            # EC100 Seri No
            serial_item = QTableWidgetItem(device.serial_number or "-")
            serial_item.setFlags(serial_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 3, serial_item)

            # NetZr Seri No (location alanında tutuluyor)
            netzr_item = QTableWidgetItem(device.location or "-")
            netzr_item.setFlags(netzr_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 4, netzr_item)

            # MAC Adresi
            mac_item = QTableWidgetItem(device.mac_address or "-")
            mac_item.setFlags(mac_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 5, mac_item)

            # Status
            status_item = QTableWidgetItem(device.status.capitalize())
            status_item.setFlags(status_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            if device.status == 'online':
                status_item.setForeground(QColor("#4ec9b0"))
            elif device.status == 'offline':
                status_item.setForeground(QColor("#f14c4c"))
            else:
                status_item.setForeground(QColor("#ce9178"))
            self.devices_table.setItem(row, 6, status_item)

            # Last seen
            if device.last_seen:
                last_seen = self.format_time_ago(device.last_seen)
            else:
                last_seen = "-"
            last_seen_item = QTableWidgetItem(last_seen)
            last_seen_item.setFlags(last_seen_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.devices_table.setItem(row, 7, last_seen_item)

    def format_time_ago(self, dt: datetime) -> str:
        """Format datetime as relative time"""
        if not dt:
            return "-"

        now = datetime.now()
        diff = now - dt

        if diff.days > 0:
            return f"{diff.days} gün"
        elif diff.seconds >= 3600:
            return f"{diff.seconds // 3600} saat"
        elif diff.seconds >= 60:
            return f"{diff.seconds // 60} dk"
        else:
            return "Az önce"

    def on_search_changed(self, text: str):
        """Handle search text change"""
        self.apply_filters()

    def on_filter_changed(self, status: str):
        """Handle status filter change"""
        self.apply_filters()

    def apply_filters(self):
        """Apply search and status filters"""
        search_text = self.search_input.text().strip().lower()
        status_filter = self.status_filter.currentText()

        if search_text:
            filtered = self.db.search_devices(search_text)
        else:
            filtered = self.db.get_all_devices()

        if status_filter != "Tümü":
            status_map = {"Online": "online", "Offline": "offline", "Hata": "error"}
            status = status_map.get(status_filter, "")
            filtered = [d for d in filtered if d.status == status]

        self.populate_table(filtered)

    def on_device_selected(self):
        """Handle device selection"""
        rows = self.devices_table.selectionModel().selectedRows()
        if not rows:
            self.clear_details()
            return

        row = rows[0].row()
        device_id = self.devices_table.item(row, 0).data(Qt.ItemDataRole.UserRole)
        device = self.db.get_device(device_id)

        if not device:
            self.clear_details()
            return

        # Update details
        self.detail_port.setText(f"Port: {device.tunnel_port}")
        self.detail_serial.setText(f"EC100 Seri No: {device.serial_number or '-'}")
        self.detail_mac.setText(f"MAC: {device.mac_address or '-'}")

        if device.created_at:
            self.detail_created.setText(f"Oluşturma: {device.created_at.strftime('%Y-%m-%d %H:%M')}")
        else:
            self.detail_created.setText("Oluşturma: -")

        # SSH command
        settings = self.db.get_all_settings()
        vps_ip = settings.get('vps_ip', config.VPS_IP)
        vps_port = settings.get('vps_ssh_port', config.VPS_SSH_PORT)
        ssh_cmd = f"ssh -p {device.tunnel_port} root@localhost  # VPS üzerinden ({vps_ip}:{vps_port})"
        self.ssh_command.setText(ssh_cmd)

        self.notes_text.setText(device.notes or "")

        # Enable buttons
        self.edit_btn.setEnabled(True)
        self.reconnect_btn.setEnabled(True)
        self.delete_btn.setEnabled(True)

    def clear_details(self):
        """Clear detail fields"""
        self.detail_port.setText("Port: -")
        self.detail_serial.setText("EC100 Seri No: -")
        self.detail_mac.setText("MAC: -")
        self.detail_created.setText("Oluşturma: -")
        self.ssh_command.clear()
        self.notes_text.clear()
        self.edit_btn.setEnabled(False)
        self.reconnect_btn.setEnabled(False)
        self.delete_btn.setEnabled(False)

    def copy_ssh_command(self):
        """Copy SSH command to clipboard"""
        text = self.ssh_command.text()
        if text:
            clipboard = QApplication.clipboard()
            # Get just the command part
            cmd = text.split("#")[0].strip()
            clipboard.setText(cmd)
            QMessageBox.information(self, "Kopyalandı", "SSH komutu panoya kopyalandı")

    def check_selected_status(self):
        """Check status of selected device"""
        rows = self.devices_table.selectionModel().selectedRows()
        if not rows:
            return

        row = rows[0].row()
        device_id = self.devices_table.item(row, 0).data(Qt.ItemDataRole.UserRole)

        self.reconnect_btn.setEnabled(False)
        self.reconnect_btn.setText("Kontrol ediliyor...")

        self.status_worker = StatusCheckWorker(self.db, [device_id])
        self.status_worker.status_updated.connect(self.on_status_updated)
        self.status_worker.finished.connect(self.on_status_check_finished)
        self.status_worker.start()

    def check_all_status(self):
        """Check status of all devices"""
        if not self.devices:
            return

        reply = QMessageBox.question(
            self, "Onay",
            f"{len(self.devices)} cihazın durumu kontrol edilecek. Bu işlem uzun sürebilir. Devam?",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )

        if reply != QMessageBox.StandardButton.Yes:
            return

        device_ids = [d.id for d in self.devices]

        self.check_status_btn.setEnabled(False)
        self.check_status_btn.setText("Kontrol ediliyor...")

        self.status_worker = StatusCheckWorker(self.db, device_ids)
        self.status_worker.status_updated.connect(self.on_status_updated)
        self.status_worker.finished.connect(self.on_status_check_finished)
        self.status_worker.start()

    def on_status_updated(self, device_id: int, status: str):
        """Handle status update"""
        # Update table
        for row in range(self.devices_table.rowCount()):
            item = self.devices_table.item(row, 0)
            if item and item.data(Qt.ItemDataRole.UserRole) == device_id:
                status_item = self.devices_table.item(row, 6)  # Durum sütunu index 6
                status_item.setText(status.capitalize())
                if status == 'online':
                    status_item.setForeground(QColor("#4ec9b0"))
                elif status == 'offline':
                    status_item.setForeground(QColor("#f14c4c"))
                else:
                    status_item.setForeground(QColor("#ce9178"))
                break

    def on_status_check_finished(self):
        """Handle status check completion"""
        self.reconnect_btn.setEnabled(True)
        self.reconnect_btn.setText("Durumu Kontrol Et")
        self.check_status_btn.setEnabled(True)
        self.check_status_btn.setText("Durumları Kontrol Et")
        self.refresh_devices()

    def edit_device(self):
        """Edit selected device"""
        # TODO: Implement edit dialog
        QMessageBox.information(self, "Bilgi", "Düzenleme özelliği yakında eklenecek")

    def delete_device(self):
        """Delete selected device"""
        rows = self.devices_table.selectionModel().selectedRows()
        if not rows:
            return

        row = rows[0].row()
        device_id = self.devices_table.item(row, 0).data(Qt.ItemDataRole.UserRole)
        device = self.db.get_device(device_id)

        if not device:
            return

        reply = QMessageBox.question(
            self, "Silme Onayı",
            f"'{device.device_name}' (Port: {device.tunnel_port}) silinecek.\n\n"
            "Bu işlem:\n"
            "- VPS'ten SSH key'i kaldırır\n"
            "- Veritabanından cihazı siler\n\n"
            "Devam etmek istiyor musunuz?",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )

        if reply != QMessageBox.StandardButton.Yes:
            return

        provisioner = Provisioner(self.db)
        if provisioner.remove_device(device_id):
            QMessageBox.information(self, "Başarılı", "Cihaz silindi")
            self.refresh_devices()
            self.clear_details()
        else:
            QMessageBox.warning(self, "Hata", "Cihaz silinemedi")

    def export_csv(self):
        """Export devices to CSV"""
        if not self.devices:
            QMessageBox.warning(self, "Uyarı", "Dışa aktarılacak cihaz yok")
            return

        filename, _ = QFileDialog.getSaveFileName(
            self, "CSV Kaydet", "ec100_devices.csv", "CSV Files (*.csv)"
        )

        if not filename:
            return

        try:
            with open(filename, 'w', newline='', encoding='utf-8-sig') as f:
                writer = csv.writer(f)
                writer.writerow([
                    "ID", "Port", "Cihaz Adı", "EC100 Seri No", "MAC Adresi",
                    "NetZr Seri No", "Durum", "Son Görülme", "Oluşturma Tarihi", "Notlar"
                ])

                for device in self.devices:
                    writer.writerow([
                        device.id,
                        device.tunnel_port,
                        device.device_name,
                        device.serial_number or "",
                        device.mac_address or "",
                        device.location or "",
                        device.status,
                        device.last_seen.isoformat() if device.last_seen else "",
                        device.created_at.isoformat() if device.created_at else "",
                        device.notes or ""
                    ])

            QMessageBox.information(self, "Başarılı", f"CSV dosyası kaydedildi:\n{filename}")

        except Exception as e:
            QMessageBox.critical(self, "Hata", f"CSV kaydedilemedi: {e}")
