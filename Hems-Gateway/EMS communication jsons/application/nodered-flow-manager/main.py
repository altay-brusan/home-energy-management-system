"""
Node-RED Flow Manager Desktop Application
Modern UI for managing Node-RED flows on EC100 devices via ThingsBoard
"""

import tkinter as tk
from tkinter import ttk, filedialog, messagebox
import json
import os
import threading
from datetime import datetime
from typing import Optional, Dict, Any, List

try:
    from ttkthemes import ThemedTk
    USE_THEMED = True
except ImportError:
    USE_THEMED = False

from api_client import ThingsBoardClient


class ModernButton(tk.Canvas):
    """Custom modern-looking button with hover effects"""

    def __init__(self, parent, text, command, icon="", bg_color="#4CAF50",
                 hover_color="#45a049", fg_color="white", **kwargs):
        super().__init__(parent, highlightthickness=0, **kwargs)

        self.command = command
        self.bg_color = bg_color
        self.hover_color = hover_color
        self.fg_color = fg_color
        self.text = f"{icon} {text}".strip()

        self.config(width=kwargs.get('width', 120), height=kwargs.get('height', 35))
        self._draw_button(self.bg_color)

        self.bind("<Enter>", lambda e: self._draw_button(self.hover_color))
        self.bind("<Leave>", lambda e: self._draw_button(self.bg_color))
        self.bind("<Button-1>", lambda e: self._on_click())

    def _draw_button(self, color):
        self.delete("all")
        width = self.winfo_reqwidth()
        height = self.winfo_reqheight()

        # Rounded rectangle
        radius = 6
        self.create_arc(0, 0, radius*2, radius*2, start=90, extent=90,
                       fill=color, outline=color)
        self.create_arc(width-radius*2, 0, width, radius*2, start=0, extent=90,
                       fill=color, outline=color)
        self.create_arc(0, height-radius*2, radius*2, height, start=180, extent=90,
                       fill=color, outline=color)
        self.create_arc(width-radius*2, height-radius*2, width, height, start=270, extent=90,
                       fill=color, outline=color)
        self.create_rectangle(radius, 0, width-radius, height, fill=color, outline=color)
        self.create_rectangle(0, radius, width, height-radius, fill=color, outline=color)

        self.create_text(width//2, height//2, text=self.text, fill=self.fg_color,
                        font=("Segoe UI", 9, "bold"))

    def _on_click(self):
        if self.command:
            self.command()


class BatchAddProgressDialog(tk.Toplevel):
    """Progress dialog for batch adding flows"""

    def __init__(self, parent, total_files):
        super().__init__(parent)
        self.title("Adding Multiple Flows")
        self.geometry("500x250")
        self.resizable(False, False)
        self.transient(parent)
        self.grab_set()

        # Center on parent
        self.update_idletasks()
        x = parent.winfo_x() + (parent.winfo_width() // 2) - (self.winfo_width() // 2)
        y = parent.winfo_y() + (parent.winfo_height() // 2) - (self.winfo_height() // 2)
        self.geometry(f"+{x}+{y}")

        self.total_files = total_files
        self.current_file = 0
        self.success_count = 0
        self.error_count = 0
        self.errors = []

        self._build_ui()

    def _build_ui(self):
        """Build progress dialog UI"""
        main_frame = tk.Frame(self, bg='white', padx=20, pady=20)
        main_frame.pack(fill=tk.BOTH, expand=True)

        # Title
        tk.Label(main_frame, text="Adding Flows to Device",
                font=("Segoe UI", 12, "bold"), bg='white').pack(pady=(0, 10))

        # Current file label
        self.current_label = tk.Label(main_frame, text="Preparing...",
                                     font=("Segoe UI", 9), bg='white', fg='#555')
        self.current_label.pack(pady=(0, 10))

        # Progress bar
        self.progress_var = tk.DoubleVar()
        self.progress_bar = ttk.Progressbar(main_frame, variable=self.progress_var,
                                           maximum=100, length=400, mode='determinate')
        self.progress_bar.pack(pady=(0, 10))

        # Progress text
        self.progress_text = tk.Label(main_frame, text="0 / 0 files processed",
                                     font=("Segoe UI", 9), bg='white')
        self.progress_text.pack(pady=(0, 10))

        # Status text
        self.status_text = tk.Label(main_frame, text="✓ 0 successful  ✗ 0 failed",
                                   font=("Segoe UI", 9), bg='white', fg='#555')
        self.status_text.pack(pady=(0, 15))

        # Close button (initially disabled)
        self.close_button = tk.Button(main_frame, text="Close", command=self.destroy,
                                     state=tk.DISABLED, font=("Segoe UI", 9),
                                     bg='#2196F3', fg='white', padx=20, pady=5,
                                     relief=tk.FLAT, cursor='hand2')
        self.close_button.pack()

    def update_progress(self, current, filename, success=None, error=None):
        """Update progress display"""
        self.current_file = current

        if success:
            self.success_count += 1
        elif error:
            self.error_count += 1
            self.errors.append((filename, error))

        # Update labels
        progress_percent = (current / self.total_files) * 100
        self.progress_var.set(progress_percent)

        if current < self.total_files:
            self.current_label.config(text=f"Adding flow {current + 1}/{self.total_files}: {filename}")
        else:
            self.current_label.config(text="Completed!", fg='#4CAF50')

        self.progress_text.config(text=f"{current} / {self.total_files} files processed")
        self.status_text.config(text=f"✓ {self.success_count} successful  ✗ {self.error_count} failed")

        # Enable close button when done
        if current >= self.total_files:
            self.close_button.config(state=tk.NORMAL)

        self.update_idletasks()

    def get_summary(self):
        """Get summary of batch operation"""
        return {
            'total': self.total_files,
            'success': self.success_count,
            'errors': self.error_count,
            'error_details': self.errors
        }


class FlowManagerApp:
    """Main application class for Node-RED Flow Manager"""

    # Protected flow that cannot be deleted
    PROTECTED_FLOW_ID = "rpc_flow_manager_tab"

    # Color scheme
    COLORS = {
        'primary': '#2196F3',
        'success': '#4CAF50',
        'danger': '#f44336',
        'warning': '#FF9800',
        'info': '#00BCD4',
        'dark': '#263238',
        'light': '#ECEFF1',
        'bg': '#FAFAFA',
        'white': '#FFFFFF'
    }

    def __init__(self):
        """Initialize the application"""
        # Create main window
        if USE_THEMED:
            self.root = ThemedTk(theme="arc")
        else:
            self.root = tk.Tk()

        self.root.title("🔧 Node-RED Flow Manager v1.0")
        self.root.geometry("900x700")
        self.root.minsize(800, 600)
        self.root.configure(bg=self.COLORS['bg'])

        # Initialize API client
        self.client: Optional[ThingsBoardClient] = None
        self.current_device_id: Optional[str] = None
        self.devices_map: Dict[str, str] = {}  # name -> id mapping
        self.flows_data: List[Dict[str, Any]] = []

        # Load saved settings
        self.config_file = "config.json"
        self.settings = self.load_settings()

        # Build UI
        self._build_ui()

        # Auto-fill settings if available
        if self.settings:
            self.url_var.set(self.settings.get('thingsboard_url', ''))
            self.username_var.set(self.settings.get('username', ''))

        # Log startup
        self.log("Application started", "INFO")
        self.log("Ready to connect to ThingsBoard", "INFO")

    def _build_ui(self):
        """Build the user interface"""
        # Main container with padding
        main_frame = tk.Frame(self.root, bg=self.COLORS['bg'])
        main_frame.pack(fill=tk.BOTH, expand=True, padx=15, pady=15)

        # Connection settings section
        self._build_connection_section(main_frame)

        # Separator
        ttk.Separator(main_frame, orient='horizontal').pack(fill=tk.X, pady=10)

        # Device flows section
        self._build_flows_section(main_frame)

        # Action buttons
        self._build_action_buttons(main_frame)

        # Separator
        ttk.Separator(main_frame, orient='horizontal').pack(fill=tk.X, pady=10)

        # Log section
        self._build_log_section(main_frame)

    def _build_connection_section(self, parent):
        """Build connection settings UI section"""
        # Section frame
        section = tk.LabelFrame(parent, text=" Connection Settings ",
                               font=("Segoe UI", 10, "bold"),
                               bg=self.COLORS['white'], fg=self.COLORS['dark'],
                               relief=tk.FLAT, borderwidth=2)
        section.pack(fill=tk.X, pady=(0, 10))

        # Inner padding
        inner = tk.Frame(section, bg=self.COLORS['white'])
        inner.pack(fill=tk.BOTH, padx=15, pady=10)

        # Variables
        self.url_var = tk.StringVar(value="https://thingsboard-dev.oklidis.com.tr")
        self.username_var = tk.StringVar()
        self.password_var = tk.StringVar()
        self.device_var = tk.StringVar()
        self.status_var = tk.StringVar(value="⚪ Disconnected")

        # Grid layout
        row = 0

        # ThingsBoard URL
        tk.Label(inner, text="ThingsBoard URL:", bg=self.COLORS['white'],
                font=("Segoe UI", 9)).grid(row=row, column=0, sticky='w', pady=5)
        tk.Entry(inner, textvariable=self.url_var, width=50,
                font=("Segoe UI", 9)).grid(row=row, column=1, sticky='ew', pady=5, padx=(10, 0))
        row += 1

        # Username
        tk.Label(inner, text="Username:", bg=self.COLORS['white'],
                font=("Segoe UI", 9)).grid(row=row, column=0, sticky='w', pady=5)
        tk.Entry(inner, textvariable=self.username_var, width=50,
                font=("Segoe UI", 9)).grid(row=row, column=1, sticky='ew', pady=5, padx=(10, 0))
        row += 1

        # Password
        tk.Label(inner, text="Password:", bg=self.COLORS['white'],
                font=("Segoe UI", 9)).grid(row=row, column=0, sticky='w', pady=5)
        tk.Entry(inner, textvariable=self.password_var, width=50, show="*",
                font=("Segoe UI", 9)).grid(row=row, column=1, sticky='ew', pady=5, padx=(10, 0))
        row += 1

        # Device selection
        tk.Label(inner, text="Device:", bg=self.COLORS['white'],
                font=("Segoe UI", 9)).grid(row=row, column=0, sticky='w', pady=5)
        self.device_combo = ttk.Combobox(inner, textvariable=self.device_var,
                                        state='readonly', width=47,
                                        font=("Segoe UI", 9))
        self.device_combo.grid(row=row, column=1, sticky='ew', pady=5, padx=(10, 0))
        self.device_combo.bind('<<ComboboxSelected>>', self._on_device_selected)
        row += 1

        # Buttons and status
        button_frame = tk.Frame(inner, bg=self.COLORS['white'])
        button_frame.grid(row=row, column=0, columnspan=2, sticky='ew', pady=(10, 0))

        ModernButton(button_frame, "💾 Save Settings", self.save_settings,
                    bg_color=self.COLORS['info'], hover_color="#0097A7",
                    width=130, height=35).pack(side=tk.LEFT, padx=(0, 5))

        ModernButton(button_frame, "🔗 Connect", self.connect,
                    bg_color=self.COLORS['success'], hover_color="#388E3C",
                    width=130, height=35).pack(side=tk.LEFT)

        self.status_label = tk.Label(button_frame, textvariable=self.status_var,
                                     font=("Segoe UI", 9, "bold"),
                                     bg=self.COLORS['white'], fg=self.COLORS['dark'])
        self.status_label.pack(side=tk.RIGHT)

        # Configure grid weights
        inner.columnconfigure(1, weight=1)

    def _build_flows_section(self, parent):
        """Build device flows list UI section"""
        # Section frame
        section = tk.LabelFrame(parent, text=" Device Flows ",
                               font=("Segoe UI", 10, "bold"),
                               bg=self.COLORS['white'], fg=self.COLORS['dark'],
                               relief=tk.FLAT, borderwidth=2)
        section.pack(fill=tk.BOTH, expand=True, pady=(0, 10))

        # Inner padding
        inner = tk.Frame(section, bg=self.COLORS['white'])
        inner.pack(fill=tk.BOTH, expand=True, padx=15, pady=10)

        # Create Treeview
        columns = ('id', 'label', 'status')
        self.tree = ttk.Treeview(inner, columns=columns, show='headings',
                                height=10, selectmode='browse')

        # Define columns
        self.tree.heading('id', text='Flow ID')
        self.tree.heading('label', text='Label')
        self.tree.heading('status', text='Status')

        self.tree.column('id', width=250)
        self.tree.column('label', width=400)
        self.tree.column('status', width=100)

        # Scrollbar
        scrollbar = ttk.Scrollbar(inner, orient=tk.VERTICAL, command=self.tree.yview)
        self.tree.configure(yscrollcommand=scrollbar.set)

        # Pack
        self.tree.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)

        # Style for tree
        style = ttk.Style()
        style.configure("Treeview", font=("Segoe UI", 9), rowheight=25)
        style.configure("Treeview.Heading", font=("Segoe UI", 9, "bold"))

    def _build_action_buttons(self, parent):
        """Build action buttons section"""
        button_frame = tk.Frame(parent, bg=self.COLORS['bg'])
        button_frame.pack(fill=tk.X, pady=(0, 10))

        ModernButton(button_frame, "🔄 Refresh", self.refresh_flows,
                    bg_color=self.COLORS['primary'], hover_color="#1976D2",
                    width=130, height=35).pack(side=tk.LEFT, padx=(0, 5))

        ModernButton(button_frame, "➕ Add Flow", self.add_flow,
                    bg_color=self.COLORS['success'], hover_color="#388E3C",
                    width=130, height=35).pack(side=tk.LEFT, padx=(0, 5))

        ModernButton(button_frame, "📁 Add Multiple", self.add_multiple_flows,
                    bg_color="#9C27B0", hover_color="#7B1FA2",
                    width=150, height=35).pack(side=tk.LEFT, padx=(0, 5))

        ModernButton(button_frame, "🗑️ Remove Selected", self.remove_selected,
                    bg_color=self.COLORS['danger'], hover_color="#D32F2F",
                    width=150, height=35).pack(side=tk.LEFT, padx=(0, 5))

        ModernButton(button_frame, "💾 Backup All", self.backup_all,
                    bg_color=self.COLORS['warning'], hover_color="#F57C00",
                    width=130, height=35).pack(side=tk.LEFT)

    def _build_log_section(self, parent):
        """Build log display section"""
        # Section frame
        section = tk.LabelFrame(parent, text=" Activity Log ",
                               font=("Segoe UI", 10, "bold"),
                               bg=self.COLORS['white'], fg=self.COLORS['dark'],
                               relief=tk.FLAT, borderwidth=2)
        section.pack(fill=tk.BOTH, expand=True)

        # Inner padding
        inner = tk.Frame(section, bg=self.COLORS['white'])
        inner.pack(fill=tk.BOTH, expand=True, padx=15, pady=10)

        # Text widget with scrollbar
        self.log_text = tk.Text(inner, height=8, wrap=tk.WORD,
                               font=("Consolas", 9), bg="#263238", fg="#00FF00",
                               relief=tk.FLAT)
        log_scrollbar = ttk.Scrollbar(inner, orient=tk.VERTICAL,
                                     command=self.log_text.yview)
        self.log_text.configure(yscrollcommand=log_scrollbar.set)

        self.log_text.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        log_scrollbar.pack(side=tk.RIGHT, fill=tk.Y)

        # Configure text tags for colors
        self.log_text.tag_config("INFO", foreground="#00FF00")
        self.log_text.tag_config("SUCCESS", foreground="#4CAF50")
        self.log_text.tag_config("WARNING", foreground="#FFA726")
        self.log_text.tag_config("ERROR", foreground="#EF5350")

    def load_settings(self) -> Dict[str, Any]:
        """Load saved settings from config file"""
        if os.path.exists(self.config_file):
            try:
                with open(self.config_file, 'r') as f:
                    return json.load(f)
            except Exception as e:
                print(f"Failed to load settings: {e}")
        return {}

    def save_settings(self):
        """Save current settings to config file"""
        try:
            settings = {
                'thingsboard_url': self.url_var.get(),
                'username': self.username_var.get(),
                'last_device_id': self.current_device_id
            }

            with open(self.config_file, 'w') as f:
                json.dump(settings, f, indent=2)

            self.log("Settings saved successfully", "SUCCESS")
            messagebox.showinfo("Success", "Settings saved successfully!")
        except Exception as e:
            self.log(f"Failed to save settings: {str(e)}", "ERROR")
            messagebox.showerror("Error", f"Failed to save settings:\n{str(e)}")

    def log(self, message: str, level: str = "INFO"):
        """Add timestamped message to log"""
        timestamp = datetime.now().strftime("%H:%M:%S")
        log_message = f"[{timestamp}] {message}\n"

        self.log_text.insert(tk.END, log_message, level)
        self.log_text.see(tk.END)

    def update_status(self, status: str, color: str):
        """Update connection status indicator"""
        self.status_var.set(status)
        self.status_label.config(fg=color)

    def connect(self):
        """Connect to ThingsBoard and load devices"""
        url = self.url_var.get().strip()
        username = self.username_var.get().strip()
        password = self.password_var.get().strip()

        if not url or not username or not password:
            messagebox.showwarning("Warning", "Please fill in all connection fields")
            return

        def _connect_thread():
            try:
                self.log("Connecting to ThingsBoard...", "INFO")
                self.update_status("🟡 Connecting...", self.COLORS['warning'])

                # Initialize client
                self.client = ThingsBoardClient(url)

                # Login
                if not self.client.login(username, password):
                    raise Exception("Login failed - invalid credentials")

                self.log("Successfully authenticated", "SUCCESS")

                # Get devices
                self.log("Loading devices...", "INFO")
                devices = self.client.get_devices()

                if not devices:
                    raise Exception("No devices found")

                # Populate device dropdown
                self.devices_map = {dev['name']: dev['id']['id'] for dev in devices}
                device_names = list(self.devices_map.keys())

                self.device_combo['values'] = device_names

                # Select last used device if available
                if self.settings.get('last_device_id'):
                    for name, dev_id in self.devices_map.items():
                        if dev_id == self.settings['last_device_id']:
                            self.device_var.set(name)
                            break
                elif device_names:
                    self.device_var.set(device_names[0])

                self.log(f"Loaded {len(devices)} device(s)", "SUCCESS")
                self.update_status("🟢 Connected", self.COLORS['success'])

                # Auto-load flows if device selected
                if self.device_var.get():
                    self._on_device_selected(None)

            except Exception as e:
                self.log(f"Connection failed: {str(e)}", "ERROR")
                self.update_status("🔴 Connection Failed", self.COLORS['danger'])
                messagebox.showerror("Connection Error", str(e))

        # Run in background thread
        threading.Thread(target=_connect_thread, daemon=True).start()

    def _on_device_selected(self, event):
        """Handle device selection"""
        device_name = self.device_var.get()
        if device_name and device_name in self.devices_map:
            self.current_device_id = self.devices_map[device_name]
            self.log(f"Selected device: {device_name}", "INFO")
            self.refresh_flows()

    def refresh_flows(self):
        """Refresh flow list from selected device"""
        if not self.client or not self.current_device_id:
            messagebox.showwarning("Warning", "Please connect and select a device first")
            return

        def _refresh_thread():
            try:
                self.log("Loading flows from device...", "INFO")

                # Get flows via RPC
                response = self.client.get_flows(self.current_device_id)

                if not response.get('success'):
                    raise Exception(response.get('message', 'Failed to get flows'))

                flows = response.get('flows', [])

                # Filter only tab-type flows
                self.flows_data = [f for f in flows if f.get('type') == 'tab']

                # Update treeview
                self.tree.delete(*self.tree.get_children())

                for flow in self.flows_data:
                    flow_id = flow.get('id', '')
                    label = flow.get('label', 'Unnamed Flow')

                    # Check if protected
                    status = "🔒 Protected" if flow_id == self.PROTECTED_FLOW_ID else ""

                    self.tree.insert('', tk.END, values=(flow_id, label, status))

                self.log(f"Loaded {len(self.flows_data)} flow(s)", "SUCCESS")

            except Exception as e:
                self.log(f"Failed to load flows: {str(e)}", "ERROR")
                messagebox.showerror("Error", f"Failed to load flows:\n{str(e)}")

        threading.Thread(target=_refresh_thread, daemon=True).start()

    def add_flow(self):
        """Add a new flow from JSON file"""
        if not self.client or not self.current_device_id:
            messagebox.showwarning("Warning", "Please connect and select a device first")
            return

        # Open file dialog
        file_path = filedialog.askopenfilename(
            title="Select Flow JSON File",
            filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
        )

        if not file_path:
            return

        def _add_thread():
            try:
                self.log(f"Parsing flow file: {os.path.basename(file_path)}", "INFO")

                # Parse flow file
                formatted_flow = self.client.parse_flow_file(file_path)

                self.log(f"Adding flow: {formatted_flow.get('label')}", "INFO")

                # Add flow via RPC
                response = self.client.add_flow(self.current_device_id, formatted_flow)

                if not response.get('success'):
                    raise Exception(response.get('message', 'Failed to add flow'))

                self.log(f"Flow added successfully: {response.get('flowId')}", "SUCCESS")
                messagebox.showinfo("Success", "Flow added successfully!")

                # Refresh list
                self.refresh_flows()

            except Exception as e:
                self.log(f"Failed to add flow: {str(e)}", "ERROR")
                messagebox.showerror("Error", f"Failed to add flow:\n{str(e)}")

        threading.Thread(target=_add_thread, daemon=True).start()

    def add_multiple_flows(self):
        """Add multiple flows from files or folder"""
        if not self.client or not self.current_device_id:
            messagebox.showwarning("Warning", "Please connect and select a device first")
            return

        # Show selection dialog
        choice_dialog = tk.Toplevel(self.root)
        choice_dialog.title("Add Multiple Flows")
        choice_dialog.geometry("400x200")
        choice_dialog.resizable(False, False)
        choice_dialog.transient(self.root)
        choice_dialog.grab_set()

        # Center on parent
        choice_dialog.update_idletasks()
        x = self.root.winfo_x() + (self.root.winfo_width() // 2) - (choice_dialog.winfo_width() // 2)
        y = self.root.winfo_y() + (self.root.winfo_height() // 2) - (choice_dialog.winfo_height() // 2)
        choice_dialog.geometry(f"+{x}+{y}")

        selected_files = []

        def select_files():
            files = filedialog.askopenfilenames(
                title="Select Multiple Flow JSON Files",
                filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
            )
            if files:
                selected_files.extend(files)
                choice_dialog.destroy()
                self._process_batch_add(selected_files)

        def select_folder():
            folder = filedialog.askdirectory(title="Select Folder Containing Flow Files")
            if folder:
                # Find all .json files in the folder
                json_files = []
                for file in os.listdir(folder):
                    if file.endswith('.json'):
                        json_files.append(os.path.join(folder, file))

                if not json_files:
                    messagebox.showwarning("No Files", "No JSON files found in selected folder")
                    choice_dialog.destroy()
                    return

                selected_files.extend(json_files)
                choice_dialog.destroy()
                self._process_batch_add(selected_files)

        # Dialog content
        main_frame = tk.Frame(choice_dialog, bg='white', padx=30, pady=30)
        main_frame.pack(fill=tk.BOTH, expand=True)

        tk.Label(main_frame, text="How would you like to add flows?",
                font=("Segoe UI", 11, "bold"), bg='white').pack(pady=(0, 20))

        tk.Label(main_frame, text="Choose one of the following options:",
                font=("Segoe UI", 9), bg='white', fg='#555').pack(pady=(0, 15))

        # Buttons
        btn_frame = tk.Frame(main_frame, bg='white')
        btn_frame.pack()

        tk.Button(btn_frame, text="📄 Select Files",
                 command=select_files, font=("Segoe UI", 9, "bold"),
                 bg='#2196F3', fg='white', padx=20, pady=10,
                 relief=tk.FLAT, cursor='hand2', width=15).pack(side=tk.LEFT, padx=5)

        tk.Button(btn_frame, text="📁 Select Folder",
                 command=select_folder, font=("Segoe UI", 9, "bold"),
                 bg='#9C27B0', fg='white', padx=20, pady=10,
                 relief=tk.FLAT, cursor='hand2', width=15).pack(side=tk.LEFT, padx=5)

    def _process_batch_add(self, file_paths: List[str]):
        """Process batch adding of flows"""
        if not file_paths:
            return

        self.log(f"Starting batch add for {len(file_paths)} file(s)", "INFO")

        # Create progress dialog
        progress_dialog = BatchAddProgressDialog(self.root, len(file_paths))

        def _batch_add_thread():
            for idx, file_path in enumerate(file_paths):
                filename = os.path.basename(file_path)

                try:
                    # Update progress - starting
                    self.root.after(0, lambda i=idx, fn=filename:
                                   progress_dialog.update_progress(i, fn))

                    self.log(f"Adding flow {idx + 1}/{len(file_paths)}: {filename}", "INFO")

                    # Parse flow file
                    formatted_flow = self.client.parse_flow_file(file_path)
                    flow_label = formatted_flow.get('label', 'Unnamed')

                    # Add flow via RPC
                    response = self.client.add_flow(self.current_device_id, formatted_flow)

                    if not response.get('success'):
                        raise Exception(response.get('message', 'Failed to add flow'))

                    # Update progress - success
                    self.root.after(0, lambda i=idx+1, fn=filename:
                                   progress_dialog.update_progress(i, fn, success=True))

                    self.log(f"✓ Successfully added: {flow_label}", "SUCCESS")

                except Exception as e:
                    # Update progress - error
                    error_msg = str(e)
                    self.root.after(0, lambda i=idx+1, fn=filename, err=error_msg:
                                   progress_dialog.update_progress(i, fn, error=err))

                    self.log(f"✗ Failed to add {filename}: {error_msg}", "ERROR")

            # Show summary
            summary = progress_dialog.get_summary()
            self.log(f"Batch add completed: {summary['success']} successful, "
                    f"{summary['errors']} failed", "INFO")

            # Show summary message if there were errors
            if summary['errors'] > 0:
                error_details = "\n".join([f"- {fn}: {err}" for fn, err in summary['error_details'][:5]])
                if len(summary['error_details']) > 5:
                    error_details += f"\n... and {len(summary['error_details']) - 5} more"

                self.root.after(0, lambda:
                    messagebox.showwarning("Batch Add Complete",
                        f"Batch add completed with some errors:\n\n"
                        f"✓ Successful: {summary['success']}\n"
                        f"✗ Failed: {summary['errors']}\n\n"
                        f"Failed files:\n{error_details}"))
            else:
                self.root.after(0, lambda:
                    messagebox.showinfo("Success",
                        f"All flows added successfully!\n\n"
                        f"Total: {summary['total']} flows"))

            # Refresh flow list
            self.root.after(100, self.refresh_flows)

        # Start batch add thread
        threading.Thread(target=_batch_add_thread, daemon=True).start()

    def remove_selected(self):
        """Remove selected flow"""
        if not self.client or not self.current_device_id:
            messagebox.showwarning("Warning", "Please connect and select a device first")
            return

        # Get selected item
        selection = self.tree.selection()
        if not selection:
            messagebox.showwarning("Warning", "Please select a flow to remove")
            return

        item = self.tree.item(selection[0])
        flow_id = item['values'][0]
        flow_label = item['values'][1]

        # Check if protected
        if flow_id == self.PROTECTED_FLOW_ID:
            messagebox.showerror("Error",
                               f"Cannot remove protected flow: {flow_label}\n\n"
                               "This flow is required for the system to function.")
            return

        # Confirm deletion
        if not messagebox.askyesno("Confirm Deletion",
                                   f"Are you sure you want to remove flow:\n\n"
                                   f"ID: {flow_id}\n"
                                   f"Label: {flow_label}\n\n"
                                   "This action cannot be undone."):
            return

        def _remove_thread():
            try:
                self.log(f"Removing flow: {flow_label}", "WARNING")

                # Remove flow via RPC
                response = self.client.remove_flow(self.current_device_id, flow_id)

                if not response.get('success'):
                    raise Exception(response.get('message', 'Failed to remove flow'))

                self.log(f"Flow removed successfully: {flow_label}", "SUCCESS")
                messagebox.showinfo("Success", "Flow removed successfully!")

                # Refresh list
                self.refresh_flows()

            except Exception as e:
                self.log(f"Failed to remove flow: {str(e)}", "ERROR")
                messagebox.showerror("Error", f"Failed to remove flow:\n{str(e)}")

        threading.Thread(target=_remove_thread, daemon=True).start()

    def backup_all(self):
        """Backup all flows to JSON file"""
        if not self.client or not self.current_device_id:
            messagebox.showwarning("Warning", "Please connect and select a device first")
            return

        # Open save dialog
        file_path = filedialog.asksaveasfilename(
            title="Save Backup",
            defaultextension=".json",
            filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")],
            initialfile=f"flows_backup_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        )

        if not file_path:
            return

        def _backup_thread():
            try:
                self.log("Creating backup...", "INFO")

                # Get all flows
                response = self.client.get_flows(self.current_device_id)

                if not response.get('success'):
                    raise Exception(response.get('message', 'Failed to get flows'))

                # Save to file
                with open(file_path, 'w', encoding='utf-8') as f:
                    json.dump(response, f, indent=2, ensure_ascii=False)

                self.log(f"Backup saved: {os.path.basename(file_path)}", "SUCCESS")
                messagebox.showinfo("Success",
                                  f"Backup saved successfully!\n\n"
                                  f"File: {os.path.basename(file_path)}\n"
                                  f"Flows: {response.get('flowCount', 0)}")

            except Exception as e:
                self.log(f"Backup failed: {str(e)}", "ERROR")
                messagebox.showerror("Error", f"Failed to create backup:\n{str(e)}")

        threading.Thread(target=_backup_thread, daemon=True).start()

    def run(self):
        """Start the application"""
        self.root.mainloop()


def main():
    """Application entry point"""
    app = FlowManagerApp()
    app.run()


if __name__ == "__main__":
    main()
