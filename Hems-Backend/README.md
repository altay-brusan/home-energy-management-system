# 🚀 HEMS Backend API - Teknik Dokümantasyon

HEMS Backend, Clean Architecture prensiplerine uygun olarak geliştirilmiş .NET 9.0 tabanlı bir REST API'sidir. Bu dokümantasyon, backend sisteminin teknik detaylarını, kurulum sürecini ve kullanımını açıklar.

## 📋 İçindekiler

- [🏗️ Sistem Mimarisi](#️-sistem-mimarisi)
- [🚀 Kurulum ve Çalıştırma](#-kurulum-ve-çalıştırma)
- [⚙️ Konfigürasyon](#️-konfigürasyon)
- [🔧 Backend Bileşenleri](#-backend-bileşenleri)
- [📡 API Endpoints](#-api-endpoints)
- [🎮 Device Simulator](#-device-simulator)
- [📡 SignalR Hub](#-signalr-hub)
- [🧪 Test ve Deployment](#-test-ve-deployment)

## 🏗️ Sistem Mimarisi

Backend sistemi Clean Architecture prensiplerine uygun 4 katmanlı yapıda organize edilmiştir:

```
📁 Hems-Backend/
├── 🎯 Domain/           # İş mantığı ve varlıklar
├── 🔧 Application/     # Use case'ler ve CQRS
├── 🏗️ Infrastructure/  # Veri erişimi ve harici servisler
├── 🌐 Web/             # API endpoints ve middleware
├── 🎮 DeviceSimulator/ # Cihaz simülasyon servisi
└── 🧪 tests/           # Test projeleri
```

### 🎯 Domain Katmanı (`src/Domain/`)
- **Entities**: Device, Site, User, DeviceComponent, DeviceData vb.
- **Enums**: DeviceType, NotificationType, UserRole vb.
- **ValueObjects**: Email, PhoneNumber vb.
- **Events**: Domain olayları

### 🔧 Application Katmanı (`src/Application/`)
- **Commands**: CreateDevice, UpdateSite, DeleteUser vb. (CQRS)
- **Queries**: GetSites, GetDevices, GetDeviceData vb. (CQRS)
- **Handlers**: Command/Query handler'ları
- **Validators**: FluentValidation ile input doğrulama
- **DTOs**: Veri transfer nesneleri

### 🏗️ Infrastructure Katmanı (`src/Infrastructure/`)
- **Data**: Entity Framework Core ile PostgreSQL entegrasyonu
- **Identity**: ASP.NET Core Identity ile kullanıcı yönetimi
- **Services**: SMTP email, Weather API entegrasyonları
- **Migrations**: Veritabanı migration'ları

### 🌐 Web Katmanı (`src/Web/`)
- **Endpoints**: Minimal API endpoint'leri
- **Hubs**: SignalR DeviceNotificationHub
- **Middleware**: GlobalExceptionMiddleware, DeviceDataRateLimitMiddleware
- **Configuration**: DependencyInjection, Program.cs

## 🚀 Kurulum ve Çalıştırma

### 📋 Sistem Gereksinimleri

#### Minimum Sunucu Gereksinimleri:
- **CPU**: 2 core (4 core önerilen)
- **RAM**: 4GB (8GB önerilen)
- **Disk**: 20GB boş alan
- **OS**: Ubuntu 20.04+ / Windows Server 2019+ / CentOS 8+
- **Network**: 80, 443, 5432 portları açık

#### Yazılım Gereksinimleri:
- **.NET 9.0 Runtime** (sunucu için)
- **PostgreSQL 12+** veritabanı
- **Nginx** (reverse proxy için)
- **Systemd** (Linux servis yönetimi için)

### 🔧 Development Kurulumu

#### 1. Projeyi Klonlayın
```bash
git clone <repository-url>
cd Hems-Backend
```

#### 2. Veritabanını Hazırlayın
```bash
# PostgreSQL servisini başlatın
# Windows: net start postgresql-x64-14
# Linux: sudo systemctl start postgresql
```

#### 3. Bağlantı String'ini Güncelleyin
`src/Web/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Hems_BackendDb": "Server=localhost;Port=5433;User ID=postgres;Password=***REMOVED***;Database=HEMS;Pooling=true;"
  }
}
```

#### 4. Paketleri Yükleyin ve Çalıştırın
```bash
# Ana API'yi başlatın
dotnet restore
cd src/Web
dotnet ef database update
dotnet run

# Device Simulator'ı başlatın (ayrı terminal)
cd Hems.DeviceSimulator
dotnet run
```

### 🌐 Erişim Adresleri
- **Ana API**: https://localhost:7164
- **Swagger UI**: https://localhost:7164/api
- **Device Simulator**: https://localhost:5001
- **Hangfire Dashboard**: https://localhost:5001/hangfire

### 🖥️ Production Sunucu Kurulumu

#### 1. Sunucu Hazırlığı (Ubuntu 20.04+)

```bash
# Sistem güncellemesi
sudo apt update && sudo apt upgrade -y

# .NET 9.0 Runtime kurulumu
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-runtime-9.0

# PostgreSQL kurulumu
sudo apt install -y postgresql postgresql-contrib

# Nginx kurulumu
sudo apt install -y nginx

# Firewall ayarları
sudo ufw allow 22
sudo ufw allow 80
sudo ufw allow 443
sudo ufw allow 5432
sudo ufw enable
```

#### 2. PostgreSQL Veritabanı Kurulumu

```bash
# PostgreSQL servisini başlat
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Veritabanı ve kullanıcı oluştur
sudo -u postgres psql
```

PostgreSQL içinde:
```sql
-- Veritabanı oluştur
CREATE DATABASE hems;

-- Kullanıcı oluştur
CREATE USER hems_user WITH PASSWORD 'secure_password_here';

-- Yetkileri ver
GRANT ALL PRIVILEGES ON DATABASE hems TO hems_user;
GRANT ALL ON SCHEMA public TO hems_user;

-- Çık
\q
```

#### 3. Uygulama Dosyalarını Sunucuya Kopyalama

```bash
# Proje dosyalarını sunucuya kopyala
scp -r ./Hems-Backend user@your-server:/opt/

# Sunucuda dizin oluştur
sudo mkdir -p /opt/hems
sudo chown -R www-data:www-data /opt/hems
```

#### 4. Production Konfigürasyonu

`/opt/hems/appsettings.Production.json` oluştur:
```json
{
  "ConnectionStrings": {
    "Hems_BackendDb": "Server=localhost;Port=5432;User ID=hems_user;Password=secure_password_here;Database=hems;Pooling=true;"
  },
  "SmtpSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-production-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true,
    "FromEmail": "your-production-email@gmail.com",
    "FromName": "HEMS System"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secure-production-key-min-32-chars",
    "Issuer": "HEMS-API",
    "Audience": "HEMS-Users",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

#### 5. Systemd Servis Dosyaları

Ana API için `/etc/systemd/system/hems-api.service`:
```ini
[Unit]
Description=HEMS Backend API
After=network.target postgresql.service

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/opt/hems/src/Web
ExecStart=/usr/bin/dotnet /opt/hems/src/Web/Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=hems-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

Device Simulator için `/etc/systemd/system/hems-simulator.service`:
```ini
[Unit]
Description=HEMS Device Simulator
After=network.target postgresql.service hems-api.service

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/opt/hems/Hems.DeviceSimulator
ExecStart=/usr/bin/dotnet /opt/hems/Hems.DeviceSimulator/Hems.DeviceSimulator.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=hems-simulator
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5001

[Install]
WantedBy=multi-user.target
```

#### 6. Nginx Reverse Proxy Konfigürasyonu

`/etc/nginx/sites-available/hems`:
```nginx
server {
    listen 80;
    server_name your-domain.com;

    # Ana API
    location /api/ {
        proxy_pass http://localhost:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # SignalR Hub
    location /deviceNotificationHub {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Device Simulator
    location /simulator/ {
        proxy_pass http://localhost:5001/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Hangfire Dashboard
    location /hangfire/ {
        proxy_pass http://localhost:5001/hangfire/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

#### 7. SSL Sertifikası (Let's Encrypt)

```bash
# Certbot kurulumu
sudo apt install -y certbot python3-certbot-nginx

# SSL sertifikası al
sudo certbot --nginx -d your-domain.com

# Otomatik yenileme
sudo crontab -e
# Şu satırı ekle:
# 0 12 * * * /usr/bin/certbot renew --quiet
```

#### 8. Servisleri Başlatma

```bash
# Nginx konfigürasyonunu aktif et
sudo ln -s /etc/nginx/sites-available/hems /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx

# Migration çalıştır
cd /opt/hems/src/Web
sudo -u www-data dotnet ef database update

# Servisleri başlat
sudo systemctl daemon-reload
sudo systemctl enable hems-api
sudo systemctl enable hems-simulator
sudo systemctl start hems-api
sudo systemctl start hems-simulator

# Durum kontrolü
sudo systemctl status hems-api
sudo systemctl status hems-simulator
```

#### 9. Monitoring ve Logging

```bash
# Log dosyalarını izle
sudo journalctl -u hems-api -f
sudo journalctl -u hems-simulator -f

# Servis durumlarını kontrol et
sudo systemctl status hems-api hems-simulator postgresql nginx

# Disk kullanımını kontrol et
df -h
du -sh /opt/hems/
```

#### 10. Backup Stratejisi

```bash
# PostgreSQL backup script
#!/bin/bash
BACKUP_DIR="/opt/backups/hems"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR

# Veritabanı backup
pg_dump -h localhost -U hems_user hems > $BACKUP_DIR/hems_backup_$DATE.sql

# Eski backup'ları temizle (7 günden eski)
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete

# Cron job ekle
# 0 2 * * * /opt/scripts/hems_backup.sh
```

### 🔧 Windows Server Kurulumu

#### 1. IIS ve .NET Hosting Bundle

```powershell
# IIS kurulumu
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-HttpErrors, IIS-HttpLogging, IIS-RequestFiltering, IIS-StaticContent, IIS-DefaultDocument, IIS-DirectoryBrowsing

# .NET 9.0 Hosting Bundle indir ve kur
# https://dotnet.microsoft.com/download/dotnet/9.0
```

#### 2. PostgreSQL Windows Kurulumu

```powershell
# PostgreSQL Windows installer indir
# https://www.postgresql.org/download/windows/
# Kurulum sırasında port 5432, password belirle
```

#### 3. Windows Service Olarak Çalıştırma

```powershell
# NSSM (Non-Sucking Service Manager) kur
# https://nssm.cc/download

# Ana API servisi
nssm install HemsAPI "C:\Program Files\dotnet\dotnet.exe" "C:\inetpub\wwwroot\hems\Web.dll"
nssm set HemsAPI AppDirectory "C:\inetpub\wwwroot\hems"
nssm set HemsAPI AppEnvironmentExtra ASPNETCORE_ENVIRONMENT=Production
nssm start HemsAPI

# Device Simulator servisi
nssm install HemsSimulator "C:\Program Files\dotnet\dotnet.exe" "C:\inetpub\wwwroot\hems\Hems.DeviceSimulator.dll"
nssm set HemsSimulator AppDirectory "C:\inetpub\wwwroot\hems"
nssm set HemsSimulator AppEnvironmentExtra ASPNETCORE_ENVIRONMENT=Production
nssm start HemsSimulator
```

## ⚙️ Konfigürasyon

### 🔐 JWT Kimlik Doğrulama
```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-here",
    "Issuer": "HEMS-API",
    "Audience": "HEMS-Users",
    "ExpirationMinutes": 60
  }
}
```

### 📧 SMTP Email Servisi
```json
{
  "SmtpSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "Username": "pentaunity@pentaunity.com",
    "Password": "***REMOVED***",
    "EnableSsl": true,
    "FromEmail": "pentaunity@pentaunity.com",
    "FromName": "HEMS System"
  }
}
```

### 🌤️ Weather API Entegrasyonu
```json
{
  "WeatherApi": {
    "ApiKey": "***REMOVED***",
    "BaseUrl": "http://api.weatherapi.com/v1"
  }
}
```

### 🎮 Device Simulator Ayarları
```json
{
  "DeviceSimulator": {
    "AutoStart": true,
    "DefaultIntervalSeconds": 300,
    "CronExpression": "0 */5 * * * *"
  },
  "HemsBackend": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

## 🔧 Backend Bileşenleri

### 🎮 Device Simulator (`Hems.DeviceSimulator/`)

Device Simulator, gerçek cihazların davranışını simüle eden ayrı bir .NET servisidir.

#### Teknik Özellikler:
- **Hangfire**: Zamanlanmış görevler için background job processing
- **PostgreSQL Storage**: Hangfire job storage için PostgreSQL
- **HTTP Client**: Ana API'ye veri gönderimi
- **JSON Data Generation**: Gerçekçi cihaz verisi üretimi

#### Servis Yapısı:
```
Hems.DeviceSimulator/
├── Controllers/
│   └── DeviceController.cs      # API endpoints
├── Services/
│   ├── DeviceSimulationJobService.cs    # Ana simülasyon servisi
│   ├── EnergyDataJobService.cs          # Enerji verisi servisi
│   ├── DeviceNotificationJobService.cs   # Bildirim simülasyonu
│   ├── DataGeneratorService.cs          # Veri üretimi
│   └── JsonDataService.cs              # JSON veri servisi
├── Models/
│   ├── BatteryDataJson.cs
│   ├── InverterData.cs
│   └── EnergyFlowData.cs
└── DataJsonFiles/               # Simülasyon verileri
    ├── Battery/
    ├── BMS/
    └── Inverter/
```

#### API Endpoints:
```http
POST /api/device/start           # Simülasyonu başlat
POST /api/device/stop            # Simülasyonu durdur
GET  /api/device/status          # Durum kontrolü
POST /api/device/schedule/{id}   # Belirli cihazı zamanla
POST /api/device/takeLiveData    # Canlı veri üret
```

### 📡 SignalR Hub (`src/Web/Hubs/DeviceNotificationHub.cs`)

Gerçek zamanlı bildirim sistemi için SignalR hub'ı.

#### Teknik Özellikler:
- **Authorization**: JWT token tabanlı kimlik doğrulama
- **Group Management**: Site ve cihaz bazlı grup yönetimi
- **Access Control**: Kullanıcı yetki kontrolü

#### Hub Metodları:
```csharp
// Site grubuna katılma
JoinSiteGroup(Guid siteId)

// Cihaz grubuna katılma  
JoinDeviceGroup(Guid deviceId)

// Gruplardan çıkma
LeaveSiteGroup(Guid siteId)
LeaveDeviceGroup(Guid deviceId)
```

#### Client Interface:
```csharp
public interface IDeviceNotificationHubClient
{
    Task ReceiveDeviceNotification(DeviceNotificationDto notification);
    Task ReceiveDeviceNotificationUpdate(DeviceNotificationDto notification);
}
```

### 🏗️ Infrastructure Servisleri

#### SMTP Email Service (`src/Infrastructure/Services/SmtpEmailSender.cs`)
- **SMTP Configuration**: Gmail SMTP entegrasyonu
- **Template Support**: HTML email template desteği
- **Async Operations**: Asenkron email gönderimi

#### Weather API Service
- **External API Integration**: WeatherAPI.com entegrasyonu
- **Caching**: API response caching
- **Error Handling**: API hata yönetimi

## 📡 API Endpoints

### 🔐 Kimlik Doğrulama (`src/Web/Endpoints/Auth.cs`)

#### Kullanıcı Girişi
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

#### Cihaz Girişi
```http
POST /api/devices/login
Content-Type: application/json

{
  "serialNumber": "DEVICE123456"
}
```

### 🏠 Site Yönetimi (`src/Web/Endpoints/Sites.cs`)

#### Site Oluşturma
```http
POST /api/sites
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Evim",
  "address": "İstanbul, Türkiye",
  "latitude": 41.0082,
  "longitude": 28.9784
}
```

#### Site Listesi
```http
GET /api/sites
Authorization: Bearer {token}
```

### 🔧 Cihaz Yönetimi (`src/Web/Endpoints/Devices.cs`)

#### Cihaz Ekleme
```http
POST /api/devices
Authorization: Bearer {token}
Content-Type: application/json

{
  "manufacturer": "Tesla",
  "modelNumber": "Powerwall-2",
  "serialNumber": "PW123456789",
  "deviceType": "Battery"
}
```

### 📊 Veri Gönderimi (`src/Web/Endpoints/DeviceData.cs`)

#### Cihaz Verisi Gönderme
```http
POST /api/deviceData
Authorization: Bearer {device_token}
Content-Type: application/json

{
  "dataType": "BatteryData",
  "deviceId": "device-guid",
  "requestDate": "2024-01-15T10:30:00Z",
  "data": {
    "voltage": 48.5,
    "current": 25.3,
    "temperature": 22.1,
    "soc": 85.2
  }
}
```

### 📧 Bildirim Sistemi (`src/Web/Endpoints/DeviceNotifications.cs`)

#### Bildirim Listesi
```http
GET /api/deviceNotifications
Authorization: Bearer {token}
```

#### Bildirim Oluşturma
```http
POST /api/deviceNotifications
Authorization: Bearer {token}
Content-Type: application/json

{
  "deviceId": "device-guid",
  "title": "Yüksek Sıcaklık Uyarısı",
  "message": "Batarya sıcaklığı kritik seviyede",
  "severity": "High"
}
```

### 📱 Mobil API (`src/Web/Endpoints/MobileDashboard.cs`)

#### Mobil Dashboard Verileri
```http
GET /api/mobile/dashboard
Authorization: Bearer {token}
```

### 📈 Dashboard API (`src/Web/Endpoints/Dashboard.cs`)

#### Dashboard Özet Verileri
```http
GET /api/dashboard/summary
Authorization: Bearer {token}
```

#### Enerji Akış Verileri
```http
GET /api/dashboard/energyFlow
Authorization: Bearer {token}
```

## 🧪 Test ve Deployment

### 🧪 Test Yapısı

#### Test Projeleri:
```
tests/
├── Application.UnitTests/        # Application katmanı unit testleri
├── Application.FunctionalTests/  # End-to-end API testleri
├── Domain.UnitTests/            # Domain katmanı unit testleri
└── Infrastructure.IntegrationTests/ # Infrastructure entegrasyon testleri
```

#### Test Çalıştırma:
```bash
# Tüm testleri çalıştır
dotnet test

# Belirli bir test projesi
dotnet test tests/Application.UnitTests

# Coverage ile test
dotnet test --collect:"XPlat Code Coverage"
```

### 🚀 Deployment

#### Docker ile Deployment:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Web/Web.csproj", "src/Web/"]
RUN dotnet restore "src/Web/Web.csproj"
COPY . .
WORKDIR "/src/src/Web"
RUN dotnet build "Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.dll"]
```

#### Azure DevOps Pipeline:
```yaml
# azure-pipelines.yaml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '9.x'

- script: |
    dotnet restore
    dotnet build --configuration $(buildConfiguration)
    dotnet test --configuration $(buildConfiguration)
  displayName: 'Build and Test'

- script: |
    dotnet publish src/Web/Web.csproj --configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)
  displayName: 'Publish'

- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: '$(Build.ArtifactStagingDirectory)'
    ArtifactName: 'drop'
```

### 🔧 Production Konfigürasyonu

#### Environment Variables:
```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__Hems_BackendDb="Server=prod-server;Database=HEMS;User ID=prod_user;Password=secure_password;"
export JwtSettings__SecretKey="super-secure-production-key"
export SmtpSettings__Password="production-smtp-password"
```

#### Health Checks:
```http
GET /health
```

---

## 📞 Teknik Destek

### 🐛 Hata Bildirimi
GitHub Issues kullanarak hata bildirimi yapabilirsiniz.

### 💡 Özellik İsteği
Yeni özellik önerileri için GitHub Issues'da "Feature Request" etiketi ile açın.

### 📚 Dokümantasyon
- **Swagger UI**: https://localhost:7164/api
- **Hangfire Dashboard**: https://localhost:5001/hangfire

---

**HEMS Backend API v1.0** - Clean Architecture ile geliştirilmiş modern .NET 9.0 API'si