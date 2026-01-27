# EC100 Provisioning Tool - Sistem Tasarım Dokümanı

## 1. Proje Özeti

EC100 endüstriyel bilgisayarlarına reverse SSH tunnel kurulumunu otomatize eden masaüstü uygulama.

### Amaç
- 1000+ cihaza hızlı kurulum
- Manuel hata riskini sıfırlama
- Merkezi envanter yönetimi
- Tek tıkla provisioning

### EC100 Cihaz Bilgileri

| Parametre | Değer |
|-----------|-------|
| SSH Kullanıcı | root |
| SSH Şifre | ***REMOVED*** |
| SSH Port | 22 |
| İşletim Sistemi | Linux (Ubuntu tabanlı, Çince locale) |

### VPS Bilgileri

| Parametre | Değer |
|-----------|-------|
| IP Adresi | REDACTED_HOST |
| SSH Port | 2222 |
| Tunnel Kullanıcı | tunnel |
| Tunnel Port Aralığı | 10001 - 11000 |

### Teknoloji Stack

| Bileşen | Teknoloji |
|---------|-----------|
| Dil | Python 3.10+ |
| GUI | PyQt6 |
| SSH | Paramiko |
| Veritabanı | SQLite |
| Ağ Tarama | python-nmap veya scapy |
| Paketleme | PyInstaller |

---

## 2. Kurulacak Python Paketleri

```bash
pip install PyQt6 paramiko python-nmap scapy cryptography pyinstaller
```

---

## 3. Proje Yapısı

```
ec100-provisioning-tool/
├── main.py                     # Uygulama giriş noktası
├── requirements.txt            # Bağımlılıklar
├── config.py                   # Sabit ayarlar (VPS IP, port, şifreler)
│
├── ui/                         # Arayüz dosyaları
│   ├── __init__.py
│   ├── main_window.py          # Ana pencere
│   ├── scan_widget.py          # Cihaz tarama ekranı
│   ├── provision_widget.py     # Kurulum ekranı
│   ├── inventory_widget.py     # Envanter ekranı
│   ├── settings_widget.py      # Ayarlar ekranı
│   └── styles.qss              # Qt stylesheet
│
├── core/                       # İş mantığı
│   ├── __init__.py
│   ├── scanner.py              # Ağ tarama
│   ├── ssh_manager.py          # SSH işlemleri
│   ├── provisioner.py          # Kurulum mantığı
│   ├── vps_manager.py          # VPS işlemleri
│   └── key_generator.py        # SSH key oluşturma
│
├── database/                   # Veritabanı
│   ├── __init__.py
│   ├── db_manager.py           # SQLite işlemleri
│   ├── models.py               # Veri modelleri
│   └── devices.db              # SQLite veritabanı
│
├── templates/                  # Şablon dosyalar
│   └── reverse-tunnel.service  # systemd servis şablonu
│
├── assets/                     # Görseller
│   ├── icon.png
│   └── logo.png
│
└── logs/                       # Log dosyaları
    └── provisioning.log
```

---

## 4. Veritabanı Şeması (SQLite)

### devices tablosu
```sql
CREATE TABLE devices (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    device_name VARCHAR(100) NOT NULL,
    serial_number VARCHAR(50) UNIQUE,
    mac_address VARCHAR(17),
    tunnel_port INTEGER UNIQUE NOT NULL,
    location VARCHAR(200),
    notes TEXT,
    public_key TEXT,
    status VARCHAR(20) DEFAULT 'offline',  -- online, offline, error
    last_seen DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### provision_logs tablosu
```sql
CREATE TABLE provision_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    device_id INTEGER,
    action VARCHAR(50),
    status VARCHAR(20),  -- success, failed
    message TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (device_id) REFERENCES devices(id)
);
```

### settings tablosu
```sql
CREATE TABLE settings (
    key VARCHAR(50) PRIMARY KEY,
    value TEXT
);

-- Varsayılan ayarlar
INSERT INTO settings (key, value) VALUES
    ('vps_ip', 'REDACTED_HOST'),
    ('vps_ssh_port', '2222'),
    ('vps_user', 'tunnel'),
    ('vps_key_path', ''),
    ('ec100_default_user', 'root'),
    ('ec100_default_password', '***REMOVED***'),
    ('ec100_ssh_port', '22'),
    ('port_range_start', '10001'),
    ('port_range_end', '11000'),
    ('scan_subnet', '192.168.1.0/24');
```

---

## 5. Arayüz Tasarımları

### 5.1 Ana Pencere (main_window.py)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EC100 Provisioning Tool                                    [─] [□] [X] │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐                    │
│  │ 🔍 Tara  │ │ 📋 Liste │ │ ⚙️ Ayarlar│ │ 📊 Loglar│                    │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                     [Seçili Tab İçeriği Burada]                         │
│                                                                         │
│                                                                         │
│                                                                         │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│  VPS: REDACTED_HOST ● Bağlı    |    Toplam Cihaz: 156    |   v1.0.0   │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Cihaz Tarama ve Kurulum Ekranı (scan_widget.py)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Ağ Tarama                                                      │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │  Subnet: [192.168.1.0/24        ]  [🔍 Tara]  [⏹ Durdur]       │    │
│  │                                                                 │    │
│  │  Bulunan Cihazlar:                                              │    │
│  │  ┌─────────────────────────────────────────────────────────┐   │    │
│  │  │ ☑ │ IP Adresi      │ MAC Adresi       │ Durum          │   │    │
│  │  ├───┼────────────────┼──────────────────┼────────────────┤   │    │
│  │  │ ☑ │ 192.168.1.101  │ AA:BB:CC:DD:EE:01│ SSH Açık ✅    │   │    │
│  │  │ ☑ │ 192.168.1.102  │ AA:BB:CC:DD:EE:02│ SSH Açık ✅    │   │    │
│  │  │ ☐ │ 192.168.1.103  │ AA:BB:CC:DD:EE:03│ Zaten Kurulu ⚠│   │    │
│  │  │ ☑ │ 192.168.1.104  │ AA:BB:CC:DD:EE:04│ SSH Açık ✅    │   │    │
│  │  └─────────────────────────────────────────────────────────┘   │    │
│  │                                                                 │    │
│  │  [Tümünü Seç]  [Seçimi Kaldır]         Seçili: 3 cihaz         │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Cihaz Bilgileri (Seçili cihaz için)                           │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                 │    │
│  │  IP Adresi:     192.168.1.101                                   │    │
│  │  MAC Adresi:    AA:BB:CC:DD:EE:01                               │    │
│  │                                                                 │    │
│  │  Cihaz Adı:     [EC100-                              ]          │    │
│  │  Lokasyon:      [                                    ]          │    │
│  │  Seri No:       [                                    ]          │    │
│  │  Port:          [10001     ] (Otomatik atandı)                  │    │
│  │                                                                 │    │
│  │  ┌───────────────────────────────────────────────────────────┐ │    │
│  │  │                    [🚀 Kurulumu Başlat]                    │ │    │
│  │  └───────────────────────────────────────────────────────────┘ │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Kurulum İlerlemesi                                             │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                 │    │
│  │  Cihaz: 192.168.1.101 (EC100-Fabrika-A)                        │    │
│  │  ████████████████████░░░░░░░░░░░░░░░░░░░░  45%                 │    │
│  │                                                                 │    │
│  │  ✅ SSH bağlantısı kuruldu                                      │    │
│  │  ✅ SSH key oluşturuldu                                         │    │
│  │  ✅ Key VPS'e eklendi                                           │    │
│  │  ⏳ autossh kuruluyor...                                        │    │
│  │  ⬜ Servis oluşturulacak                                        │    │
│  │  ⬜ Tunnel test edilecek                                        │    │
│  │  ⬜ Envantere kaydedilecek                                      │    │
│  │                                                                 │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.3 Envanter Ekranı (inventory_widget.py)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Arama ve Filtre                                                │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │  🔍 [Ara...                    ]   Durum: [Tümü        ▼]       │    │
│  │                                                                 │    │
│  │  [CSV Dışa Aktar]  [Yenile]  [Seçiliyi Sil]                    │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Cihaz Listesi                                    Toplam: 156   │    │
│  ├───┬──────────┬─────────────────┬───────────┬────────┬──────────┤    │
│  │ # │ Port     │ Cihaz Adı       │ Lokasyon  │ Durum  │ Son Gör. │    │
│  ├───┼──────────┼─────────────────┼───────────┼────────┼──────────┤    │
│  │ 1 │ 10001    │ EC100-Fabrika-A │ Istanbul  │ 🟢 Online│ 2 dk    │    │
│  │ 2 │ 10002    │ EC100-Fabrika-B │ Ankara    │ 🟢 Online│ 5 dk    │    │
│  │ 3 │ 10003    │ EC100-Depo-1    │ Izmir     │ 🔴 Offline│ 3 saat │    │
│  │ 4 │ 10004    │ EC100-Santral-1 │ Bursa     │ 🟢 Online│ 1 dk    │    │
│  │ 5 │ 10005    │ EC100-Santral-2 │ Bursa     │ 🟡 Error │ 15 dk   │    │
│  │...│ ...      │ ...             │ ...       │ ...    │ ...      │    │
│  └───┴──────────┴─────────────────┴───────────┴────────┴──────────┘    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Cihaz Detayı (Seçili: EC100-Fabrika-A)                        │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                 │    │
│  │  Port: 10001                    Seri No: ABC123456              │    │
│  │  MAC: AA:BB:CC:DD:EE:01         Oluşturma: 2024-01-15 14:30    │    │
│  │                                                                 │    │
│  │  SSH Komutu: ssh -p 10001 root@localhost                        │    │
│  │              (VPS üzerinden)                          [Kopyala] │    │
│  │                                                                 │    │
│  │  [🔌 Bağlan]  [✏️ Düzenle]  [🔄 Yeniden Kur]  [🗑️ Sil]          │    │
│  │                                                                 │    │
│  │  Notlar:                                                        │    │
│  │  ┌───────────────────────────────────────────────────────────┐ │    │
│  │  │ Fabrika A için kuruldu. Inverter kontrolü yapıyor.       │ │    │
│  │  └───────────────────────────────────────────────────────────┘ │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.4 Ayarlar Ekranı (settings_widget.py)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  VPS Ayarları                                                   │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                 │    │
│  │  IP Adresi:        [REDACTED_HOST              ]              │    │
│  │  SSH Portu:        [2222                         ]              │    │
│  │  Kullanıcı:        [tunnel                       ]              │    │
│  │  SSH Key:          [/home/user/.ssh/id_ed25519   ] [Gözat]      │    │
│  │                                                                 │    │
│  │  [🔗 Bağlantıyı Test Et]                          ✅ Bağlı      │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  EC100 Varsayılan Ayarları                                      │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                 │    │
│  │  Varsayılan Kullanıcı:  [root                    ]              │    │
│  │  Varsayılan Şifre:      [••••••••••              ] [👁]         │    │
│  │  SSH Portu:             [22                      ]              │    │
│  │                                                                 │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Port Aralığı                                                   │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                 │    │
│  │  Başlangıç Portu:   [10001     ]                                │    │
│  │  Bitiş Portu:       [11000     ]                                │    │
│  │                                                                 │    │
│  │  Kullanılabilir Port Sayısı: 999                                │    │
│  │  Kullanılan Port Sayısı: 156                                    │    │
│  │                                                                 │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Ağ Tarama Ayarları                                             │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                 │    │
│  │  Varsayılan Subnet:  [192.168.1.0/24             ]              │    │
│  │  Tarama Timeout:     [5          ] saniye                       │    │
│  │                                                                 │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│                    [💾 Kaydet]      [↩️ Varsayılana Dön]                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.5 Toplu Kurulum Ekranı (batch_provision_widget.py)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Toplu Kurulum                                                  │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │                                                                 │    │
│  │  Seçili Cihaz Sayısı: 5                                         │    │
│  │                                                                 │    │
│  │  ┌───────────────────────────────────────────────────────────┐ │    │
│  │  │ Cihaz              │ Port  │ İlerleme      │ Durum        │ │    │
│  │  ├────────────────────┼───────┼───────────────┼──────────────┤ │    │
│  │  │ 192.168.1.101      │ 10007 │ ██████████    │ ✅ Tamamlandı│ │    │
│  │  │ 192.168.1.102      │ 10008 │ ██████████    │ ✅ Tamamlandı│ │    │
│  │  │ 192.168.1.103      │ 10009 │ ██████░░░░    │ ⏳ Kuruluyor │ │    │
│  │  │ 192.168.1.104      │ 10010 │ ░░░░░░░░░░    │ ⏸️ Bekliyor  │ │    │
│  │  │ 192.168.1.105      │ 10011 │ ░░░░░░░░░░    │ ⏸️ Bekliyor  │ │    │
│  │  └───────────────────────────────────────────────────────────┘ │    │
│  │                                                                 │    │
│  │  Genel İlerleme: ████████░░░░░░░░░░░░  40%  (2/5 tamamlandı)   │    │
│  │                                                                 │    │
│  │  [⏸️ Duraklat]  [⏹️ İptal]                                       │    │
│  │                                                                 │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  Log                                                            │    │
│  ├─────────────────────────────────────────────────────────────────┤    │
│  │  14:30:15 [192.168.1.101] SSH bağlantısı kuruldu               │    │
│  │  14:30:16 [192.168.1.101] SSH key oluşturuldu                  │    │
│  │  14:30:18 [192.168.1.101] Key VPS'e eklendi                    │    │
│  │  14:30:20 [192.168.1.101] autossh kuruldu                      │    │
│  │  14:30:22 [192.168.1.101] Servis oluşturuldu                   │    │
│  │  14:30:25 [192.168.1.101] Tunnel aktif - BAŞARILI              │    │
│  │  14:30:26 [192.168.1.102] SSH bağlantısı kuruldu               │    │
│  │  ...                                                            │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Core Modüller

### 6.1 config.py

```python
# Varsayılan Konfigürasyon

# VPS Ayarları
VPS_IP = "REDACTED_HOST"
VPS_SSH_PORT = 2222
VPS_USER = "tunnel"
VPS_KEY_PATH = ""

# EC100 Varsayılan Ayarları
EC100_DEFAULT_USER = "root"
EC100_DEFAULT_PASSWORD = "***REMOVED***"
EC100_SSH_PORT = 22

# Port Aralığı
PORT_RANGE_START = 10001
PORT_RANGE_END = 11000

# Ağ Ayarları
DEFAULT_SUBNET = "192.168.1.0/24"
SCAN_TIMEOUT = 5

# Dosya Yolları
DB_PATH = "database/devices.db"
LOG_PATH = "logs/provisioning.log"

# systemd servis şablonu
SERVICE_TEMPLATE = """[Unit]
Description=Reverse SSH Tunnel
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
ExecStart=/usr/bin/autossh -M 0 -N -o "ServerAliveInterval=30" -o "ServerAliveCountMax=3" -o "ExitOnForwardFailure=yes" -o "StrictHostKeyChecking=accept-new" -i /root/.ssh/tunnel_key -R {port}:localhost:22 -p {vps_port} {vps_user}@{vps_ip}
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
"""
```

### 6.2 scanner.py - Ağ Tarama

```python
"""
Fonksiyonlar:
- scan_network(subnet: str) -> List[Dict]
    Verilen subnet'i tarar, SSH açık cihazları döner
    Return: [{"ip": "192.168.1.101", "mac": "AA:BB:CC:DD:EE:FF", "ssh_open": True}, ...]

- check_ssh_port(ip: str, port: int = 22) -> bool
    Belirtilen IP'de SSH portunun açık olup olmadığını kontrol eder

- get_mac_address(ip: str) -> str
    IP adresinden MAC adresini alır
"""
```

### 6.3 ssh_manager.py - SSH İşlemleri

```python
"""
Class: SSHManager

Metodlar:
- connect(host: str, username: str, password: str, port: int = 22) -> bool
    EC100'e SSH bağlantısı kurar

- execute_command(command: str) -> Tuple[str, str, int]
    Komut çalıştırır, (stdout, stderr, exit_code) döner

- upload_file(local_path: str, remote_path: str) -> bool
    Dosya yükler

- download_file(remote_path: str, local_path: str) -> bool
    Dosya indirir

- close()
    Bağlantıyı kapatır

- generate_ssh_key() -> Tuple[str, str]
    EC100'de SSH key oluşturur, (private_key, public_key) döner

- get_device_info() -> Dict
    Cihaz bilgilerini alır (hostname, MAC, seri no vb.)
"""
```

### 6.4 vps_manager.py - VPS İşlemleri

```python
"""
Class: VPSManager

Metodlar:
- connect() -> bool
    VPS'e bağlanır

- add_authorized_key(public_key: str) -> bool
    EC100'ün public key'ini VPS'e ekler

- remove_authorized_key(public_key: str) -> bool
    Key'i VPS'ten kaldırır

- check_tunnel_active(port: int) -> bool
    Belirtilen portta tunnel aktif mi kontrol eder

- get_active_tunnels() -> List[int]
    Aktif tunnel portlarını listeler

- close()
    Bağlantıyı kapatır
"""
```

### 6.5 provisioner.py - Kurulum Mantığı

```python
"""
Class: Provisioner

Metodlar:
- provision_device(ip: str, device_name: str, location: str, port: int) -> bool
    Tek bir cihaza kurulum yapar
    
    Adımlar:
    1. EC100'e SSH bağlantısı kur
    2. SSH key oluştur
    3. Public key'i VPS'e ekle
    4. autossh kur (apt install autossh)
    5. systemd servis dosyası oluştur
    6. Servisi başlat ve enable et
    7. Tunnel'ı test et
    8. Veritabanına kaydet
    
- provision_batch(devices: List[Dict], callback: Callable) -> Dict
    Toplu kurulum yapar, her adımda callback çağırır
    
- get_next_available_port() -> int
    Bir sonraki kullanılabilir port numarasını döner
    
- verify_tunnel(port: int) -> bool
    Tunnel'ın çalıştığını doğrular
    
- remove_device(device_id: int) -> bool
    Cihazı sistemden kaldırır (VPS'ten key sil, DB'den sil)

Signals (PyQt):
- progress_updated(device_ip: str, step: int, total_steps: int, message: str)
- device_completed(device_ip: str, success: bool, message: str)
- batch_completed(total: int, success: int, failed: int)
"""
```

### 6.6 db_manager.py - Veritabanı İşlemleri

```python
"""
Class: DatabaseManager

Metodlar:
- init_database()
    Veritabanını ve tabloları oluşturur

- add_device(device_data: Dict) -> int
    Yeni cihaz ekler, device_id döner

- update_device(device_id: int, data: Dict) -> bool
    Cihaz bilgilerini günceller

- delete_device(device_id: int) -> bool
    Cihazı siler

- get_device(device_id: int) -> Dict
    Cihaz bilgilerini getirir

- get_all_devices() -> List[Dict]
    Tüm cihazları listeler

- search_devices(query: str) -> List[Dict]
    Cihazlarda arama yapar

- get_device_by_port(port: int) -> Dict
    Port numarasına göre cihaz getirir

- get_next_port() -> int
    Kullanılmayan en düşük port numarasını döner

- get_setting(key: str) -> str
    Ayar değerini getirir

- set_setting(key: str, value: str) -> bool
    Ayar değerini kaydeder

- add_log(device_id: int, action: str, status: str, message: str)
    Log kaydı ekler

- get_logs(device_id: int = None) -> List[Dict]
    Log kayıtlarını getirir
"""
```

---

## 7. Kurulum Akışı (Provision Flow)

```
┌─────────────────┐
│  Başla          │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ 1. EC100'e SSH  │────▶│ Başarısız?      │──▶ Hata Logla, Bitir
│    Bağlan       │     │ Timeout?        │
└────────┬────────┘     └─────────────────┘
         │ Başarılı
         ▼
┌─────────────────┐
│ 2. Cihaz Bilgi  │
│    Topla (MAC,  │
│    hostname)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ 3. SSH Key      │────▶│ Key zaten var?  │──▶ Mevcut key'i kullan
│    Oluştur      │     │                 │
└────────┬────────┘     └─────────────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ 4. VPS'e        │────▶│ Başarısız?      │──▶ Hata Logla, Bitir
│    Bağlan       │     │                 │
└────────┬────────┘     └─────────────────┘
         │
         ▼
┌─────────────────┐
│ 5. Public Key'i │
│    VPS'e Ekle   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ 6. autossh      │────▶│ Zaten kurulu?   │──▶ Atla
│    Kur          │     │                 │
└────────┬────────┘     └─────────────────┘
         │
         ▼
┌─────────────────┐
│ 7. systemd      │
│    Servis       │
│    Oluştur      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 8. Servisi      │
│    Başlat       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ 9. Tunnel'ı     │────▶│ Başarısız?      │──▶ Retry (3x), Hata Logla
│    Test Et      │     │                 │
└────────┬────────┘     └─────────────────┘
         │ Başarılı
         ▼
┌─────────────────┐
│ 10. Veritabanına│
│     Kaydet      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Bitir (Başarı) │
└─────────────────┘
```

---

## 8. PyInstaller ile Paketleme

```bash
# Tek dosya executable oluştur
pyinstaller --onefile --windowed --icon=assets/icon.ico --name="EC100-Provisioning-Tool" main.py

# Veya spec dosyası ile
pyinstaller ec100-provisioning-tool.spec
```

---

## 9. Geliştirme Adımları (Claude Code için)

### Aşama 1: Temel Yapı
1. Proje klasör yapısını oluştur
2. requirements.txt oluştur
3. config.py oluştur
4. SQLite veritabanı ve db_manager.py oluştur

### Aşama 2: Core Modüller
5. ssh_manager.py - Paramiko ile SSH işlemleri
6. vps_manager.py - VPS bağlantı yönetimi
7. scanner.py - Ağ tarama
8. provisioner.py - Ana kurulum mantığı

### Aşama 3: Arayüz
9. main_window.py - Ana pencere ve tab yapısı
10. scan_widget.py - Tarama ekranı
11. provision_widget.py - Kurulum ekranı
12. inventory_widget.py - Envanter ekranı
13. settings_widget.py - Ayarlar ekranı
14. styles.qss - Tema ve stiller

### Aşama 4: Entegrasyon
15. Tüm modülleri entegre et
16. Hata yönetimi ve loglama ekle
17. Test et

### Aşama 5: Paketleme
18. PyInstaller ile .exe oluştur
19. Test ve dağıtım

---

## 10. Notlar

- Thread kullanımı: SSH işlemleri UI'ı bloklamemali, QThread kullan
- Loglama: Her adım loglanmalı (hem dosyaya hem UI'a)
- Hata yönetimi: Her SSH işlemi try-except içinde olmalı
- Bağlantı timeout: 10 saniye makul
- Retry mantığı: Başarısız işlemler 3 kez denenebilir