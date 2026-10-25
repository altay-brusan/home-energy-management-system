# 🔐 Güncellenmiş Authorization Matrisi

## 📋 Yapılan Değişiklikler

### ✅ 1. Yeni Policy Eklendi
```csharp
// Setup Management - Admin, MobilUser
public const string SetupManagement = nameof(SetupManagement);
```

### ✅ 2. Policy Konfigürasyonu
```csharp
// Setup Management - Admin, MobilUser
options.AddPolicy(Policies.SetupManagement, policy => 
    policy.RequireRole(Roles.Administrator, Roles.MobilUser));
```

### ✅ 3. Endpoint Authorization'ları

#### **Setups Endpoint**
- **GET /api/Setups** - `[Authorize(Policies.SetupManagement)]`
- **GET /api/Setups/{id}** - `[Authorize(Policies.SetupManagement)]`
- **POST /api/Setups** - `[Authorize(Policies.SetupManagement)]`
- **PUT /api/Setups/{id}** - `[Authorize(Policies.SetupManagement)]`
- **DELETE /api/Setups/{id}** - `[Authorize(Policies.AdminOnly)]`

#### **Sites Endpoint**
- **GET /api/Sites** - `[Authorize(Policies.SiteManagement)]`
- **GET /api/Sites/user/{userId}** - `[Authorize(Policies.SiteManagement)]`
- **GET /api/Sites/{id}** - `[Authorize(Policies.SiteManagement)]`
- **POST /api/Sites** - `[Authorize(Policies.SiteManagement)]`
- **PUT /api/Sites/{id}** - `[Authorize(Policies.SiteManagement)]`
- **DELETE /api/Sites/{id}** - `[Authorize(Policies.AdminOnly)]`

## 📊 Tam Authorization Matrisi

| Endpoint | Administrator | MobilUser | TechnicalServiceUser | DashboardUser |
|----------|---------------|-----------|---------------------|---------------|
| **🔧 DEVICES** | | | | |
| GET /api/Devices | ✅ | ✅ | ✅ | ✅ |
| GET /api/Devices/{id} | ✅ | ✅ | ✅ | ✅ |
| POST /api/Devices | ✅ | ✅ | ✅ | ❌ |
| PUT /api/Devices/{id} | ✅ | ✅ | ✅ | ❌ |
| DELETE /api/Devices/{id} | ✅ | ❌ | ❌ | ❌ |
| **🏠 SITES** | | | | |
| GET /api/Sites | ✅ | ✅ | ❌ | ✅ |
| GET /api/Sites/user/{userId} | ✅ | ✅ | ❌ | ✅ |
| GET /api/Sites/{id} | ✅ | ✅ | ❌ | ✅ |
| POST /api/Sites | ✅ | ✅ | ❌ | ✅ |
| PUT /api/Sites/{id} | ✅ | ✅ | ❌ | ✅ |
| DELETE /api/Sites/{id} | ✅ | ❌ | ❌ | ❌ |
| **⚙️ SETUPS** | | | | |
| GET /api/Setups | ✅ | ✅ | ❌ | ❌ |
| GET /api/Setups/{id} | ✅ | ✅ | ❌ | ❌ |
| POST /api/Setups | ✅ | ✅ | ❌ | ❌ |
| PUT /api/Setups/{id} | ✅ | ✅ | ❌ | ❌ |
| DELETE /api/Setups/{id} | ✅ | ❌ | ❌ | ❌ |
| **👥 ROLE MANAGEMENT** | | | | |
| GET /api/RoleManagement | ✅ | ❌ | ❌ | ❌ |
| POST /api/RoleManagement | ✅ | ❌ | ❌ | ❌ |
| DELETE /api/RoleManagement/{roleName} | ✅ | ❌ | ❌ | ❌ |
| PUT /api/RoleManagement/assign | ✅ | ❌ | ❌ | ❌ |
| GET /api/RoleManagement/user/{userId} | ✅ | ❌ | ❌ | ❌ |

## 🎯 Role Özetleri

### **Administrator** 🔑
- **Tüm işlemleri yapabilir**
- Sistem yönetimi
- Role atama
- Silme işlemleri

### **MobilUser** 📱
- Device okuma/yazma
- Site okuma/yazma
- Setup okuma/yazma
- Silme işlemleri yapamaz

### **TechnicalServiceUser** 🔧
- Device okuma/yazma
- Diğer endpoint'lere erişim yok
- Silme işlemleri yapamaz

### **DashboardUser** 📊
- Device okuma
- Site okuma/yazma
- Setup'lara erişim yok
- Silme işlemleri yapamaz

## 🧪 Test Senaryoları

### ✅ MobilUser Token ile Test
```bash
# Setup endpoint'leri
GET /api/Setups                    # ✅ 200 OK
POST /api/Setups                   # ✅ 201 Created
PUT /api/Setups/{id}               # ✅ 204 No Content
DELETE /api/Setups/{id}            # ❌ 403 Forbidden

# Site endpoint'leri  
GET /api/Sites                     # ✅ 200 OK
POST /api/Sites                    # ✅ 201 Created
PUT /api/Sites/{id}                # ✅ 204 No Content
DELETE /api/Sites/{id}             # ❌ 403 Forbidden
```

### ❌ TechnicalServiceUser Token ile Test
```bash
# Setup endpoint'leri
GET /api/Setups                    # ❌ 403 Forbidden
POST /api/Setups                   # ❌ 403 Forbidden

# Site endpoint'leri
GET /api/Sites                     # ❌ 403 Forbidden
POST /api/Sites                    # ❌ 403 Forbidden
```

### ❌ DashboardUser Token ile Test
```bash
# Setup endpoint'leri
GET /api/Setups                    # ❌ 403 Forbidden
POST /api/Setups                   # ❌ 403 Forbidden

# Site endpoint'leri
GET /api/Sites                     # ✅ 200 OK
POST /api/Sites                    # ✅ 201 Created
```

## 🎯 Sonuç

Artık sisteminizde:
- **Setup işlemleri**: Sadece Admin ve MobilUser yapabilir
- **Site işlemleri**: Admin, MobilUser ve DashboardUser yapabilir
- **Device işlemleri**: Admin, MobilUser ve TechnicalServiceUser yapabilir
- **Silme işlemleri**: Sadece Admin yapabilir

Bu şekilde her role'ün kendi yetki seviyesine göre işlem yapabilmesi sağlanmıştır! 🎉


