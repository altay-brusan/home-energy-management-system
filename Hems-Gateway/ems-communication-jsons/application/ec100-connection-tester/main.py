"""
EC100 Connection Tester Application
Simple desktop application for testing EC100 device connections via ThingsBoard RPC
"""

import tkinter as tk
from tkinter import ttk, scrolledtext
import threading
import json
import os
from datetime import datetime
from api_client import ThingsBoardClient


class ConnectionTesterApp:
    """Main application for EC100 connection testing"""

    # Colors
    COLOR_SUCCESS = "#4CAF50"  # Green
    COLOR_FAIL = "#F44336"     # Red
    COLOR_WAITING = "#FFC107"  # Yellow/Amber
    COLOR_NEUTRAL = "#E0E0E0"  # Gray
    COLOR_BG = "#FAFAFA"       # Light background
    COLOR_TEXT_LIGHT = "#FFFFFF"
    COLOR_TEXT_DARK = "#212121"

    def __init__(self, root):
        self.root = root
        self.root.title("EC100 Baglanti Testi")
        self.root.geometry("600x500")
        self.root.minsize(500, 400)
        self.root.configure(bg=self.COLOR_BG)

        # Load config
        self.config = self.load_config()
        self.client = None

        # Build UI
        self._build_ui()

    def load_config(self) -> dict:
        """Load configuration from config.json"""
        config_path = os.path.join(os.path.dirname(__file__), 'config.json')
        default_config = {
            "thingsboard_url": "https://thingsboard-stg.oklidis.com.tr",
            "username": "tenant@thingsboard.org",
            "password": "",
            "rpc_method": "getInverterInfo",
            "rpc_timeout": 30
        }

        try:
            if os.path.exists(config_path):
                with open(config_path, 'r', encoding='utf-8') as f:
                    return json.load(f)
        except Exception:
            pass

        return default_config

    def _build_ui(self):
        """Build the user interface"""
        # Main container with padding
        main_frame = tk.Frame(self.root, bg=self.COLOR_BG, padx=20, pady=20)
        main_frame.pack(fill=tk.BOTH, expand=True)

        # Title
        title_label = tk.Label(
            main_frame,
            text="EC100 BAGLANTI TESTI",
            font=("Segoe UI", 24, "bold"),
            bg=self.COLOR_BG,
            fg=self.COLOR_TEXT_DARK
        )
        title_label.pack(pady=(0, 20))

        # Serial number input section
        input_frame = tk.Frame(main_frame, bg=self.COLOR_BG)
        input_frame.pack(fill=tk.X, pady=(0, 15))

        serial_label = tk.Label(
            input_frame,
            text="Cihaz Seri No:",
            font=("Segoe UI", 14),
            bg=self.COLOR_BG,
            fg=self.COLOR_TEXT_DARK
        )
        serial_label.pack(anchor=tk.W)

        self.serial_entry = tk.Entry(
            input_frame,
            font=("Segoe UI", 16),
            justify=tk.CENTER
        )
        self.serial_entry.pack(fill=tk.X, pady=(5, 0), ipady=8)
        self.serial_entry.bind('<Return>', lambda e: self.run_test())

        # Test button
        self.test_button = tk.Button(
            main_frame,
            text="TEST ET",
            font=("Segoe UI", 16, "bold"),
            bg="#2196F3",
            fg=self.COLOR_TEXT_LIGHT,
            activebackground="#1976D2",
            activeforeground=self.COLOR_TEXT_LIGHT,
            cursor="hand2",
            command=self.run_test,
            relief=tk.FLAT,
            padx=40,
            pady=12
        )
        self.test_button.pack(pady=15)

        # Result area
        self.result_frame = tk.Frame(
            main_frame,
            bg=self.COLOR_NEUTRAL,
            height=100
        )
        self.result_frame.pack(fill=tk.X, pady=(10, 15))
        self.result_frame.pack_propagate(False)

        self.result_label = tk.Label(
            self.result_frame,
            text="Test bekleniyor...",
            font=("Segoe UI", 20, "bold"),
            bg=self.COLOR_NEUTRAL,
            fg=self.COLOR_TEXT_DARK
        )
        self.result_label.pack(expand=True)

        # Log section
        log_label = tk.Label(
            main_frame,
            text="Log:",
            font=("Segoe UI", 10),
            bg=self.COLOR_BG,
            fg=self.COLOR_TEXT_DARK,
            anchor=tk.W
        )
        log_label.pack(fill=tk.X)

        self.log_text = scrolledtext.ScrolledText(
            main_frame,
            font=("Consolas", 9),
            height=8,
            wrap=tk.WORD,
            state=tk.DISABLED
        )
        self.log_text.pack(fill=tk.BOTH, expand=True, pady=(5, 0))

    def log(self, message: str, level: str = "INFO"):
        """Add message to log area"""
        timestamp = datetime.now().strftime("%H:%M:%S")
        formatted = f"[{timestamp}] [{level}] {message}\n"

        self.log_text.config(state=tk.NORMAL)
        self.log_text.insert(tk.END, formatted)
        self.log_text.see(tk.END)
        self.log_text.config(state=tk.DISABLED)

    def set_result(self, status: str, message: str = ""):
        """Update result display"""
        if status == "SUCCESS":
            self.result_frame.configure(bg=self.COLOR_SUCCESS)
            self.result_label.configure(
                text="BASARILI",
                bg=self.COLOR_SUCCESS,
                fg=self.COLOR_TEXT_LIGHT
            )
        elif status == "FAIL":
            self.result_frame.configure(bg=self.COLOR_FAIL)
            self.result_label.configure(
                text="BASARISIZ",
                bg=self.COLOR_FAIL,
                fg=self.COLOR_TEXT_LIGHT
            )
        elif status == "WAITING":
            self.result_frame.configure(bg=self.COLOR_WAITING)
            self.result_label.configure(
                text="TEST EDILIYOR...",
                bg=self.COLOR_WAITING,
                fg=self.COLOR_TEXT_DARK
            )
        else:
            self.result_frame.configure(bg=self.COLOR_NEUTRAL)
            self.result_label.configure(
                text="Test bekleniyor...",
                bg=self.COLOR_NEUTRAL,
                fg=self.COLOR_TEXT_DARK
            )

    def run_test(self):
        """Start the connection test"""
        serial_number = self.serial_entry.get().strip()

        if not serial_number:
            self.log("Lutfen seri numarasi girin!", "ERROR")
            return

        # Disable button during test
        self.test_button.config(state=tk.DISABLED)
        self.set_result("WAITING")
        self.log(f"Test baslatiliyor: {serial_number}")

        # Run test in background thread
        thread = threading.Thread(target=self._execute_test, args=(serial_number,), daemon=True)
        thread.start()

    def _execute_test(self, serial_number: str):
        """Execute the actual test (runs in background thread)"""
        try:
            # Step 1: Login
            self.root.after(0, lambda: self.log("ThingsBoard'a baglaniliyor..."))

            self.client = ThingsBoardClient(self.config['thingsboard_url'])
            self.client.login(self.config['username'], self.config['password'])

            self.root.after(0, lambda: self.log("Giris basarili"))

            # Step 2: Find device
            self.root.after(0, lambda: self.log(f"Cihaz araniyor: {serial_number}"))

            device_id = self.client.find_device_by_name(serial_number)

            if not device_id:
                self.root.after(0, lambda: self._test_failed(f"Cihaz bulunamadi: {serial_number}"))
                return

            self.root.after(0, lambda: self.log(f"Cihaz bulundu: {device_id}"))

            # Step 3: RPC call
            rpc_method = self.config.get('rpc_method', 'getInverterInfo')
            rpc_timeout = self.config.get('rpc_timeout', 30)

            self.root.after(0, lambda: self.log(f"RPC cagrisi yapiliyor: {rpc_method}"))

            response = self.client.rpc_call(device_id, rpc_method, {}, timeout=rpc_timeout)

            # Log the response
            response_str = json.dumps(response, indent=2, ensure_ascii=False)
            self.root.after(0, lambda: self.log(f"RPC Cevabi:\n{response_str}"))

            # Step 4: Check success (kind == "InverterInfoState")
            if response.get("kind") == "InverterInfoState":
                self.root.after(0, lambda: self._test_success(response))
            else:
                self.root.after(0, lambda: self._test_failed("Gecersiz cevap - kind alani bulunamadi"))

        except Exception as e:
            error_msg = str(e)
            self.root.after(0, lambda: self._test_failed(error_msg))

    def _test_success(self, response: dict):
        """Handle successful test"""
        self.set_result("SUCCESS")
        self.log("TEST BASARILI!", "SUCCESS")
        self.test_button.config(state=tk.NORMAL)

    def _test_failed(self, error: str):
        """Handle failed test"""
        self.set_result("FAIL")
        self.log(f"TEST BASARISIZ: {error}", "ERROR")
        self.test_button.config(state=tk.NORMAL)


def main():
    root = tk.Tk()
    app = ConnectionTesterApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
