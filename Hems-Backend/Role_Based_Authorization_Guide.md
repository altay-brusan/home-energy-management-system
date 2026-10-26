# 🔐 Role-Based Authorization Rehberi

## 📋 Yapılan Değişiklikler

### 1. Policies.cs - Yeni Policy'ler Eklendi
```csharp
public abstract class Policies
{
    public const string CanPurge = nameof(CanPurge);
    public const string AdminOnly = nameof(AdminOnly);        // ✅ YENİ
    public const string UserManagement = nameof(UserManagement); // ✅ YENİ
}
```

### 2. Infrastructure/DependencyInjection.cs - Policy Konfigürasyonu
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator));
    options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Administrator));     // ✅ YENİ
    options.AddPolicy(Policies.UserManagement, policy => policy.RequireRole(Roles.Administrator)); // ✅ YENİ
});
```

### 3. RoleManagement.cs - AdminOnly Policy Uygulandı
```csharp
public override void Map(WebApplication app)
{
    app.MapGroup(this)
        .RequireAuthorization(Policies.AdminOnly)  // ✅ Sadece Admin erişebilir
        .MapGet(GetRoles)
        .MapPost(CreateRole)
        .MapDelete(DeleteRole, "{roleName}")
        .MapPut(AssignRoleToUser, "assign")
        .MapGet(GetUserRoles, "user/{userId}");
}
```

## 🎯 Authorization Kullanım Örnekleri

### ✅ 1. Tüm Endpoint'ler İçin Admin Gerekli
```csharp
public override void Map(WebApplication app)
{
    app.MapGroup(this)
        .RequireAuthorization(Policies.AdminOnly)  // Tüm endpoint'ler admin gerekli
        .MapGet(GetRoles)
        .MapPost(CreateRole);
}
```

### ✅ 2. Belirli Endpoint'ler İçin Farklı Yetkiler
```csharp
public override void Map(WebApplication app)
{
    app.MapGroup(this)
        .RequireAuthorization()
        .MapGet(GetSites)                    // Herkes okuyabilir
        .MapPost(CreateSite)                 // Herkes oluşturabilir
        .MapPut(UpdateSite, "{id}")          // Herkes güncelleyebilir
        .MapDelete(DeleteSite, "{id}")      // Sadece admin silebilir
        .RequireAuthorization(Policies.AdminOnly);
}
```

### ✅ 3. Method Seviyesinde Authorization
```csharp
public async Task<IResult> DeleteSite(
    ISender sender,
    Guid id,
    [Authorize(Policies.AdminOnly)] string? currentUser)  // Sadece bu method admin gerekli
{
    await sender.Send(new DeleteSiteCommand(id));
    return Results.NoContent();
}
```

### ✅ 4. Multiple Role Gereksinimi
```csharp
// Policies.cs'ye ekle:
public const string AdminOrManager = nameof(AdminOrManager);

// DependencyInjection.cs'ye ekle:
options.AddPolicy(Policies.AdminOrManager, policy => 
    policy.RequireRole(Roles.Administrator, "Manager"));

// Endpoint'te kullan:
.RequireAuthorization(Policies.AdminOrManager)
```

## 🔧 Yeni Role'ler Ekleme

### 1. Roles.cs'ye Yeni Role Ekle
```csharp
public abstract class Roles
{
    public const string Administrator = nameof(Administrator);
    public const string Manager = nameof(Manager);        // ✅ YENİ
    public const string User = nameof(User);              // ✅ YENİ
    public const string Guest = nameof(Guest);            // ✅ YENİ
}
```

### 2. Yeni Policy'ler Oluştur
```csharp
public abstract class Policies
{
    public const string CanPurge = nameof(CanPurge);
    public const string AdminOnly = nameof(AdminOnly);
    public const string UserManagement = nameof(UserManagement);
    public const string ManagerOrAdmin = nameof(ManagerOrAdmin);  // ✅ YENİ
    public const string AuthenticatedUser = nameof(AuthenticatedUser); // ✅ YENİ
}
```

### 3. Policy Konfigürasyonu
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Administrator));
    options.AddPolicy(Policies.ManagerOrAdmin, policy => 
        policy.RequireRole(Roles.Manager, Roles.Administrator));
    options.AddPolicy(Policies.AuthenticatedUser, policy => 
        policy.RequireAuthenticatedUser());
});
```

## 🚀 Pratik Kullanım Senaryoları

### Senaryo 1: Energy Management - Sadece Admin
```csharp
public class EnergyManagement : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization(Policies.AdminOnly)  // Sadece admin
            .MapGet(GetEnergyReports)
            .MapPost(CreateEnergyReport)
            .MapDelete(DeleteEnergyReport, "{id}");
    }
}
```

### Senaryo 2: Device Control - Manager ve Admin
```csharp
public class DeviceControl : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization(Policies.ManagerOrAdmin)  // Manager veya Admin
            .MapGet(GetDevices)
            .MapPost(ControlDevice)
            .MapPut(UpdateDeviceSettings, "{id}");
    }
}
```

### Senaryo 3: User Dashboard - Authenticated Users
```csharp
public class UserDashboard : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization(Policies.AuthenticatedUser)  // Giriş yapmış herkes
            .MapGet(GetUserProfile)
            .MapGet(GetUserSites)
            .MapGet(GetUserDevices);
    }
}
```

## 🔍 Test Etme

### 1. Admin Token ile Test
```bash
# Admin token al
POST /api/Users/login
{
  "email": "admin@example.com",
  "password": "<your-password>"
}

# Role management endpoint'lerini test et
GET /api/RoleManagement
Authorization: Bearer {admin_token}
```

### 2. Normal User Token ile Test
```bash
# Normal user token al
POST /api/Users/login
{
  "email": "user@example.com", 
  "password": "User123!"
}

# Role management endpoint'lerini test et (403 Forbidden dönmeli)
GET /api/RoleManagement
Authorization: Bearer {user_token}
```

## ⚠️ Önemli Notlar

1. **Policy İsimleri**: `nameof()` kullanarak compile-time safety sağlıyoruz
2. **Role İsimleri**: Sabit string'ler yerine constants kullanıyoruz
3. **Endpoint Seviyesi**: Tüm endpoint grubu için policy uygulanır
4. **Method Seviyesi**: Tek tek method'lar için de policy uygulanabilir
5. **Multiple Roles**: Bir policy birden fazla role gerektirebilir

## 🎯 Sonuç

Artık Role Management endpoint'leri sadece **Administrator** role'üne sahip kullanıcılar tarafından erişilebilir. Diğer endpoint'ler için de benzer şekilde farklı authorization politikaları uygulayabilirsiniz.


