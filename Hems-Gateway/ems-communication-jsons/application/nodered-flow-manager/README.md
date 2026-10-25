# 🔧 Node-RED Flow Manager

Modern desktop application for managing Node-RED flows on EC100 devices via ThingsBoard RPC API.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Python](https://img.shields.io/badge/python-3.7+-green)
![License](https://img.shields.io/badge/license-MIT-orange)

## ✨ Features

- 🔐 **Secure Authentication** - Connect to ThingsBoard with encrypted credentials
- 📱 **Device Management** - View and select from all available EC100 devices
- 📋 **Flow Listing** - Display all Node-RED flows with clear identification
- ➕ **Add Flows** - Import flows from JSON files with automatic parsing
- 📁 **Batch Add Flows** - Add multiple flows at once from files or folder
- 🗑️ **Remove Flows** - Delete flows with protection for critical system flows
- 💾 **Backup System** - Export all flows to JSON for safekeeping
- 🎨 **Modern UI** - Clean, intuitive interface with custom styling
- 📝 **Activity Log** - Real-time logging of all operations
- ⚡ **Async Operations** - Non-blocking UI with threaded API calls
- 📊 **Progress Tracking** - Real-time progress bars for batch operations

## 📸 Screenshots

```
┌─────────────────────────────────────────────────────────────────┐
│  🔧 Node-RED Flow Manager v1.0                             [X]  │
├─────────────────────────────────────────────────────────────────┤
│  ┌─── Connection Settings ───────────────────────────────────┐  │
│  │ ThingsBoard URL: [https://thingsboard-dev.oklidis.com.tr] │  │
│  │ Username:        [tenant@thingsboard.org                ] │  │
│  │ Password:        [**********************                ] │  │
│  │ Device:          [EC100 Device 1                        ▼] │  │
│  │                                                            │  │
│  │ [💾 Save Settings]  [🔗 Connect]      Status: 🟢 Connected │  │
│  └────────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│  ┌─── Device Flows ──────────────────────────────────────────┐  │
│  │ Flow ID               │ Label               │ Status      │  │
│  │───────────────────────┼─────────────────────┼─────────────│  │
│  │ ad4d868acd65d470      │ Flow 1              │             │  │
│  │ 1bb10362e8c9abd6      │ RPC_BatterySocState │             │  │
│  │ rpc_flow_manager_tab  │ RPC_FlowManager     │ 🔒 Protected│  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  [🔄 Refresh]  [➕ Add Flow]  [🗑️ Remove Selected]  [💾 Backup All] │
├─────────────────────────────────────────────────────────────────┤
│  ┌─── Activity Log ──────────────────────────────────────────┐  │
│  │ [12:34:56] Application started                            │  │
│  │ [12:35:01] Connected to ThingsBoard                       │  │
│  │ [12:35:02] Loaded 5 flows from device                     │  │
│  └────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites

- Python 3.7 or higher
- Internet connection for ThingsBoard API access
- Valid ThingsBoard account with device access

### Installation

1. **Clone or download this repository**

```bash
cd nodered-flow-manager
```

2. **Install dependencies**

```bash
pip install -r requirements.txt
```

3. **Run the application**

```bash
python main.py
```

## 📖 Usage Guide

### 1. Connect to ThingsBoard

1. Enter your ThingsBoard server URL
2. Provide your username and password
3. Click **🔗 Connect**
4. Select your target device from the dropdown

### 2. View Flows

- Once connected and device selected, flows load automatically
- Click **🔄 Refresh** to reload the flow list
- Protected flows are marked with 🔒 icon

### 3. Add a New Flow

1. Click **➕ Add Flow**
2. Select a Node-RED flow JSON file
3. The application will parse and upload the flow
4. Flow list updates automatically

**Flow File Format:**
```json
[
  {
    "type": "tab",
    "id": "unique_flow_id",
    "label": "My Flow",
    "disabled": false,
    "info": ""
  },
  {
    "type": "function",
    "id": "node_id",
    "name": "My Function",
    "z": "unique_flow_id",
    ...
  }
]
```

### 4. Add Multiple Flows (Batch Add)

1. Click **📁 Add Multiple**
2. Choose one of two options:
   - **📄 Select Files** - Choose multiple JSON files (use Ctrl+Click)
   - **📁 Select Folder** - Select a folder containing JSON files
3. Watch the progress dialog showing:
   - Current file being processed
   - Progress bar with percentage
   - Success/failure count
4. View summary when complete
5. Failed files are logged and skipped automatically

**Benefits:**
- Upload dozens of flows at once
- Real-time progress tracking
- Error handling - continues even if some files fail
- Detailed summary report

### 5. Remove a Flow

1. Select the flow from the list
2. Click **🗑️ Remove Selected**
3. Confirm the deletion
4. Protected flows cannot be deleted

### 6. Backup Flows

1. Click **💾 Backup All**
2. Choose save location
3. All flows are exported to JSON format

### 7. Save Settings

- Click **💾 Save Settings** to remember:
  - ThingsBoard URL
  - Username (password not saved)
  - Last selected device

## 🔐 Security Features

### Protected Flows

The flow with ID `rpc_flow_manager_tab` is **protected** and cannot be deleted as it's critical for system operation.

### Credential Storage

- Passwords are **never saved** to disk
- Only URL and username are stored in `config.json`
- JWT tokens are kept in memory only

## 🏗️ Project Structure

```
nodered-flow-manager/
├── main.py              # Main application and UI
├── api_client.py        # ThingsBoard API client
├── config.json          # Saved settings (auto-created)
├── requirements.txt     # Python dependencies
└── README.md           # This file
```

## 🔧 Configuration

### config.json Format

```json
{
  "thingsboard_url": "https://your-thingsboard-server.com",
  "username": "your-username@example.com",
  "last_device_id": "device-uuid-or-null"
}
```

This file is automatically created when you click **Save Settings**.

## 🌐 API Reference

### ThingsBoard Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/auth/login` | POST | Authenticate and get JWT token |
| `/api/tenant/devices` | GET | List all accessible devices |
| `/api/rpc/twoway/{deviceId}` | POST | Execute RPC commands |

### RPC Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `getFlows` | `{}` | Retrieve all flows from device |
| `addFlow` | `{flow: {...}}` | Add a new flow to device |
| `removeFlow` | `{id: "flow_id"}` | Remove a flow from device |

## 🐛 Troubleshooting

### Connection Failed

- Verify ThingsBoard URL is correct (include `https://`)
- Check username and password
- Ensure network connectivity
- Verify firewall settings

### No Devices Found

- Confirm your account has device access permissions
- Check device status in ThingsBoard dashboard

### Flow Upload Failed

- Verify JSON file format is correct
- Ensure flow has a valid `tab` node
- Check file encoding is UTF-8

### UI Freezing

- The app uses threading for API calls
- If freezing occurs, check network connection
- Restart the application if issue persists

## 🎨 Customization

### Color Scheme

Edit the `COLORS` dictionary in `main.py`:

```python
COLORS = {
    'primary': '#2196F3',    # Blue
    'success': '#4CAF50',    # Green
    'danger': '#f44336',     # Red
    'warning': '#FF9800',    # Orange
    'info': '#00BCD4',       # Cyan
    'dark': '#263238',       # Dark gray
    'light': '#ECEFF1',      # Light gray
    'bg': '#FAFAFA',         # Background
    'white': '#FFFFFF'       # White
}
```

### Window Size

Modify in `FlowManagerApp.__init__()`:

```python
self.root.geometry("900x700")  # Width x Height
self.root.minsize(800, 600)    # Minimum size
```

## 📋 Requirements

- **requests** >= 2.28.0 - HTTP library for API calls
- **ttkthemes** >= 3.2.2 - Enhanced UI themes (optional)

## 🤝 Contributing

Feel free to submit issues, fork the repository, and create pull requests for any improvements.

## 📄 License

This project is licensed under the MIT License.

## 👨‍💻 Development

### Testing Credentials (Development Environment)

```
ThingsBoard URL: https://thingsboard-dev.oklidis.com.tr
Username: tenant@thingsboard.org
Password: ***REMOVED***
Test Device ID: 725dabc0-c6bf-11f0-a84b-2f34a5f564b4
```

**⚠️ WARNING:** These are development credentials. Never use in production!

### Code Style

- PEP 8 compliant
- Type hints where applicable
- Comprehensive docstrings
- Error handling for all API calls

### Adding New Features

1. Update `api_client.py` for new API methods
2. Add UI components in `main.py`
3. Use threading for blocking operations
4. Update this README

## 📞 Support

For issues and questions:
- Create an issue in the repository
- Contact the development team
- Check ThingsBoard documentation

## 🔮 Future Enhancements

- [ ] Flow editing capabilities
- [ ] Flow search and filtering
- [ ] Bulk flow operations
- [ ] Flow import from URL
- [ ] Export individual flows
- [ ] Dark mode toggle
- [ ] Multi-language support
- [ ] Flow validation before upload
- [ ] Scheduled backups
- [ ] Flow comparison tool

## 📚 Additional Resources

- [ThingsBoard Documentation](https://thingsboard.io/docs/)
- [Node-RED Documentation](https://nodered.org/docs/)
- [Python tkinter Guide](https://docs.python.org/3/library/tkinter.html)

---

**Made with ❤️ for EC100 Device Management**

*Version 1.0.0 - 2024*
