# 🏗️ HEMS Backend - Sistem Genel Bakış ve Çalışma Mantığı

Bu dokümantasyon, HEMS (Home Energy Management System) Backend uygulamasının genel mimarisini, çalışma mantığını, veri akışlarını ve temel kavramlarını açıklar.

## 📋 İçindekiler

- [🎯 Sistem Nedir?](#-sistem-nedir)
- [🏛️ Mimari Yapı](#️-mimari-yapı)
- [🔄 Sistem Akışları](#-sistem-akışları)
- [👥 Kullanıcı Rolleri ve Senaryolar](#-kullanıcı-rolleri-ve-senaryolar)
- [📡 Veri Akışı ve İletişim](#-veri-akışı-ve-iletişim)
- [🔐 Kimlik Doğrulama ve Yetkilendirme](#-kimlik-doğrulama-ve-yetkilendirme)
- [💾 Veri Modeli ve İlişkiler](#-veri-modeli-ve-ilişkiler)
- [⚙️ Teknik Bileşenler](#️-teknik-bileşenler)
- [🔄 Zamanlanmış İşlemler](#-zamanlanmış-işlemler)
- [📊 Dashboard ve Raporlama](#-dashboard-ve-raporlama)
- [🚨 Bildirim Sistemi](#-bildirim-sistemi)
- [🌐 API Yapısı](#-api-yapısı)

---

## 🎯 Sistem Nedir?

HEMS Backend, ev enerji yönetim sistemleri için geliştirilmiş bir REST API'sidir. Sistem, aşağıdaki ana işlevleri yerine getirir:

### Ana İşlevler

1. **Enerji Cihazı Yönetimi**: Solar inverter, batarya, PV panel gibi enerji cihazlarının kayıt ve yönetimi
2. **Enerji Verisi Toplama**: Cihazlardan gelen gerçek zamanlı enerji verilerini (üretim, tüketim, şarj/deşarj) kaydetme
3. **Dashboard ve Analiz**: Kullanıcılara enerji durumunu görselleştirme ve analiz sunma
4. **Bildirim Sistemi**: Cihaz alarmları ve uyarılarını gerçek zamanlı olarak kullanıcılara iletme
5. **Çoklu Kullanıcı Desteği**: Site'ları paylaşma ve yetki yönetimi
6. **Parametre Yönetimi**: Cihaz ayarlarını yönetme ve yapılandırma

### Sistem Bileşenleri

```
┌─────────────────────────────────────────────────────────┐
│                    HEMS Ecosystem                       │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐      ┌──────────────┐               │
│  │   Web App    │      │  Mobile App   │               │
│  │  (Frontend)  │      │  (Frontend)   │               │
│  └──────┬───────┘      └──────┬───────┘               │
│         │                     │                         │
│         └──────────┬──────────┘                        │
│                    │                                    │
│         ┌─────────▼─────────┐                          │
│         │   HEMS Backend API │                          │
│         │   (REST + SignalR) │                          │
│         └─────────┬─────────┘                          │
│                   │                                      │
│  ┌────────────────┴────────────────┐                   │
│  │                                  │                   │
│  │  ┌──────────────┐  ┌──────────┐│                   │
│  │  │  PostgreSQL  │  │ Weather  ││                   │
│  │  │  Database    │  │   API    ││                   │
│  │  └──────────────┘  └──────────┘│                   │
│  │                                  │                   │
│  └─────────────────────────────────┘                   │
│                   │                                      │
│         ┌─────────▼─────────┐                          │
│         │  Device Simulator  │                          │
│         │  (Background Jobs) │                          │
│         └─────────┬─────────┘                          │
│                   │                                      │
│         ┌─────────▼─────────┐                          │
│         │  Physical Devices │                          │
│         │ (Inverter, Battery)│                          │
│         └───────────────────┘                          │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 🏛️ Mimari Yapı

### Clean Architecture Katmanları

HEMS Backend, Clean Architecture prensiplerine uygun olarak 4 katmanlı bir yapıda organize edilmiştir:

#### 1. Domain Katmanı (İç Çekirdek)
- **Amaç**: İş mantığının saf hali, hiçbir dış bağımlılık yok
- **İçerik**:
  - **Entities**: Device, Site, User, DeviceComponent, DeviceData vb.
  - **Enums**: DeviceType, NotificationType, UserRole, InvitationStatus vb.
  - **ValueObjects**: Özel veri tipleri (Email, PhoneNumber vb.)
  - **Domain Events**: İş kurallarına göre tetiklenen olaylar
  - **Business Rules**: Saf iş mantığı kuralları

**Örnek:**
```csharp
public class Site : BaseAuditableEntity
{
    public string Name { get; set; }
    public string? Country { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    // ...
}
```

#### 2. Application Katmanı (Use Cases)
- **Amaç**: İş kullanım senaryolarını (use cases) tanımlar
- **Pattern**: CQRS (Command Query Responsibility Segregation)
- **İçerik**:
  - **Commands**: Veri değişikliği yapan işlemler (Create, Update, Delete)
  - **Queries**: Sadece veri okuma işlemleri
  - **Handlers**: Command ve Query işleyicileri
  - **Validators**: FluentValidation ile input doğrulama
  - **DTOs**: Veri transfer nesneleri
  - **Mappings**: AutoMapper konfigürasyonları

**Örnek:**
```csharp
// Command - Veri değiştirir
public record CreateSiteCommand : IRequest<Guid>
{
    public string Name { get; init; }
    // ...
}

// Query - Sadece okur
public record GetSitesQuery : IRequest<PaginatedList<SiteDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
```

#### 3. Infrastructure Katmanı (Dış Katman)
- **Amaç**: Dış dünya ile iletişim (veritabanı, harici servisler)
- **İçerik**:
  - **Data**: Entity Framework Core ile PostgreSQL entegrasyonu
  - **Identity**: ASP.NET Core Identity ile kullanıcı yönetimi
  - **Services**: SMTP email, Weather API entegrasyonları
  - **Migrations**: Veritabanı migration'ları
  - **Interceptors**: Auditing, Domain Events için interceptor'lar

**Örnek:**
```csharp
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Site> Sites { get; set; }
    public DbSet<Device> Devices { get; set; }
    // ...
}
```

#### 4. Web Katmanı (Sunum)
- **Amaç**: HTTP isteklerini karşılama ve yanıt verme
- **İçerik**:
  - **Endpoints**: Minimal API endpoint'leri
  - **Hubs**: SignalR gerçek zamanlı iletişim
  - **Middleware**: Exception handling, rate limiting, logging
  - **Configuration**: Dependency injection, CORS, authentication

**Örnek:**
```csharp
public class Sites : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetSites)
            .MapPost(CreateSite);
    }
}
```

### Katman Bağımlılıkları

```
┌─────────────────────────────────────────────────┐
│                   Web                            │
│  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Endpoints   │  │  SignalR Hubs        │   │
│  └──────┬───────┘  └──────────┬───────────┘   │
│         │                     │                │
│         └──────────┬──────────┘                │
│                    │                             │
│         ┌──────────▼──────────┐                 │
│         │   Application        │                 │
│         │  ┌──────┐ ┌───────┐ │                 │
│         │  │Cmd/Qr│ │Handlers││                 │
│         │  └──┬───┘ └───┬───┘ │                 │
│         └─────┼─────────┼─────┘                 │
│               │         │                        │
│  ┌────────────▼─────────▼────────┐            │
│  │    Infrastructure              │            │
│  │  ┌──────────┐  ┌──────────┐   │            │
│  │  │Database │  │ Services  │   │            │
│  │  └──────────┘  └──────────┘   │            │
│  └──────────────┬─────────────────┘            │
│                 │                                │
│         ┌───────▼────────┐                      │
│         │    Domain      │                      │
│         │  (No Dependencies)                    │
│         └─────────────────┘                      │
└─────────────────────────────────────────────────┘
```

**Bağımlılık Kuralı**: Dış katmanlar iç katmanlara bağımlıdır, iç katmanlar dış katmanlara bağımlı DEĞİLDİR.

---

## 🔄 Sistem Akışları

### 1. Kullanıcı Kayıt ve İlk Kurulum Akışı

```
┌─────────────┐
│   Kullanıcı  │
└──────┬───────┘
       │
       │ 1. Register/Login
       │
┌──────▼──────────────┐
│  GetUserStatus      │
│  (Durum Kontrolü)   │
└──────┬──────────────┘
       │
       ├─► NeedsSite?
       │   │
       │   └─► CreateSite
       │
       ├─► NeedsSetup?
       │   │
       │   └─► CreateSetup
       │       │
       │       ├─► CreateDevice (Yeni cihaz)
       │       │   veya
       │       └─► Mevcut Device'ı Bağla
       │
       └─► Complete!
           │
           └─► Dashboard'a Yönlendir
```

**Adımlar:**
1. Kullanıcı sisteme giriş yapar
2. `GetUserStatus` query'si çalışır ve kullanıcının durumunu kontrol eder:
   - Site'ı var mı? → Yoksa CreateSite
   - Setup'ı var mı? → Yoksa CreateSetup
   - Device'ı var mı? → Yoksa Device bağla
3. Her adım tamamlandıkça kullanıcı bir sonraki adıma yönlendirilir
4. Tüm adımlar tamamlandığında Dashboard'a yönlendirilir

### 2. Device Verisi Toplama ve İşleme Akışı

```
┌──────────────┐
│   Physical    │
│    Device     │
└──────┬────────┘
       │
       │ 1. Device Login
       │    (SerialNumber ile)
       │
┌──────▼──────────────┐
│ RegisterDevice      │
│ (JWT Token Al)      │
└──────┬──────────────┘
       │
       │ 2. Periodic Data Send
       │    (Her 5 dakikada bir)
       │
┌──────▼──────────────────────┐
│ CreateDeviceEnergyData      │
│  - PvEnergyKWh             │
│  - GridImport/Export         │
│  - BatteryCharge/Discharge   │
│  - LoadEnergy                │
└──────┬───────────────────────┘
       │
       ├─► 3. Save to Database
       │   (DeviceEnergyData table)
       │
       ├─► 4. Get Weather Data
       │   (Site koordinatlarından)
       │
       └─► 5. Update/Create Record
           (Upsert mantığı)
```

**Device Simulator Akışı:**
1. Device Simulator, Hangfire ile periyodik olarak çalışır
2. Her device için:
   - Device'a login olur (JWT token alır)
   - Simüle edilmiş enerji verileri oluşturur
   - `/api/deviceData/energy` endpoint'ine POST isteği gönderir
3. Backend:
   - Token doğrulaması yapar
   - Veriyi işler ve kaydeder
   - Hava durumu bilgisini entegre eder

### 3. Bildirim ve Alarm Akışı

```
┌──────────────┐
│    Device    │
└──────┬───────┘
       │
       │ 1. Alarm/Notification Trigger
       │
┌──────▼─────────────────┐
│ CreateDeviceNotification│
│ (POST /api/deviceNotifications)│
└──────┬─────────────────┘
       │
       ├─► 2. Save to Database
       │
       ├─► 3. SignalR Hub'a Gönder
       │   (DeviceNotificationService)
       │
       └─► 4. Connected Clients'a Push
           │
           ├─► Web Client
           └─► Mobile Client
```

**SignalR Bildirim Akışı:**
1. Device bir bildirim gönderir
2. Backend bildirimi veritabanına kaydeder
3. `DeviceNotificationService` SignalR hub'ına bildirim gönderir
4. Hub, o site'a bağlı tüm kullanıcılara bildirimi push eder
5. Web ve mobil uygulamalar gerçek zamanlı olarak bildirimi alır

### 4. Site Paylaşımı ve Davetiye Akışı

```
┌──────────────┐      ┌──────────────┐
│ Site Sahibi  │      │ Davet Edilen │
└──────┬───────┘      │  Kullanıcı   │
       │              └──────┬───────┘
       │                      │
       │ 1. CreateInvitation  │
       │    (Email gönder)    │
       │                      │
       │─────────►───────────►│
       │                       │
       │ 2. Email'de Token    │
       │    (6 haneli kod)    │
       │                       │
       │ 3. AcceptInvitation  │
       │    (Token ile)      │
       │                       │
       │◄─────────◄───────────│
       │                       │
       │ 4. UserSite Oluştur   │
       │    (Guest rolü)      │
       │                       │
       └───────►───────────────┘
```

**Davetiye Süreci:**
1. Site sahibi bir kullanıcıyı email ile davet eder
2. Sistem 6 haneli benzersiz bir token oluşturur
3. Email gönderilir (InvitationEmailService)
4. Davet edilen kullanıcı email'deki token'ı kullanarak davetiyeyi kabul eder
5. `UserSite` kaydı oluşturulur (Guest rolü ile)
6. Kullanıcı artık site'a erişebilir ve paylaşılan site'larını görebilir

---

## 👥 Kullanıcı Rolleri ve Senaryolar

### Rol Tipleri

#### 1. Administrator (Yönetici)
- **Yetkiler**: Tüm site'lara, device'lara ve kullanıcılara erişim
- **Kullanım**: Sistem yönetimi, raporlama, kullanıcı yönetimi

#### 2. Owner (Site Sahibi)
- **Yetkiler**: 
  - Kendi site'larını yönetebilir
  - Site'a device bağlayabilir
  - Site'ı başkalarıyla paylaşabilir
  - Paylaşılan kullanıcıları kaldırabilir
- **Kullanım**: Site kurulumu, device yönetimi, davetiye gönderme

#### 3. Guest (Misafir)
- **Yetkiler**:
  - Paylaşılan site'ları görüntüleyebilir
  - Dashboard verilerini görebilir
  - Site ayarlarını değiştiremez
- **Kullanım**: Paylaşılan site'ları izleme

### Kullanıcı Senaryoları

#### Senaryo 1: Yeni Kullanıcı Kurulumu
```
1. Kullanıcı kayıt olur / giriş yapar
2. GetUserStatus → NeedsSite döner
3. Site oluşturur (CreateSite)
   - Konum bilgileri girer (lat/long hava durumu için)
4. GetUserStatus → NeedsSetup döner
5. Setup oluşturur (CreateSetup)
   - Device'ı seçer veya yeni device oluşturur
   - QR Code veya Serial Number ile device bağlar
6. GetUserStatus → Complete döner
7. Dashboard'a yönlendirilir
```

#### Senaryo 2: Device Verisi Görüntüleme
```
1. Kullanıcı Dashboard'a girer
2. GetSitesByUser → Tüm site'ları listeler
3. Bir site seçer
4. GetSetupDevicesByUser → Site'ın device'ını getirir
5. Dashboard Query'leri çağrılır:
   - GetHero → Genel durum
   - GetEnergyProduction → Üretim grafiği
   - GetEnergyConsumption → Tüketim grafiği
   - GetBattery → Batarya durumu
   - GetSOC/GetSOH → Batarya sağlığı
```

#### Senaryo 3: Bildirim İzleme
```
1. Kullanıcı bildirimler sayfasına girer
2. GetDeviceNotifications → Site'a ait bildirimleri getirir
3. SignalR Hub'a bağlanır (JoinSiteGroup)
4. Yeni bildirim geldiğinde:
   - Hub gerçek zamanlı push eder
   - Kullanıcı bildirimi görür
   - MarkNotificationAsRead → Okundu işaretler
```

---

## 📡 Veri Akışı ve İletişim

### 1. Client → Backend Akışı (REST API)

```
┌──────────┐
│  Client  │
│ (Web/Mob)│
└────┬─────┘
     │
     │ HTTP Request
     │ (JWT Token)
     │
┌────▼──────────────────────┐
│     Web Endpoint          │
│  (Minimal API Route)      │
└────┬──────────────────────┘
     │
     │ MediatR Send
     │
┌────▼──────────────────────┐
│  Command/Query Handler    │
│  (Application Layer)      │
└────┬──────────────────────┘
     │
     │ IApplicationDbContext
     │
┌────▼──────────────────────┐
│    ApplicationDbContext    │
│    (EF Core)              │
└────┬──────────────────────┘
     │
     │ SQL Query
     │
┌────▼──────────────┐
│   PostgreSQL      │
└───────────────────┘
```

**Örnek Akış:**
```
1. Client: GET /api/sites (Bearer Token)
2. Web: Sites.GetSites endpoint'i çağrılır
3. Application: GetSitesQuery MediatR'a gönderilir
4. Handler: GetSitesQueryHandler çalışır
5. Infrastructure: DbContext.Sites sorgusu
6. Database: SQL query çalıştırılır
7. Response: SiteDto listesi döner
```

### 2. Device → Backend Akışı

```
┌──────────┐
│  Device  │
│ (Physical│
│  /Sim)   │
└────┬─────┘
     │
     │ 1. Device Login
     │ POST /api/devices/login
     │ { SerialNumber }
     │
┌────▼──────────────────────┐
│  RegisterDevice Command    │
│  (JWT Token döner)         │
└────┬───────────────────────┘
     │
     │ 2. Data Send (Periodic)
     │ POST /api/deviceData/energy
     │ Authorization: Bearer {DeviceToken}
     │
┌────▼──────────────────────┐
│ CreateDeviceEnergyData    │
│ - Token doğrulaması       │
│ - Veri kaydı              │
│ - Weather entegrasyonu    │
└───────────────────────────┘
```

### 3. Real-Time Communication (SignalR)

```
┌──────────┐                    ┌──────────┐
│  Client  │                    │  Client  │
│   1      │                    │   2      │
└────┬─────┘                    └────┬─────┘
     │                                │
     │ 1. Connect to Hub              │
     │    (JWT Token)                 │
     │                                │
     │ 2. JoinSiteGroup(siteId)       │
     │                                │
┌────▼────────────────────────────────▼────┐
│      DeviceNotificationHub              │
│      (SignalR Hub)                      │
└────┬────────────────────────────────────┘
     │
     │ 3. Notification Received
     │    (From Device/Service)
     │
┌────▼──────────────────────┐
│ DeviceNotificationService │
│ (Hub'a push eder)         │
└────┬───────────────────────┘
     │
     │ 4. Broadcast to Group
     │
     │───────► Client 1
     │───────► Client 2
```

---

## 🔐 Kimlik Doğrulama ve Yetkilendirme

### 1. Kullanıcı Authentication (JWT Bearer)

```
┌──────────┐
│  User    │
└────┬─────┘
     │
     │ POST /api/auth/login
     │ { email, password }
     │
┌────▼──────────────────────┐
│  IdentityService          │
│  (ASP.NET Core Identity)  │
└────┬───────────────────────┘
     │
     │ JWT Token
     │ { accessToken, refreshToken }
     │
┌────▼──────────────────────┐
│  Subsequent Requests     │
│  Authorization: Bearer    │
│  { accessToken }         │
└───────────────────────────┘
```

**JWT Token İçeriği:**
- User ID
- Email
- Roles
- Expiration

### 2. Device Authentication

```
┌──────────┐
│  Device  │
└────┬─────┘
     │
     │ POST /api/devices/login
     │ { SerialNumber, QRCode? }
     │
┌────▼──────────────────────┐
│ DeviceLoginCommand        │
│ - Device'ı bul            │
│ - DeviceJwtService        │
│   ile token oluştur       │
└────┬───────────────────────┘
     │
     │ Device Token
     │ (Farklı claims)
     │
┌────▼──────────────────────┐
│  DeviceOnly Policy       │
│  (Authorization)         │
└───────────────────────────┘
```

**Device Token Özellikleri:**
- Device ID claim
- "Device" role
- Özel DeviceOnly policy'si ile korunur

### 3. Authorization Pipeline

```
Request
   │
   ├─► [Authorize] Attribute
   │       │
   │       └─► JWT Token Validasyonu
   │
   ├─► AuthorizationBehaviour
   │       │
   │       ├─► [Authorize(Roles="Admin")]
   │       ├─► [Authorize(Policy="DeviceOnly")]
   │       └─► Custom Authorization
   │
   ├─► CheckSiteAccess / CheckDeviceAccess
   │       │
   │       ├─► Admin? → Allow
   │       ├─► Owner? → Allow
   │       └─► Shared? → Check UserSite
   │
   └─► Handler Execution
```

---

## 💾 Veri Modeli ve İlişkiler

### Ana Entity'ler ve İlişkileri

```
┌─────────────────────────────────────────────────┐
│              Domain Model                        │
└─────────────────────────────────────────────────┘

User (Identity)
  │
  ├─► Site (1:N)
  │     │
  │     ├─► Setup (1:1)
  │     │     │
  │     │     ├─► SetupDevice (1:1)
  │     │     │     │
  │     │     │     ├─► Device (N:1)
  │     │     │     │     │
  │     │     │     │     ├─► DeviceComponent (1:N)
  │     │     │     │     │
  │     │     │     │     ├─► DeviceData (1:N)
  │     │     │     │     │
  │     │     │     │     ├─► DeviceEnergyData (1:N)
  │     │     │     │     │
  │     │     │     │     └─► DeviceNotification (1:N)
  │     │     │     │
  │     │     │     └─► ExternalDevice (1:N)
  │     │     │
  │     │     └─► SetupDeviceParameterValue (1:N)
  │     │           │
  │     │           ├─► Parameter (N:1)
  │     │           │     │
  │     │           │     └─► ParameterGroup (N:1)
  │     │           │
  │     │           └─► ParameterValueSettingScheme (N:1)
  │     │
  │     ├─► Invitation (1:N)
  │     │     │
  │     │     └─► Notification (1:N)
  │     │
  │     └─► UserSite (N:M) [Shared Sites]
  │
  └─► UserSite (N:M) [Guest Access]
```

### Önemli İlişkiler

#### 1. Site → Setup → Device Zinciri
- **Site**: Fiziksel konum (ev, ofis vb.)
- **Setup**: Bir site'a ait device konfigürasyonu (bir site'ın sadece bir setup'ı olabilir)
- **SetupDevice**: Setup ile Device arasındaki bağlantı
- **Device**: Fiziksel enerji cihazı

#### 2. Device → Component Hiyerarşisi
- **Device**: Ana cihaz (örneğin: Solar Inverter)
- **DeviceComponent**: Cihazın parçaları (Batarya, PV Panel, BMS vb.)
- Her component bir DeviceType'a sahiptir (Battery, Inverter, PV vb.)

#### 3. Parameter Yönetimi
- **ParameterGroup**: Parametre grupları (örneğin: "Battery Settings")
- **Parameter**: Ayarlanabilir parametreler (örneğin: "Max Charge Current")
- **ParameterValueSettingScheme**: Device model'leri için varsayılan değerler
- **SetupDeviceParameterValue**: Device'ın gerçek çalışma ayarları

---

## ⚙️ Teknik Bileşenler

### 1. MediatR (CQRS Pattern)

**Amaç**: Command ve Query'leri merkezi olarak yönetmek

**Pipeline:**
```
Request (Command/Query)
   │
   ├─► AuthorizationBehaviour
   │       └─► [Authorize] kontrolleri
   │
   ├─► ValidationBehaviour
   │       └─► FluentValidation
   │
   ├─► PerformanceBehaviour
   │       └─► Logging, Timing
   │
   ├─► Handler Execution
   │       └─► İş mantığı
   │
   └─► Response
```

### 2. AutoMapper

**Amaç**: Entity ↔ DTO dönüşümleri

**Kullanım:**
- Entity'leri DTO'lara map etme
- Query'lerde `ProjectTo<T>()` ile veritabanı seviyesinde mapping

### 3. Entity Framework Core

**Özellikler:**
- PostgreSQL provider (Npgsql)
- Change tracking
- Lazy/Explicit loading
- Query filters (soft delete için)
- Interceptors (auditing, domain events)

### 4. FluentValidation

**Amaç**: Input doğrulama

**Örnek:**
```csharp
public class CreateSiteCommandValidator : AbstractValidator<CreateSiteCommand>
{
    public CreateSiteCommandValidator()
    {
        RuleFor(v => v.Name)
            .MaximumLength(200)
            .NotEmpty();
    }
}
```

### 5. SignalR

**Hub**: `DeviceNotificationHub`
- Site bazlı grup yönetimi
- JWT authentication
- Real-time bildirim gönderimi

**Client Interface:**
```csharp
public interface IDeviceNotificationHubClient
{
    Task ReceiveDeviceNotification(DeviceNotificationDto notification);
}
```

---

## 🔄 Zamanlanmış İşlemler

### Device Simulator (Hangfire)

**Çalışma Şekli:**
1. Hangfire background job processing kullanır
2. PostgreSQL'de job storage
3. Recurring jobs ile periyodik görevler

**Job Türleri:**

#### 1. Device Data Simulation
- **Frekans**: Her 5 dakikada bir
- **İşlev**: Her device için simüle edilmiş veri gönderir
- **Veri Tipi**: `CreateDeviceEnergyData`

#### 2. Energy Data Aggregation
- **Frekans**: Her saatte bir
- **İşlev**: Günlük/aylık toplam değerleri hesaplar

#### 3. Notification Simulation
- **Frekans**: Rastgele (test için)
- **İşlev**: Test bildirimleri oluşturur

---

## 📊 Dashboard ve Raporlama

### Dashboard Veri Yapısı

```
Dashboard
   │
   ├─► Hero (Özet Bilgiler)
   │     ├─► Toplam Üretim
   │     ├─► Toplam Tüketim
   │     ├─► Net Enerji
   │     └─► Batarya Durumu
   │
   ├─► Enerji Grafikleri
   │     ├─► GetEnergyProduction
   │     │     └─► PV üretim grafiği
   │     │
   │     ├─► GetEnergyConsumption
   │     │     └─► Yük tüketim grafiği
   │     │
   │     └─► GetEnergyData
   │           └─► Tüm enerji akışı
   │
   ├─► Güç Metrikleri
   │     └─► GetPowerMetrics
   │           └─► Anlık güç değerleri
   │
   └─► Batarya Bilgileri
         ├─► GetBattery
         ├─► GetSOC (State of Charge)
         ├─► GetSOH (State of Health)
         └─► GetChargeDischarge
```

### Veri Kaynağı

Dashboard verileri `DeviceEnergyData` tablosundan gelir:
- **DataType**: "DailyCumulativeEnergyData" veya "TotalCumulativeEnergyData"
- **Time Period**: daily, weekly, monthly, yearly
- **Aggregation**: Zaman dilimine göre toplama/ortalama

---

## 🚨 Bildirim Sistemi

### Bildirim Tipleri

#### 1. Device Notifications
- **Kaynak**: Fiziksel cihazlardan gelen alarmlar
- **Tip**: INFO, WARNING, ERROR
- **İçerik**: DeviceNotificationType tablosundan tanımlı mesajlar

#### 2. Invitation Notifications
- **Kaynak**: Site paylaşım davetiyeleri
- **İçerik**: "Kullanıcı size [Site] evini paylaştı"

### Bildirim Akışı

```
1. Bildirim Oluşur
   │
   ├─► CreateDeviceNotification (Device'dan)
   │     veya
   └─► CreateInvitation (Sistem tarafından)
   
2. Database'e Kaydedilir
   │
   ├─► DeviceNotifications table
   │     veya
   └─► Notifications table
   
3. SignalR Hub'a Gönderilir
   │
   └─► DeviceNotificationService
         │
         └─► JoinSiteGroup olan client'lara push
```

### Bildirim Filtreleme

- **Site bazlı**: Kullanıcı sadece erişebildiği site'ların bildirimlerini görür
- **Tarih bazlı**: Bugünkü, bu hafta, bu ay filtresi
- **Kategori bazlı**: INFO, WARNING, ERROR
- **Durum bazlı**: Okundu/okunmadı

---

## 🌐 API Yapısı

### Endpoint Organizasyonu

Her feature için ayrı `EndpointGroupBase` sınıfı:

```csharp
public class Sites : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetSites)
            .MapPost(CreateSite)
            .MapPut(UpdateSite, "{id}")
            .MapDelete(DeleteSite, "{id}");
    }
}
```

### Endpoint Kategorileri

#### 1. Auth Endpoints
- `POST /api/auth/login` - Kullanıcı girişi
- `POST /api/auth/register` - Kullanıcı kaydı
- `GET /api/auth/status` - Kullanıcı durumu

#### 2. Device Endpoints
- `POST /api/devices/login` - Device girişi
- `GET /api/devices` - Device listesi
- `POST /api/devices` - Device oluşturma
- `POST /api/deviceData/energy` - Enerji verisi gönderme (DeviceOnly)

#### 3. Site Endpoints
- `GET /api/sites` - Site listesi
- `POST /api/sites` - Site oluşturma
- `GET /api/sites/{id}` - Site detayı

#### 4. Dashboard Endpoints
- `GET /api/dashboard/hero?deviceId={id}`
- `GET /api/dashboard/energy-production?deviceId={id}&period={period}`
- `GET /api/dashboard/battery?deviceId={id}`

#### 5. Notification Endpoints
- `GET /api/deviceNotifications?siteId={id}`
- `POST /api/deviceNotifications` - Bildirim oluşturma (DeviceOnly)
- `PUT /api/deviceNotifications/{id}/read` - Okundu işaretleme

---

## 🔄 İş Akışı Özetleri

### Tam Sistem Çalışma Döngüsü

```
1. Kullanıcı Girişi
   │
   └─► JWT Token Al
   
2. Dashboard Yükleme
   │
   ├─► Site Listesi
   ├─► Device Bilgileri
   ├─► Enerji Verileri
   └─► Bildirimler
   
3. Device Verisi Gelişi (Sürekli)
   │
   ├─► Device Login
   ├─► Periyodik Veri Gönderimi
   ├─► Database'e Kayıt
   └─► SignalR Push (Gerçek zamanlı)
   
4. Bildirim Oluşumu
   │
   ├─► Device'dan Alarm
   ├─► Database'e Kayıt
   ├─► SignalR Broadcast
   └─► Client'lara Push
```

### Veri İşleme Döngüsü

```
DeviceEnergyData Gelişi
   │
   ├─► Validation
   ├─► Upsert Logic (Aynı datetime+type varsa güncelle)
   ├─► Weather Data Entegrasyonu
   │     └─► Site koordinatlarından hava durumu çek
   ├─► Database'e Kayıt
   │
   └─► Dashboard Verileri Güncellenir
         └─► Query'ler cached/real-time veri döner
```

---

## 📈 Performans ve Optimizasyon

### 1. Query Optimizasyonları

- **AsNoTracking()**: Read-only query'lerde change tracking kapalı
- **ProjectTo<T>()**: AutoMapper ile database seviyesinde projection
- **Pagination**: Tüm listelerde pagination desteği
- **Selective Loading**: Include() ile sadece gerekli ilişkiler

### 2. Caching Stratejisi

- **Query Results**: Dashboard verilerinde caching (gelecekte)
- **Weather Data**: Hava durumu verileri cache'lenir
- **Device Tokens**: Device authentication token'ları memory'de tutulur

### 3. Rate Limiting

- **Device Data Endpoints**: `DeviceDataRateLimitMiddleware` ile korunur
- **API Endpoints**: Genel rate limiting (gelecekte)

---

## 🔒 Güvenlik

### 1. Authentication

- **JWT Bearer Tokens**: Kullanıcı ve device authentication
- **Token Expiration**: 60 dakika (konfigüre edilebilir)
- **Refresh Tokens**: Token yenileme mekanizması

### 2. Authorization

- **Role-Based**: Admin, Owner, Guest rolleri
- **Resource-Based**: Site ve device erişim kontrolleri
- **Policy-Based**: DeviceOnly policy'si

### 3. Data Protection

- **HTTPS**: Tüm iletişim şifreli
- **SQL Injection**: EF Core parameterized queries
- **XSS Protection**: Input sanitization
- **CORS**: Cross-origin istek kontrolü

---

## 🧪 Test ve Kalite

### Test Yapısı

```
tests/
├── Application.UnitTests/
│   └─► Command/Query handler testleri
├── Domain.UnitTests/
│   └─► Business logic testleri
├── Infrastructure.IntegrationTests/
│   └─► Database, external services testleri
└── Application.FunctionalTests/
    └─► End-to-end API testleri
```

### Test Stratejisi

- **Unit Tests**: Her handler ve business rule için
- **Integration Tests**: Database ve external services için
- **Functional Tests**: API endpoint'leri için

---

## 📚 Özet

### Sistemin Temel Prensipleri

1. **Clean Architecture**: Katmanlar arası bağımlılık yönetimi
2. **CQRS**: Command ve Query ayrımı
3. **SOLID Principles**: Tek sorumluluk, açık-kapalı prensipleri
4. **Domain-Driven Design**: İş mantığına odaklı tasarım
5. **RESTful API**: Standart HTTP metodları ve durum kodları
6. **Real-Time Communication**: SignalR ile anlık bildirimler

### Sistemin Güçlü Yönleri

- ✅ Modüler ve genişletilebilir yapı
- ✅ Test edilebilir mimari
- ✅ Güvenli ve ölçeklenebilir
- ✅ Gerçek zamanlı bildirim desteği
- ✅ Çoklu kullanıcı ve yetki yönetimi
- ✅ Esnek parametre yapılandırma sistemi

### Gelecek Geliştirmeler

- 🔄 Real-time device status tracking
- 🔄 Advanced analytics ve raporlama
- 🔄 Machine learning ile enerji tahmini
- 🔄 Multi-tenant support
- 🔄 GraphQL API desteği
- 🔄 WebSocket alternatifi

---

*Bu dokümantasyon, HEMS Backend sisteminin genel çalışma mantığını ve mimarisini açıklar. Detaylı feature dokümantasyonu için `APPLICATION_FEATURES_DOCUMENTATION.md` dosyasına bakınız.*

