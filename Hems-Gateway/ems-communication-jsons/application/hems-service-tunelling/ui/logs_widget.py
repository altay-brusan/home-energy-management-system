"""
Logs Widget for EC100 Provisioning Tool
Log viewing and export interface
"""
import csv
import logging
from datetime import datetime

from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QGroupBox, QLabel,
    QPushButton, QTableWidget, QTableWidgetItem,
    QHeaderView, QComboBox, QFileDialog, QMessageBox, QSpinBox
)
from PyQt6.QtCore import Qt
from PyQt6.QtGui import QColor

from database.db_manager import DatabaseManager

logger = logging.getLogger(__name__)


class LogsWidget(QWidget):
    """Widget for viewing provision logs"""

    def __init__(self, db: DatabaseManager):
        super().__init__()
        self.db = db
        self.logs = []

        self.setup_ui()
        self.refresh_logs()

    def setup_ui(self):
        """Setup UI components"""
        layout = QVBoxLayout(self)

        # Filter group
        filter_group = QGroupBox("Filtre")
        filter_layout = QHBoxLayout(filter_group)

        filter_layout.addWidget(QLabel("Durum:"))
        self.status_filter = QComboBox()
        self.status_filter.addItems(["Tümü", "Başarılı", "Başarısız"])
        self.status_filter.currentTextChanged.connect(self.on_filter_changed)
        filter_layout.addWidget(self.status_filter)

        filter_layout.addWidget(QLabel("Limit:"))
        self.limit_input = QSpinBox()
        self.limit_input.setRange(10, 1000)
        self.limit_input.setValue(100)
        self.limit_input.setSingleStep(50)
        self.limit_input.valueChanged.connect(self.refresh_logs)
        filter_layout.addWidget(self.limit_input)

        filter_layout.addStretch()

        self.refresh_btn = QPushButton("Yenile")
        self.refresh_btn.clicked.connect(self.refresh_logs)
        filter_layout.addWidget(self.refresh_btn)

        self.export_btn = QPushButton("CSV Dışa Aktar")
        self.export_btn.clicked.connect(self.export_csv)
        filter_layout.addWidget(self.export_btn)

        self.clear_btn = QPushButton("Logları Temizle")
        self.clear_btn.setObjectName("dangerButton")
        self.clear_btn.clicked.connect(self.clear_logs)
        filter_layout.addWidget(self.clear_btn)

        layout.addWidget(filter_group)

        # Logs table
        logs_group = QGroupBox("Log Kayıtları")
        logs_layout = QVBoxLayout(logs_group)

        self.count_label = QLabel("Toplam: 0")
        logs_layout.addWidget(self.count_label)

        self.logs_table = QTableWidget()
        self.logs_table.setColumnCount(5)
        self.logs_table.setHorizontalHeaderLabels([
            "Tarih/Saat", "Cihaz", "İşlem", "Durum", "Mesaj"
        ])
        self.logs_table.horizontalHeader().setSectionResizeMode(0, QHeaderView.ResizeMode.Fixed)
        self.logs_table.horizontalHeader().setSectionResizeMode(1, QHeaderView.ResizeMode.Fixed)
        self.logs_table.horizontalHeader().setSectionResizeMode(2, QHeaderView.ResizeMode.Fixed)
        self.logs_table.horizontalHeader().setSectionResizeMode(3, QHeaderView.ResizeMode.Fixed)
        self.logs_table.horizontalHeader().setSectionResizeMode(4, QHeaderView.ResizeMode.Stretch)
        self.logs_table.setColumnWidth(0, 150)
        self.logs_table.setColumnWidth(1, 150)
        self.logs_table.setColumnWidth(2, 100)
        self.logs_table.setColumnWidth(3, 80)
        self.logs_table.setSelectionBehavior(QTableWidget.SelectionBehavior.SelectRows)
        self.logs_table.setAlternatingRowColors(True)
        logs_layout.addWidget(self.logs_table)

        layout.addWidget(logs_group)

    def refresh_logs(self):
        """Refresh logs from database"""
        limit = self.limit_input.value()
        self.logs = self.db.get_logs_with_device_info(limit)
        self.apply_filter()

    def on_filter_changed(self, text: str):
        """Handle filter change"""
        self.apply_filter()

    def apply_filter(self):
        """Apply status filter"""
        status_filter = self.status_filter.currentText()

        if status_filter == "Tümü":
            filtered = self.logs
        elif status_filter == "Başarılı":
            filtered = [l for l in self.logs if l.get('status') == 'success']
        else:
            filtered = [l for l in self.logs if l.get('status') == 'failed']

        self.populate_table(filtered)

    def populate_table(self, logs):
        """Populate table with logs"""
        self.logs_table.setRowCount(len(logs))
        self.count_label.setText(f"Toplam: {len(logs)}")

        for row, log in enumerate(logs):
            # Date/time
            created_at = log.get('created_at', '')
            if created_at:
                try:
                    dt = datetime.fromisoformat(created_at)
                    date_str = dt.strftime('%Y-%m-%d %H:%M:%S')
                except ValueError:
                    date_str = created_at
            else:
                date_str = '-'

            date_item = QTableWidgetItem(date_str)
            date_item.setFlags(date_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.logs_table.setItem(row, 0, date_item)

            # Device name
            device_name = log.get('device_name', '-') or '-'
            device_item = QTableWidgetItem(device_name)
            device_item.setFlags(device_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.logs_table.setItem(row, 1, device_item)

            # Action
            action = log.get('action', '-')
            action_map = {
                'provision': 'Kurulum',
                'delete': 'Silme',
                'status_check': 'Durum Kontrol'
            }
            action_item = QTableWidgetItem(action_map.get(action, action))
            action_item.setFlags(action_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self.logs_table.setItem(row, 2, action_item)

            # Status
            status = log.get('status', '-')
            status_map = {
                'success': 'Başarılı',
                'failed': 'Başarısız'
            }
            status_item = QTableWidgetItem(status_map.get(status, status))
            status_item.setFlags(status_item.flags() & ~Qt.ItemFlag.ItemIsEditable)

            if status == 'success':
                status_item.setForeground(QColor("#4ec9b0"))
            elif status == 'failed':
                status_item.setForeground(QColor("#f14c4c"))

            self.logs_table.setItem(row, 3, status_item)

            # Message
            message = log.get('message', '-') or '-'
            message_item = QTableWidgetItem(message)
            message_item.setFlags(message_item.flags() & ~Qt.ItemFlag.ItemIsEditable)
            message_item.setToolTip(message)
            self.logs_table.setItem(row, 4, message_item)

    def export_csv(self):
        """Export logs to CSV"""
        if not self.logs:
            QMessageBox.warning(self, "Uyarı", "Dışa aktarılacak log yok")
            return

        filename, _ = QFileDialog.getSaveFileName(
            self, "CSV Kaydet", "ec100_logs.csv", "CSV Files (*.csv)"
        )

        if not filename:
            return

        try:
            with open(filename, 'w', newline='', encoding='utf-8-sig') as f:
                writer = csv.writer(f)
                writer.writerow(["Tarih/Saat", "Cihaz", "İşlem", "Durum", "Mesaj"])

                for log in self.logs:
                    writer.writerow([
                        log.get('created_at', ''),
                        log.get('device_name', ''),
                        log.get('action', ''),
                        log.get('status', ''),
                        log.get('message', '')
                    ])

            QMessageBox.information(self, "Başarılı", f"CSV dosyası kaydedildi:\n{filename}")

        except Exception as e:
            QMessageBox.critical(self, "Hata", f"CSV kaydedilemedi: {e}")

    def clear_logs(self):
        """Clear all logs"""
        reply = QMessageBox.question(
            self, "Onay",
            "Tüm log kayıtları silinecek. Devam?",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        )

        if reply != QMessageBox.StandardButton.Yes:
            return

        self.db.clear_logs()
        self.refresh_logs()
        QMessageBox.information(self, "Başarılı", "Loglar temizlendi")
