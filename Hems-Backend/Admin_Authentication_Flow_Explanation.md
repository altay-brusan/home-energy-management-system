# 🔐 Admin Olup Olmadığını Nasıl Anlıyor? - Detaylı Açıklama

## 📋 Sistemin Çalışma Mantığı

### 1. 🎫 JWT Token İçinde Role Bilgisi

ASP.NET Core Identity sistemi, kullanıcı login olduğunda JWT token'ına **role bilgilerini** dahil eder.

```csharp
// Login sırasında token oluşturulurken
var user = await _userManager.FindByEmailAsync(email);
var roles = await _userManager.GetRolesAsync(user); // ["Administrator"] gibi

// Token içinde role bilgisi saklanır
{
  "sub": "user-id-123",
  "email": "admin@example.com", 
  "role": "Administrator",        // ✅ Role bilgisi burada
  "exp": 1234567890
}
```

### 2. 🔍 Authorization Middleware Kontrolü

Her API isteğinde sistem şu adımları takip eder:

```csharp
// 1. Token'ı parse eder
var token = request.Headers["Authorization"].Replace("Bearer ", "");

// 2. Token'dan user bilgilerini çıkarır
var claims = jwtService.ValidateToken(token);
var userRoles = claims.FindAll(ClaimTypes.Role); // ["Administrator"]

// 3. Policy kontrolü yapar
if (policy == "AdminOnly") {
    return userRoles.Contains("Administrator"); // ✅ Admin mi?
}
```

### 3. 🏗️ Infrastructure Katmanında Policy Kontrolü

```csharp
// Infrastructure/DependencyInjection.cs
builder.Services.AddAuthorization(options =>
{
    // AdminOnly policy'si Administrator role'ü gerektirir
    options.AddPolicy(Policies.AdminOnly, policy => 
        policy.RequireRole(Roles.Administrator)); // ✅ Burada kontrol edilir
});
```

### 4. 🎯 Endpoint Seviyesinde Uygulama

```csharp
// RoleManagement.cs
public override void Map(WebApplication app)
{
    app.MapGroup(this)
        .RequireAuthorization(Policies.AdminOnly)  // ✅ Bu policy kontrol edilir
        .MapGet(GetRoles)
        .MapPost(CreateRole);
}
```

## 🔄 Detaylı Akış

### Adım 1: Login İşlemi
```bash
POST /api/Users/login
{
  "email": "admin@example.com",
  "password": "<your-password>"
}

# Response: JWT Token (role bilgisi dahil)
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "..."
}
```

### Adım 2: API İsteği
```bash
GET /api/RoleManagement
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Adım 3: Sistem Kontrolü
```csharp
// 1. Token parse edilir
var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
var claims = jwtService.ValidateToken(token);

// 2. Role bilgisi çıkarılır
var userRoles = claims.FindAll(ClaimTypes.Role); // ["Administrator"]

// 3. Policy kontrolü
var policy = "AdminOnly";
var requiredRole = "Administrator";
var isAuthorized = userRoles.Contains(requiredRole); // true ✅

// 4. Sonuç
if (isAuthorized) {
    return Ok(data); // ✅ İstek başarılı
} else {
    return Forbid(); // ❌ 403 Forbidden
}
```

## 🧪 Test Senaryoları

### ✅ Admin Kullanıcı Testi
```bash
# 1. Admin kullanıcı ile login
POST /api/Users/login
{
  "email": "admin@example.com",
  "password": "<your-password>"
}

# 2. Role management endpoint'ini çağır
GET /api/RoleManagement
Authorization: Bearer {admin_token}

# Sonuç: 200 OK ✅
```

### ❌ Normal Kullanıcı Testi
```bash
# 1. Normal kullanıcı ile login
POST /api/Users/login
{
  "email": "user@example.com",
  "password": "User123!"
}

# 2. Role management endpoint'ini çağır
GET /api/RoleManagement
Authorization: Bearer {user_token}

# Sonuç: 403 Forbidden ❌
```

## 🔧 Role Atama İşlemi

### Admin Role'ü Nasıl Atanır?

```csharp
// IdentityService.cs - AssignRoleToUserAsync method
public async Task<Result> AssignRoleToUserAsync(string userId, string roleName)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Result.Failure(new[] { "User not found." });
    }

    // ✅ Kullanıcıya role atanır
    var result = await _userManager.AddToRoleAsync(user, roleName);
    return result.ToApplicationResult();
}
```

### Manuel Role Atama
```bash
# Admin olan bir kullanıcı ile
POST /api/RoleManagement/assign
{
  "userId": "user-id-123",
  "roleName": "Administrator"
}
```

## 🎯 Özet

1. **Login**: Kullanıcı login olduğunda JWT token'ına role bilgisi eklenir
2. **Token**: Her API isteğinde token gönderilir
3. **Parse**: Sistem token'ı parse eder ve role bilgisini çıkarır
4. **Policy**: AdminOnly policy'si Administrator role'ü gerektirir
5. **Kontrol**: Sistem kullanıcının Administrator role'üne sahip olup olmadığını kontrol eder
6. **Sonuç**: Admin ise erişim verilir, değilse 403 Forbidden döner

## 🔍 Debug İçin

Token içeriğini görmek için:
```csharp
// CurrentUser.cs'de
public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

// Role bilgisini görmek için
public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
```

Bu şekilde sistem admin olup olmadığını anlıyor! 🎉


