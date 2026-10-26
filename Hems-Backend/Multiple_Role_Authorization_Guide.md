# 🔐 Multiple Role Authorization Rehberi

## 📋 Yapılan Değişiklikler

### ✅ 1. Yeni Roller Eklendi (Roles.cs)
```csharp
public abstract class Roles
{
    public const string Administrator = nameof(Administrator);        // Sistem admini
    public const string MobilUser = nameof(MobilUser);                // Mobil kullanıcısı
    public const string TechnicalServiceUser = nameof(TechnicalServiceUser); // Teknik servis
    public const string DashboardUser = nameof(DashboardUser);        // Dashboard kullanıcısı
}
```

### ✅ 2. Yeni Policy'ler Eklendi (Policies.cs)
```csharp
public abstract class Policies
{
    // Device Management Policies
    public const string DeviceManagement = nameof(DeviceManagement);     // Admin, MobilUser, TechnicalServiceUser
    public const string DeviceReadOnly = nameof(DeviceReadOnly);         // Tüm roller
    
    // Site Management Policies  
    public const string SiteManagement = nameof(SiteManagement);        // Admin, MobilUser, DashboardUser
    
    // Dashboard Access Policies
    public const string DashboardAccess = nameof(DashboardAccess);       // Admin, DashboardUser
}
```

### ✅ 3. Policy Konfigürasyonu (Infrastructure/DependencyInjection.cs)
```csharp
builder.Services.AddAuthorization(options =>
{
    // Device Management - Admin, MobilUser, TechnicalServiceUser
    options.AddPolicy(Policies.DeviceManagement, policy => 
        policy.RequireRole(Roles.Administrator, Roles.MobilUser, Roles.TechnicalServiceUser));
    
    // Device Read Only - Tüm roller
    options.AddPolicy(Policies.DeviceReadOnly, policy => 
        policy.RequireRole(Roles.Administrator, Roles.MobilUser, Roles.TechnicalServiceUser, Roles.DashboardUser));
    
    // Site Management - Admin, MobilUser, DashboardUser
    options.AddPolicy(Policies.SiteManagement, policy => 
        policy.RequireRole(Roles.Administrator, Roles.MobilUser, Roles.DashboardUser));
    
    // Dashboard Access - Admin, DashboardUser
    options.AddPolicy(Policies.DashboardAccess, policy => 
        policy.RequireRole(Roles.Administrator, Roles.DashboardUser));
});
```

### ✅ 4. Devices Endpoint Authorization (Devices.cs)
```csharp
public override void Map(WebApplication app)
{
    app.MapGroup(this)
        .RequireAuthorization(Policies.DeviceReadOnly)      // GET - Tüm roller
        .MapGet(GetDevices)
        .MapGet(GetDevice, "{id}")
        .RequireAuthorization(Policies.DeviceManagement)    // POST, PUT - Admin, MobilUser, TechnicalServiceUser
        .MapPost(CreateDevice)
        .MapPut(UpdateDevice, "{id}")
        .RequireAuthorization(Policies.AdminOnly)           // DELETE - Sadece Admin
        .MapDelete(DeleteDevice, "{id}");
}
```

## 🎯 Authorization Matrisi

| Endpoint | Administrator | MobilUser | TechnicalServiceUser | DashboardUser |
|----------|---------------|-----------|---------------------|---------------|
| **GET /api/Devices** | ✅ | ✅ | ✅ | ✅ |
| **GET /api/Devices/{id}** | ✅ | ✅ | ✅ | ✅ |
| **POST /api/Devices** | ✅ | ✅ | ✅ | ❌ |
| **PUT /api/Devices/{id}** | ✅ | ✅ | ✅ | ❌ |
| **DELETE /api/Devices/{id}** | ✅ | ❌ | ❌ | ❌ |

## 🧪 Test Senaryoları

### ✅ Administrator Token ile Test
```bash
# 1. Admin kullanıcı ile login
POST /api/Users/login
{
  "email": "admin@example.com",
  "password": "<your-password>"
}

# 2. Tüm device endpoint'lerini test et
GET /api/Devices                    # ✅ 200 OK
GET /api/Devices/{id}              # ✅ 200 OK  
POST /api/Devices                  # ✅ 201 Created
PUT /api/Devices/{id}              # ✅ 204 No Content
DELETE /api/Devices/{id}           # ✅ 204 No Content
```

### ✅ MobilUser Token ile Test
```bash
# 1. MobilUser kullanıcı ile login
POST /api/Users/login
{
  "email": "mobil@example.com",
  "password": "Mobil123!"
}

# 2. Device endpoint'lerini test et
GET /api/Devices                    # ✅ 200 OK
GET /api/Devices/{id}              # ✅ 200 OK
POST /api/Devices                  # ✅ 201 Created
PUT /api/Devices/{id}              # ✅ 204 No Content
DELETE /api/Devices/{id}           # ❌ 403 Forbidden
```

### ✅ TechnicalServiceUser Token ile Test
```bash
# 1. TechnicalServiceUser kullanıcı ile login
POST /api/Users/login
{
  "email": "technical@example.com",
  "password": "Tech123!"
}

# 2. Device endpoint'lerini test et
GET /api/Devices                    # ✅ 200 OK
GET /api/Devices/{id}              # ✅ 200 OK
POST /api/Devices                  # ✅ 201 Created
PUT /api/Devices/{id}              # ✅ 204 No Content
DELETE /api/Devices/{id}           # ❌ 403 Forbidden
```

### ❌ DashboardUser Token ile Test
```bash
# 1. DashboardUser kullanıcı ile login
POST /api/Users/login
{
  "email": "dashboard@example.com",
  "password": "Dashboard123!"
}

# 2. Device endpoint'lerini test et
GET /api/Devices                    # ✅ 200 OK
GET /api/Devices/{id}              # ✅ 200 OK
POST /api/Devices                  # ❌ 403 Forbidden
PUT /api/Devices/{id}              # ❌ 403 Forbidden
DELETE /api/Devices/{id}           # ❌ 403 Forbidden
```

## 🔧 Role Atama İşlemleri

### 1. Yeni Kullanıcı Oluşturma
```bash
POST /api/Users/register
{
  "email": "mobil@example.com",
  "password": "Mobil123!"
}
```

### 2. Role Atama (Sadece Admin)
```bash
POST /api/RoleManagement/assign
{
  "userId": "user-id-123",
  "roleName": "MobilUser"
}
```

### 3. Kullanıcının Rollerini Görme
```bash
GET /api/RoleManagement/user/{userId}
```

## 🚀 Diğer Endpoint Örnekleri

### Sites Endpoint - Site Management Policy
```csharp
public class Sites : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization(Policies.SiteManagement)  // Admin, MobilUser, DashboardUser
            .MapGet(GetSites)
            .MapPost(CreateSite)
            .MapPut(UpdateSite, "{id}")
            .RequireAuthorization(Policies.AdminOnly)     // Sadece Admin silebilir
            .MapDelete(DeleteSite, "{id}");
    }
}
```

### Dashboard Endpoint - Dashboard Access Policy
```csharp
public class Dashboard : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization(Policies.DashboardAccess)  // Admin, DashboardUser
            .MapGet(GetDashboardData)
            .MapGet(GetAnalytics)
            .MapGet(GetReports);
    }
}
```

## 📊 Policy Özeti

| Policy | Roller | Kullanım Alanı |
|--------|--------|----------------|
| **AdminOnly** | Administrator | Sistem yönetimi, silme işlemleri |
| **DeviceManagement** | Administrator, MobilUser, TechnicalServiceUser | Cihaz ekleme/güncelleme |
| **DeviceReadOnly** | Tüm roller | Cihaz okuma |
| **SiteManagement** | Administrator, MobilUser, DashboardUser | Site yönetimi |
| **DashboardAccess** | Administrator, DashboardUser | Dashboard erişimi |

## ⚠️ Önemli Notlar

1. **Hierarchical Authorization**: Farklı endpoint'ler için farklı yetki seviyeleri
2. **Read vs Write**: Okuma ve yazma işlemleri için ayrı policy'ler
3. **Delete Operations**: Silme işlemleri genellikle sadece Admin'e özel
4. **Role Assignment**: Role atama sadece Admin tarafından yapılabilir
5. **Token Validation**: Her istekte token'daki role bilgisi kontrol edilir

## 🎯 Sonuç

Artık sisteminizde:
- **Administrator**: Tüm işlemleri yapabilir
- **MobilUser**: Device ekleme/güncelleme, Site yönetimi yapabilir
- **TechnicalServiceUser**: Device ekleme/güncelleme yapabilir
- **DashboardUser**: Sadece okuma ve dashboard erişimi

Bu şekilde her role'ün kendi yetki seviyesine göre işlem yapabilmesi sağlanmıştır! 🎉


