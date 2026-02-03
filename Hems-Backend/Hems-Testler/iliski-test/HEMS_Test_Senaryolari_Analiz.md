# HEMS Backend - Test Senaryoları Analiz Raporu

**Proje:** HEMS (Home Energy Management System) Backend
**Test Edilen Modül:** Invitation & Site Sharing
**Tarih:** 2026-01-06
**Tester:** Test Ekibi
**Branch:** stage

---

## 📋 Test Özeti

**Test Edilen Kategori:** CRITICAL Öncelikli Testler
**Toplam Test:** 2
**Geçen:** 1 (Kısmi)
**Kalan:** 1 (Kritik Güvenlik Açığı)

---

## 🔴 CRITICAL TESTLER

### TEST 1: BUG-001 - GetUserStatus Guest Kullanıcı Kontrolü

**Test ID:** BUG-001
**Kategori:** GetUserStatus / User Onboarding
**Öncelik:** 🔴 CRITICAL

#### 📝 Test Senaryosu
1. UserA bir site oluşturur
2. UserA, UserB'yi site'a davet eder (email ile invitation)
3. UserB daveti kabul eder
4. UserB login olur ve GetUserStatus çağrılır

#### ✅ Beklenen Sonuç
- UserB'nin shared site bilgisi dönmeli
- Status: `Complete` olmalı
- UserB dashboard'a yönlendirilmeli

#### 🔍 Kod İncelemesi

**Dosya:** `src/Application/Auth/Queries/GetUserStatus/GetUserStatus.cs:26-27`

**Kod:**
```csharp
var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, cancellationToken);
```

**Sorun:**
- Kod sadece `Sites.UserId == userId` kontrol ediyor
- **UserSites** tablosuna hiç bakmıyor
- Guest kullanıcı için shared site bulunamıyor
- TODO yorumu var ama yapılmamış (satır 24: "SİTE ID EKLENSİN NULL OLABİLİR")

**Mantık Hatası:**
```
❌ Mevcut: Sadece kullanıcının kendi oluşturduğu siteleri buluyor
✅ Olmalı: Kullanıcının hem oluşturduğu hem de paylaşılan siteleri bulmalı
```

#### 🧪 Manuel Test Sonucu

**Test Durumu:** ✅ **UYGULAMA ÇALIŞIYOR **

**Test Detayı:**
- UserB davet kabul ediyor
- Site'ı görebiliyor ve dashboard'a erişebiliyor
- Uygulama beklendiği gibi çalışıyor

**Açıklama:**
Frontend **farklı endpoint kullanıyor**: `Invitations` endpoint'i üzerinden site bilgilerini alıyor, GetUserStatus'e güvenmiyor.

**Sonuç:**
- ✅ Uygulama çalışıyor (frontend başka yoldan hallediyor)
- ❌ Kod mantık hatası var
- ⚠️ GetUserStatus endpoint'i yanlış sonuç dönüyor
- 🔧 Kod düzeltilmeli (ileride farklı client'lar sorun yaşayabilir)

**Öneri:**
```csharp
// Düzeltilmiş kod:
var site = await _context.Sites
    .FirstOrDefaultAsync(s =>
        (s.UserId == userId ||
         _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == userId))
        && !s.IsDeleted,
        cancellationToken);
```

**Öncelik:** MEDIUM (uygulama çalıştığı için kritiklik düştü, ama yine de düzeltilmeli)

---

### TEST 2: BUG-002 - UpdateSite Authorization Kontrolü

**Test ID:** BUG-002
**Kategori:** Authorization & Security
**Öncelik:** 🔴 CRITICAL

#### 📝 Test Senaryosu
1. UserA bir site oluşturur (SiteA)
2. UserA, UserB'yi Guest olarak SiteA'ya ekler
3. UserB (Guest), SiteA'yı güncellemeye çalışır

#### ✅ Beklenen Sonuç
- Authorization hatası dönmeli (403 Forbidden)
- Sadece Owner (UserA) site'ı güncelleyebilmeli
- Guest kullanıcı (UserB) sadece görüntüleyebilmeli

#### 🔍 Kod İncelemesi

**Dosya:** `src/Application/Sites/Commands/UpdateSite/UpdateSite.cs:30-49`

**Kod:**
```csharp
public async Task Handle(UpdateSiteCommand request, CancellationToken cancellationToken)
{
    var entity = await _context.Sites
        .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

    if (entity == null)
    {
        throw new NotFoundException(nameof(Site), request.Id);
    }

    // Direkt güncelleme yapılıyor
    entity.Name = request.Name;
    entity.Country = request.Country;
    // ... diğer alanlar

    await _context.SaveChangesAsync(cancellationToken);
}
```

**Kritik Sorun:**
- ❌ **HİÇBİR AUTHORIZATION KONTROLÜ YOK!**
- ❌ `entity.UserId == currentUserId` kontrolü yok
- ❌ Guest role kontrolü yok
- ❌ Admin kontrolü bile yok
- ✅ Sadece "Site var mı?" kontrolü yapılıyor

**Güvenlik Açığı:**
```
Herhangi bir kullanıcı → Herhangi bir site'ı güncelleyebilir!
```

#### 🧪 Manuel Test Sonucu

**Test Durumu:** ❌ **KRİTİK GÜVENLİK AÇIĞI DOĞRULANDI!**

**Test Detayı:**
UserB (Guest kullanıcı) şunları yapabiliyor:
1. ✅ Site bilgilerini değiştirebiliyor (isim, konum, vs.)
2. ✅ Cihaz ayarlarını güncelleyebiliyor
3. ✅ **UserA'yı (Owner'ı) bile silebiliyor!**

**Etki Analizi:**
- 🚨 **Kritik Güvenlik Açığı:** Herhangi bir kullanıcı başkasının site'ını kontrol edebilir
- 🚨 **Data Integrity Riski:** Guest kullanıcı Owner'ı silebiliyor
- 🚨 **Yetkilendirme Bypass:** Tüm site yönetimi açık

**Gerçek Dünya Senaryosu:**
```
UserA: "Benim evimin güneş panellerini UserB yönetsin, sadece izlesin"
UserB: Girer → Site'yi siler, cihaz ayarlarını değiştirir, UserA'yı çıkarır
UserA: Artık kendi sistemine erişemez! 😱
```

**Öncelik:** 🔴 **ACİL - HEMEN DÜZELTİLMELİ!**

#### 🔧 Düzeltme Önerisi

```csharp
public async Task Handle(UpdateSiteCommand request, CancellationToken cancellationToken)
{
    var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

    var entity = await _context.Sites
        .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

    if (entity == null)
    {
        throw new NotFoundException(nameof(Site), request.Id);
    }

    // AUTHORIZATION KONTROLÜ
    var isAdmin = await _identityService.IsInRoleAsync(userId, Roles.Administrator);

    if (!isAdmin && entity.UserId != userId)
    {
        throw new UnauthorizedAccessException("Sadece site sahibi güncelleyebilir.");
    }

    // GUEST KONTROLÜ (ilave güvenlik katmanı)
    var userSite = await _context.UserSites
        .FirstOrDefaultAsync(us => us.SiteId == request.Id && us.UserId == userId, cancellationToken);

    if (userSite != null && userSite.Role == UserSiteRole.Guest)
    {
        throw new UnauthorizedAccessException("Guest kullanıcılar site güncelleyemez.");
    }

    // Artık güvenli şekilde güncelleme yapılabilir
    entity.Name = request.Name;
    // ...
    await _context.SaveChangesAsync(cancellationToken);
}
```

---

### TEST 3: BUG-003 - UpdateDevice Endpoint Policy Kontrolü

**Test ID:** BUG-003
**Kategori:** Authorization Policy
**Öncelik:** 🟠 HIGH

#### 📝 Test Senaryosu
1. Admin bir device oluşturur
2. Normal UserA login olur (user token alır)
3. UserA, device'ı UpdateDevice endpoint'i ile güncellemeye çalışır

#### ✅ Beklenen Sonuç
- 403 Forbidden dönmeli
- Endpoint `[Authorize(Policies.AdminOnly)]` ile korunmuş
- Sadece Admin güncelleyebilmeli

#### 🔍 Kod İncelemesi

**Endpoint:** `src/Web/Endpoints/Devices.cs:63-72`

**Kod:**
```csharp
[Authorize(Policies.AdminOnly)]
public async Task<IResult> UpdateDevice(
    ISender sender,
    Guid id,
    UpdateDeviceCommand command)
{
    if (id != command.Id) return Results.BadRequest();
    await sender.Send(command);
    return Results.NoContent();
}
```

**Handler:** `src/Application/Devices/Commands/UpdateDevice/UpdateDevice.cs:40-83`

**Durum:**
- ✅ Endpoint'te `[Authorize(Policies.AdminOnly)]` attribute var (satır 63)
- ✅ Handler'da hiçbir kontrol yok (sadece device bulup güncellüyor)
- ⚠️ Policy runtime'da çalışıyor mu **TEST EDİLMELİ**

**Analiz:**
```
Endpoint Seviyesi: ✅ Policy koruması var
Handler Seviyesi:  ❓ Kontrol yok (ama endpoint zaten korumalı)
Runtime Test:      ❓ Policy gerçekten çalışıyor mu test edilmeli
```

#### 🧪 Manuel Test Gerekli

**Test Durumu:** ⏳ **FİZİKSEL TEST BEKLİYOR**

**Yapılacak Test:**
1. Normal user token al (Admin olmayan)
2. Postman ile `PUT /api/devices/{id}` endpoint'ine istek at
3. Authorization header'da normal user token gönder
4. Sonucu kontrol et

**Beklenen Sonuçlar:**
- ✅ Eğer policy çalışıyorsa: 403 Forbidden
- ❌ Eğer policy çalışmıyorsa: 204 No Content (güncelleme başarılı - BUG!)

**Öncelik:** HIGH - Runtime policy testi yapılmalı

**Açıklama:**
Endpoint attribute var ancak **runtime'da policy gerçekten çalışıyor mu?** test edilmeli. BUG-002'de UpdateSite'da authorization kontrolü yoktu, burada endpoint korumalı ama çalıştığından emin olmak gerekiyor.

---

### TEST 3: BUG-003 - UpdateDevice Endpoint Policy Kontrolü

**Test ID:** BUG-003
**Kategori:** Authorization Policy
**Öncelik:** 🟠 HIGH

#### 📝 Test Senaryosu
1. Admin bir device oluşturur
2. Normal user (Admin olmayan) login olur
3. Normal user device'ı güncellemeye çalışır

#### ✅ Beklenen Sonuç
- Sadece Admin device oluşturabilmeli
- Device ayarlarını (DeviceSettings) site sahibi ve guest güncelleyebilmeli
- Device entity'sini sadece Admin güncelleyebilmeli

#### 🔍 Kod İncelemesi

**Endpoint:** `src/Web/Endpoints/Devices.cs:63-72`

```csharp
[Authorize(Policies.AdminOnly)]
public async Task<IResult> UpdateDevice(
    ISender sender,
    Guid id,
    UpdateDeviceCommand command)
{
    if (id != command.Id) return Results.BadRequest();
    await sender.Send(command);
    return Results.NoContent();
}
```

**Handler:** `src/Application/Devices/Commands/UpdateDevice/UpdateDevice.cs:40-83`

**Durum:**
- ✅ Endpoint `[Authorize(Policies.AdminOnly)]` ile korumalı
- ✅ Sadece Admin device oluşturabilir
- ✅ Device ayarları ayrı endpoint (DeviceSettings) üzerinden güncellenebilir

#### 🧪 Manuel Test Sonucu

**Test Durumu:** ✅ **Test Geçti - Beklenen Davranış**

**Test Detayı:**
- ✅ Admin device oluşturabiliyor
- ✅ Site sahibi (normal user) device ayarlarını güncelleyebiliyor
- ✅ Guest kullanıcı device ayarlarını güncelleyebiliyor
- ✅ Sadece izin verilen device ayarları güncellenebiliyor

**Açıklama:**
- Device entity güncellemesi (UpdateDevice) AdminOnly policy ile korumalı
- Device ayarları güncellemesi (DeviceSettings endpoint) farklı authorization kuralları kullanıyor
- Site sahibi ve guest kullanıcılar sadece device parametrelerini değiştirebilir
- Device'ın kendisine (Manufacturer, ModelNumber, SerialNumber vb.) karışamazlar

**Sonuç:** Beklenen şekilde çalışıyor, sorun yok.


### TEST 4: BUG-004 - Admin Kullanıcı İçin GetUserStatus Davranışı

**Test ID:** BUG-004
**Kategori:** GetUserStatus / Business Logic
**Öncelik:** MEDIUM

#### Test Senaryosu
Admin kullanıcı login olur ve GetUserStatus endpoint'ini çağırır.

#### Beklenen Sonuç
Admin için özel davranış belirlenmeli:
- Admin site oluşturmamalı, sadece device oluşturmalı
- GetUserStatus admin için farklı response dönmeli veya kullanılmamalı
- Admin tüm sahaları görebilmeli ama kendi sahası olmayabilir

#### Kod İncelemesi

**Dosya:** `src/Application/Auth/Queries/GetUserStatus/GetUserStatus.cs:21-96`

**Kod:**
```csharp
public async Task<UserStatusDto> Handle(GetUserStatusQuery request, CancellationToken cancellationToken)
{
    var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

    var site = await _context.Sites
        .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, cancellationToken);

    if (site == null)
    {
        return new UserStatusDto
        {
            Status = UserStatusType.NeedsSite,
            RedirectTo = "create_site",
            Message = "Site oluşturmanız gerekiyor"
        };
    }
    // ...
}
```

**Sorun:**
- Admin role kontrolü yok
- Admin normal user gibi işleniyor
- Admin için özel akış yok

#### Manuel Test Sonucu

**Test Durumu:** ⚠️ Business Logic Kararı Gerekli

**Test Detayı:**
Admin login → GetUserStatus çağrıldı → Response:
```json
{
  "status": 3,
  "redirectTo": "create_setup",
  "message": "Device bağlamanız gerekiyor",
  "data": {
    "siteId": "e0c5d668-8dbe-4e65-89e3-58e908336107",
    "siteName": "Default Site",
    "setupId": "9dfa272b-e15b-48f0-bb3c-0dd2255897e5",
    "setupName": "Default Setup",
    "deviceId": null,
    "deviceName": null
  }
}
```

**Analiz:**
- Admin için otomatik "Default Site" oluşturulmuş
- GetUserStatus admin'i normal user gibi işliyor
- "Device bağlamanız gerekiyor" mesajı anlamsız (admin device eklemez, oluşturur)

**Sistem Mantığı:**
- Admin ThingsBoard'da device oluşturur
- Kullanıcılar kendi sahalarına seri numarasıyla device ekler
- Admin tüm sahaları görebilir, debug açık

**Sonuç:**
GetUserStatus endpoint'i admin için gereksiz. Admin farklı bir akış kullanmalı veya endpoint admin kontrolü yapmalı.

#### Düzeltme Önerisi

**Seçenek 1:** Admin için özel response
```csharp
// Admin kontrolü ekle
var isAdmin = await _identityService.IsInRoleAsync(userId, Roles.Administrator);

if (isAdmin)
{
    return new UserStatusDto
    {
        Status = UserStatusType.Complete,
        RedirectTo = "admin_panel",
        Message = "Admin paneline yönlendiriliyorsunuz"
    };
}
```

**Seçenek 2:** Admin GetUserStatus kullanmamalı
- Mobil app admin için farklı endpoint çağırmalı
- GetUserStatus sadece normal user için

---

## SEC Testleri

### TEST 5: SEC-001 - Token Collision Riski

**Test ID:** SEC-001
**Kategori:** Security / Token Generation
**Öncelik:** MEDIUM

#### Test Senaryosu
6 haneli token kullanılıyor. Aynı anda 10,000+ davetiye gönderilirse token çakışması olabilir mi?

#### Beklenen Sonuç
Token duplicate kontrolü yapılmalı, çakışma durumunda yeni token üretilmeli.

#### Kod İncelemesi

**Dosya:** `src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:115-135`

**Kod:**
```csharp
private async Task<string> GenerateUniqueTokenAsync(CancellationToken cancellationToken)
{
    var random = new Random();
    var twentyFourHoursAgo = _timeProvider.GetUtcNow().AddHours(-24);

    string token;
    bool isUnique;

    do
    {
        token = random.Next(100000, 999999).ToString();

        // Son 24 saatte bu token kullanılmış mı kontrol et
        isUnique = !await _context.Invitations
            .AnyAsync(i => i.Token == token && i.Created > twentyFourHoursAgo, cancellationToken);

    } while (!isUnique);

    return token;
}
```

#### Analiz

**Test Durumu:** ✅ Kod Doğru

**Güvenlik Mekanizması:**
- Token aralığı: 100000-999999 (900,000 olasılık)
- Do-while loop ile duplicate kontrolü
- Son 24 saatte kullanılmış token varsa yeni token üretiliyor
- Unique bulana kadar denemeye devam ediyor

**Risk Analizi:**
- 900,000 olası token
- Birthday paradox: 1000 token için collision riski düşük
- 10,000 token için collision ihtimali var ancak kod bunu engelliyor

**Sonuç:** Token collision riski kod tarafından yönetiliyor, manuel test gerekli değil.

---

### TEST 6: SEC-002 - Expired Invitation Kontrolü

**Test ID:** SEC-002
**Kategori:** Security / Invitation Expiry
**Öncelik:** LOW

#### Test Senaryosu
24 saatten eski bir invitation token ile kabul etmeye çalışılıyor.

#### Beklenen Sonuç
Expired hatası dönmeli, invitation kabul edilmemeli.

#### Kod İncelemesi

**Dosya:** `src/Application/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs:54-60`

**Kod:**
```csharp
// Check if invitation has expired (24 hours)
if (invitation.Created < twentyFourHoursAgo)
{
    invitation.Status = InvitationStatus.Expired;
    await _context.SaveChangesAsync(cancellationToken);
    throw new InvalidOperationException("This invitation has expired. Please request a new invitation.");
}
```

#### Analiz

**Test Durumu:** ✅ Kod Doğru

**Kontrol Mekanizması:**
- 24 saat kontrolü var
- Expired durumunda status `InvitationStatus.Expired` olarak güncelleniyor
- Hata mesajı açık ve anlaşılır
- Database'de status değişikliği kaydediliyor

**Sonuç:** Expiry kontrolü mevcut, kod doğru çalışıyor. Manuel test gerekli değil.

---

### TEST 7: SEC-003 - Aynı Kullanıcıya Birden Fazla Pending Davet

**Test ID:** SEC-003
**Kategori:** Security / Duplicate Invitation
**Öncelik:** MEDIUM

#### Test Senaryosu
UserA → UserB'ye davet gönderir (Status: Pending)
UserA → Tekrar UserB'ye davet göndermeye çalışır (hala Pending)

#### Beklenen Sonuç
İkinci davet engellanmeli, "Zaten bekleyen davetiye var" hatası dönmeli.

#### Kod İncelemesi

**Dosya:** `src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:54-65`

**Kod:**
```csharp
var existingInvitation = await _context.Invitations
    .FirstOrDefaultAsync(i =>
        i.Email == request.Email &&
        i.SiteId == request.SiteId &&
        i.Status == InvitationStatus.Pending,
        cancellationToken);

if (existingInvitation != null)
{
    throw new InvalidOperationException("Bu kullanıcıya bu ev için zaten bekleyen bir davetiye var.");
}
```

#### Analiz

**Test Durumu:** ✅ Kod Doğru

**Kontrol Mekanizması:**
- Pending status kontrolü var
- Aynı email + aynı site için duplicate pending engelleniyor
- Hata mesajı açık

**SEC-004 ile Farkı:**
- SEC-003: İkisi de Pending (bu engelleniyor ✅)
- SEC-004: Biri Accepted, diğeri yeni (bu engellenmiyor ❌)

**Sonuç:** Pending duplicate kontrolü çalışıyor, kod doğru. Manuel test gerekli değil.

---
### TEST 4: SEC-004 - Duplicate UserSite Riski

**Test ID:** SEC-004
**Kategori:** Security / Data Integrity
**Öncelik:** 🟠 HIGH

#### 📝 Test Senaryosu
1. UserA, UserB'yi SiteA'ya davet eder
2. UserB daveti kabul eder (Status: Accepted, UserSites'a eklenir)
3. UserA tekrar UserB'yi SiteA'ya davet eder
4. UserB tekrar kabul etmeye çalışır

#### ✅ Beklenen Sonuç
- CreateInvitation: UserSites kontrolü yapmalı, kabul edilmiş kullanıcıya tekrar davet göndermemeli
- AcceptInvitation: Duplicate UserSite oluşumunu engellemeli
- Database'de duplicate kayıt oluşmamalı

#### 🔍 Kod İncelemesi

**CreateInvitation:** `src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:54-65`

```csharp
var existingInvitation = await _context.Invitations
    .FirstOrDefaultAsync(i =>
        i.Email == request.Email &&
        i.SiteId == request.SiteId &&
        i.Status == InvitationStatus.Pending,
        cancellationToken);

if (existingInvitation != null)
{
    throw new InvalidOperationException("Bu kullanıcıya bu ev için zaten bekleyen bir davetiye var.");
}
```

**Sorun:**
- Sadece `InvitationStatus.Pending` kontrolü yapılıyor
- `UserSites` tablosuna bakılmıyor
- Accepted/Declined durumundaki davetler kontrol edilmiyor

**AcceptInvitation:** `src/Application/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs:72-79`

```csharp
var existingUserSite = await _context.UserSites
    .FirstOrDefaultAsync(us => us.UserId == currentUserId && us.SiteId == invitation.SiteId, cancellationToken);

if (existingUserSite != null)
{
    throw new InvalidOperationException("Bu site zaten sizinle paylaşılmış.");
}
```

**Durum:**
- ✅ `UserSites` duplicate kontrolü var

#### 🧪 Manuel Test Sonucu

**Test Durumu:** ⚠️ **Kısmi Sorun - CreateInvitation Zayıf**

**Test Detayı:**

**Test 1: Pending durumdayken tekrar davet**
1. UserA → UserB'ye davet gönder
2. Davet beklerken (Pending) → UserA tekrar davet gönder
3. Sonuç: ❌ Hata veriyor ("Zaten bekleyen davetiye var")

**Test 2: Accepted durumdan sonra tekrar davet**
1. UserA → UserB'ye davet gönder
2. UserB → Daveti kabul eder (Status: Accepted, UserSites'a eklenir)
3. UserA → Tekrar davet gönder
4. Sonuç: ✅ Davet gönderilebiliyor (olmamalı!)

**Test 3: Tekrar kabul etmeye çalışma**
1. UserB → 2. daveti kabul etmeye çalışır
2. Sonuç: ❌ Failed ("Bu site zaten sizinle paylaşılmış")

**Analiz:**
```
CreateInvitation:
  - Pending kontrol: ✅ Çalışıyor
  - Accepted kontrol: ❌ Yok
  - UserSites kontrol: ❌ Yok

AcceptInvitation:
  - UserSites kontrol: ✅ Çalışıyor
  - Duplicate engelleme: ✅ Çalışıyor
```

**Sorun:**
- UserA kabul edilmiş kullanıcıya tekrar davet gönderebiliyor
- Database'de gereksiz `Invitation` kayıtları birikiyor
- Kullanıcı deneyimi kötü (gereksiz email gidiyor)

**Sonuç:**
- ✅ Duplicate UserSite oluşmuyor (AcceptInvitation engelliyor)
- ❌ Spam invitation oluşturulabiliyor (CreateInvitation zayıf)

#### 🔧 Düzeltme Önerisi

**CreateInvitation'a UserSites kontrolü eklenmeli:**

```csharp
// Email ile kullanıcı ara
var invitedUserId = await _identityService.GetUserIdByEmailAsync(request.Email);

// UserSites kontrolü ekle
if (!string.IsNullOrEmpty(invitedUserId))
{
    var existingUserSite = await _context.UserSites
        .AnyAsync(us => us.SiteId == request.SiteId && us.UserId == invitedUserId, cancellationToken);

    if (existingUserSite)
    {
        throw new InvalidOperationException("Bu kullanıcı zaten bu site'e erişime sahip.");
    }
}

// Pending invitation kontrolü
var existingInvitation = await _context.Invitations
    .FirstOrDefaultAsync(i =>
        i.Email == request.Email &&
        i.SiteId == request.SiteId &&
        i.Status == InvitationStatus.Pending,
        cancellationToken);
```

---

### TEST 9: SEC-005 - Kendine Davet Gönderme

**Test ID:** SEC-005
**Kategori:** Security / Invitation Validation
**Öncelik:** MEDIUM

#### Test Senaryosu
UserA kendi email adresine davet gönderiyor.

#### Beklenen Sonuç
Kendine davet gönderilememeli, hata dönmeli.

#### Kod İncelemesi

**Dosya:** `src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:38-113`

**Kod:**
```csharp
var currentUserId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

// Email ile kullanıcı ara
var invitedUserId = await _identityService.GetUserIdByEmailAsync(request.Email);

// Pending invitation kontrolü
var existingInvitation = await _context.Invitations
    .FirstOrDefaultAsync(i =>
        i.Email == request.Email &&
        i.SiteId == request.SiteId &&
        i.Status == InvitationStatus.Pending,
        cancellationToken);
```

**Sorun:**
- `invitedUserId` alınıyor
- `currentUserId` var
- ❌ `invitedUserId == currentUserId` kontrolü yok
- Kullanıcı kendi emailine davet gönderebiliyor

#### Manuel Test Sonucu

**Test Durumu:** ❌ Kontrol Yok

**Test Detayı:**
UserA kendi email adresine davet gönderiyor → ✅ Gönderilebiliyor (olmamalı)

**Sonuç:** Kendine davet gönderme kontrolü yok, eklenmeli.

#### Düzeltme Önerisi

```csharp
// Email ile kullanıcı ara
var invitedUserId = await _identityService.GetUserIdByEmailAsync(request.Email);

// Kendine davet kontrolü
if (!string.IsNullOrEmpty(invitedUserId) && invitedUserId == currentUserId)
{
    throw new InvalidOperationException("Kendinize davet gönderemezsiniz.");
}
```

---

### TEST 10: SEC-006 - Guest Kullanıcı Davet Gönderebiliyor

**Test ID:** SEC-006
**Kategori:** Security / Authorization
**Öncelik:** HIGH

#### Test Senaryosu
1. UserA bir site oluşturur
2. UserA → UserB'yi Guest olarak ekler
3. UserB (Guest) → UserC'yi davet etmeye çalışır

#### Beklenen Sonuç
Sadece Owner davet gönderebilmeli, Guest davet gönderememeli.

#### Kod İncelemesi

**Dosya:** `src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:42-49`

**Kod:**
```csharp
var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.Id == request.SiteId &&
                             !s.IsDeleted &&
                             (s.UserId == currentUserId ||
                              _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == currentUserId)),
                         cancellationToken)
```

**Sorun:**
- `s.UserId == currentUserId` → Owner kontrolü ✅
- `UserSites.Any()` → Guest de geçiyor ❌
- Role kontrolü yok (Owner/Guest ayırımı yapılmıyor)

#### Manuel Test Sonucu

**Test Durumu:** ❌ Guest Davet Gönderebiliyor

**Test Detayı:**
UserB (Guest) → UserC'ye davet göndermeye çalışıyor → ✅ Gönderilebiliyor (olmamalı)

**Sonuç:** Guest kullanıcı davet gönderebiliyor, sadece Owner gönderebilmeli.

#### Düzeltme Önerisi

```csharp
// Sadece site Owner'ı davet gönderebilmeli
var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.Id == request.SiteId &&
                             !s.IsDeleted &&
                             s.UserId == currentUserId,  // UserSites kontrolünü kaldır
                         cancellationToken)
    ?? throw new NotFoundException(nameof(Site), request.SiteId);
```

---

### TEST 11: SEC-007 - DeleteSite Authorization

**Test ID:** SEC-007
**Kategori:** Authorization / Business Logic
**Öncelik:** MEDIUM

#### Test Senaryosu
Site silme yetkisi kimde olmalı?

#### Kod İncelemesi

**Dosya:** `src/Application/Sites/Commands/DeleteSite/DeleteSite.cs:24-42`

**Kod:**
```csharp
var entity = await _context.Sites
    .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId && !x.IsDeleted, cancellationToken);

if (entity == null)
{
    throw new NotFoundException(nameof(Site), request.Id);
}

// Setup kontrolü
var hasSetup = await _context.Setups
    .AnyAsync(s => s.SiteId == request.Id, cancellationToken);

if (hasSetup)
{
    throw new InvalidOperationException("Bu site'ın bir setup'ı var. Önce setup'ı silmeniz gerekiyor.");
}

entity.IsDeleted = true;
```

#### Analiz

**Test Durumu:** ⚠️ Business Logic Belirsiz

**Durum:**
- `x.UserId == userId` kontrolü var (sadece Owner)
- Setup varsa silemiyor (önce setup silinmeli)
- Owner bile kendi sitesini silemiyor (setup engeli)
- Admin kontrolü yok

**Soru:**
- Owner kendi sitesini silebilmeli mi? (Setup varken)
- Admin başkasının sitesini silebilmeli mi?
- Cascade delete mantığı nasıl olmalı?

**Sonuç:** Business logic kararı gerekli, şu anda site silme işlevi kısıtlı.

---

### TEST 12: SEC-008 - RemoveUserFromSite Authorization

**Test ID:** SEC-008
**Kategori:** Authorization / Business Logic
**Öncelik:** MEDIUM

#### Test Senaryosu
Site'den kullanıcı çıkarma yetkisi kimde olmalı?

#### Kod İncelemesi

**Dosya:** `src/Application/Invitations/Commands/RemoveUserFromSite/RemoveUserFromSite.cs:36-42`

**Kod:**
```csharp
var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.Id == invitation.SiteId &&
                             !s.IsDeleted &&
                             s.UserId == currentUserId,
                         cancellationToken)
    ?? throw new NotFoundException(nameof(Site), invitation.SiteId);
```

#### Analiz

**Test Durumu:** ⚠️ Business Logic Kararı Gerekli

**Durum:**
- `s.UserId == currentUserId` kontrolü var (sadece Owner)
- Admin kontrolü yok

**Soru:**
- Admin başkasının sitesinden user çıkarabilmeli mi?
- Guest kendini çıkarabilmeli mi?
- Owner kendini çıkarabilir mi? (Son owner kontrolü var mı?)

**Sonuç:** Kod Owner kontrolü yapıyor, admin için business logic kararı gerekli.

---

### TEST 13: SEC-009 - Guest Kullanıcı Diğer Guest'leri Görebiliyor mu?

**Test ID:** SEC-009
**Kategori:** Authorization / Privacy
**Öncelik:** MEDIUM

#### Test Senaryosu
1. UserA bir site oluşturur
2. UserA → UserB'yi ekler (Guest)
3. UserA → UserC'yi ekler (Guest)
4. UserB (Guest) diğer guest'leri (UserC) görebiliyor mu?
5. UserB invitation listesinde kendini görebiliyor mu?

#### Beklenen Sonuç
Business logic kararı:
- Guest'ler birbirini görebilmeli mi?
- Guest kendini invitation listesinde görebilmeli mi?

#### Manuel Test Sonucu

**Test Durumu:** ⚠️ Guest Diğerlerini Görebiliyor + Hata Mesajı Sorunu

**Test Detayı:**

**Test 1: Guest diğer guest'leri görebiliyor**
- UserB (Guest) → Diğer guest'leri (UserC) görebiliyor

**Test 2: Guest başka guest'i silmeye çalışıyor**
- UserB (Guest) → UserC'yi silmeye çalışıyor
- Sonuç: ❌ Failed
- Hata mesajı: Endpoint çalışmıyor gibi görünüyor (hata mesajı belirsiz)
- Beklenen: "Bu işlem için yetkiniz yok" gibi açık mesaj

**Sorun:**
- Guest'ler birbirini görebiliyor (privacy sorunu olabilir)
- Guest başka guest'i silemeye çalışınca hata mesajı belirsiz
- Authorization hatası açık ve anlaşılır olmalı

**Sonuç:**
- ❌ Guest'ler birbirini görebiliyor (business logic kararı gerekli)
- ❌ Hata mesajı düzeltilmeli (authorization hatası açık olmalı)

---

## LOGIC Testleri

### TEST 14: LOGIC-002 - Guest Kullanıcı Setup Güncelleyebilir mi?

**Test ID:** LOGIC-002
**Kategori:** Guest Role / Authorization
**Öncelik:** HIGH

#### Test Senaryosu
1. UserA bir site oluşturur ve setup ekler
2. UserB'yi Guest olarak ekle
3. UserB setup'ı güncellemeye çalışır

#### Beklenen Sonuç
Business logic kararı: Guest setup güncelleyebilmeli mi?

#### Manuel Test Sonucu

**Test Durumu:** ✅ Guest Setup Güncelleyebiliyor

**Test Detayı:**
- UserB (Guest) → Setup'ta izin verilen ayarları değiştirebiliyor
- Mode ayarları, device ayarları güncellenebiliyor
- Kısıtlı izinler var (sadece belirli ayarlar)

**Sonuç:** Guest kullanıcı setup güncelleyebiliyor, izin verilen parametreler sınırlı.

---

### TEST 15: LOGIC-004 - Guest Kullanıcı Diğer Guest'i Kaldırabilir mi?

**Test ID:** LOGIC-004
**Kategori:** Guest Role / Authorization
**Öncelik:** LOW

#### Test Senaryosu
1. UserA bir site oluşturur
2. UserA → UserB'yi Guest olarak ekler
3. UserA → UserC'yi Guest olarak ekler
4. UserB (Guest) → UserC'yi kaldırmaya çalışır

#### Beklenen Sonuç
Guest başka guest'i kaldıramamalı, sadece Owner kaldırabilmeli.

#### Kod İncelemesi

**Dosya:** `src/Application/Invitations/Commands/RemoveUserFromSite/RemoveUserFromSite.cs:36-42`

RemoveUserFromSite'da Owner kontrolü var:
```csharp
var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.Id == invitation.SiteId &&
                             !s.IsDeleted &&
                             s.UserId == currentUserId,  // Sadece Owner
                         cancellationToken)
```

#### Manuel Test Sonucu

**Test Durumu:** ✅ Kod Doğru

**Test Detayı:**
UserB (Guest) → UserC'yi kaldırmaya çalışıyor → ❌ Kaldıramadı

**Sonuç:** Guest başka guest'i kaldıramıyor, sadece Owner kaldırabiliyor. Beklenen davranış.

---

### TEST 16: LOGIC-005 - Guest Kullanıcı Site Silebilir mi?

**Test ID:** LOGIC-005
**Kategori:** Guest Role / Authorization
**Öncelik:** LOW

#### Test Senaryosu
1. UserA bir site oluşturur
2. UserA → UserB'yi Guest olarak ekler
3. UserB (Guest) → Site'ı silmeye çalışır

#### Beklenen Sonuç
Guest site silememeli, sadece Owner silebilmeli.

#### Kod İncelemesi

**Dosya:** `src/Application/Sites/Commands/DeleteSite/DeleteSite.cs:24-25`

DeleteSite'da Owner kontrolü var:
```csharp
var entity = await _context.Sites
    .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId && !x.IsDeleted, cancellationToken);
```

#### Manuel Test Sonucu

**Test Durumu:** ⚠️ Site Silme Özelliği Yok

**Test Detayı:**
- Site silme endpoint'i çalışmıyor
- Setup varsa site silinemiyor (SEC-007'de belirtildi)
- Özellik aktif değil

**Sonuç:** Site silme özelliği şu anda çalışmıyor, test edilemedi.

---

### TEST 17: LOGIC-006 - Guest Kendini Çıkarabilir mi? Owner Ne Yapabilir?

**Test ID:** LOGIC-006
**Kategori:** Guest Role / Business Logic
**Öncelik:** MEDIUM

#### Test Senaryosu
1. UserA bir site oluşturur
2. UserA → UserB'yi Guest olarak ekler
3. UserB (Guest) → Kendini çıkarmaya çalışır

#### Beklenen Sonuç
- Guest kendini çıkarabilmeli (mantıklı)
- Owner kendini çıkarabilir mi? (Son owner kontrolü gerekli)

#### Kod İncelemesi

**Dosya:** `src/Application/Invitations/Commands/RemoveUserFromSite/RemoveUserFromSite.cs:36-42`

Kod sadece Owner kontrolü yapıyor:
```csharp
var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.Id == invitation.SiteId &&
                             !s.IsDeleted &&
                             s.UserId == currentUserId,  // Sadece Owner çalıştırabilir
                         cancellationToken)
```

#### Manuel Test Sonucu

**Test Durumu:** ❌ Guest Kendini Çıkaramıyor

**Test Detayı:**
UserB (Guest) → Kendini çıkarmaya çalışıyor → ❌ Çıkaramadı

**Sorun:**
- RemoveUserFromSite sadece Owner çalıştırabilir
- Guest kendini çıkaramıyor (olması gereken: çıkarabilmeli)

**Sonuç:** Guest kendini çıkaramıyor, bu özellik eklenmeli.

#### Düzeltme Önerisi

```csharp
// Owner mı yoksa kendini mi çıkarıyor kontrol et
var isSelfRemoval = invitation.InvitedUserId == currentUserId;

if (!isSelfRemoval)
{
    // Başkasını çıkarıyorsa Owner kontrolü yap
    var site = await _context.Sites
        .FirstOrDefaultAsync(s => s.Id == invitation.SiteId &&
                                 !s.IsDeleted &&
                                 s.UserId == currentUserId,
                             cancellationToken)
        ?? throw new NotFoundException(nameof(Site), invitation.SiteId);
}
```

---

## TEST 18: AUTH-001 - AdminOnly Policy Runtime Kontrolü ✅

**Kategori:** AUTH | **Öncelik:** MEDIUM

**Senaryo:** AdminOnly policy gerektiren endpoint'lere normal user erişebiliyor mu?

**Kod İncelemesi:**

Policy tanımı (`src/Web/Infrastructure/WebApplicationBuilderExtensions.cs`):
```csharp
options.AddPolicy(Policies.AdminOnly, policy =>
    policy.RequireRole(Roles.Administrator));
```

AdminOnly kullanan endpoint'ler:
- DeviceSettings: 6 endpoint (CRUD operations)
- Sites: 3 endpoint (GetAll, GetById, Create)
- DeviceComponents: 5 endpoint (CRUD operations)
- Setups: 3 endpoint (GetAll, Create, Delete)
- Devices: 4 endpoint (Update, Delete, Create, GetAll)

Test endpoint: `GET /api/Devices/{id}` (AdminOnly protected)

**Manuel Test:**
- Normal user token ile AdminOnly endpoint'e istek atıldı
- **403 Forbidden** alındı

**Sonuç:** ✅ Policy çalışıyor, normal user engellenmiş.

---

## TEST 19: AUTH-002 - DeviceOnly Policy Kontrolü ⚠️

**Kategori:** AUTH | **Öncelik:** MEDIUM

**Senaryo:** DeviceOnly policy gerektiren endpoint'lere normal user erişebiliyor mu? Device token ile erişilebiliyor mu?

**Kod İncelemesi:**

Policy tanımı:
```csharp
options.AddPolicy(Policies.DeviceOnly, policy =>
    policy.RequireClaim("DeviceId"));
```

DeviceOnly kullanan endpoint'ler:
- DeviceNotifications: 2 endpoint (SendNotification, GetNotifications)
- DeviceData: 3 endpoint (CreateEnergyData, UpdateEnergyData, DeleteEnergyData)

Device login: `POST /api/Devices/login` (SerialNumber ile token oluşturur)

**Sorun:** `GetLivePowerFlow` endpoint'i (`src/Web/Endpoints/DeviceData.cs:103-123`)
```csharp
app.MapGroup(this)
    //.RequireAuthorization()  // <- Yorum satırında!
    .MapGet(GetDeviceData)
    .MapGet(GetLivePowerFlow, "PowerFlow/live/{deviceId:guid}")
```

Endpoint seviyesinde `[Authorize]` attribute yok, `.RequireAuthorization()` yorum satırında.

**Manuel Test:**
- Normal user token ile `GET /api/DeviceData/PowerFlow/live/{deviceId}` isteği atıldı
- **200 OK** geldi, data döndü

**Sonuç:** ⚠️ GetLivePowerFlow endpoint'i korumasız, DeviceOnly policy uygulanmamış. Normal user erişebiliyor.

---

## TEST 20: AUTH-003 - Admin GetSites Yetkisi ✅

**Kategori:** AUTH | **Öncelik:** MEDIUM

**Senaryo:** Admin rolündeki user `GET /api/sites` ile tüm siteleri görebiliyor mu?

**Kod İncelemesi:**

Endpoint (`src/Web/Endpoints/Sites.cs:30-36`):
```csharp
[Authorize(Policies.AdminOnly)]
public async Task<GetSitesResponseDto> GetSites(
    ISender sender,
    [AsParameters] GetSitesQuery query)
{
    return await sender.Send(query);
}
```

Handler (`src/Application/Sites/Queries/GetSites/GetSites.cs:36-56`):
- `UserId`, `UserEmail`, `UserName` parametreleri opsiyonel (line 11-23)
- Parametre yoksa tüm siteler getirilir (line 38)
- Soft delete kontrolü var (line 59)

**Manuel Test:**
- Normal user token ile → **403 Forbidden** (AdminOnly policy engelledi)
- Admin token ile `GET /api/sites?PageNumber=1&PageSize=1` → **200 OK**, tüm siteleri döndü

**Sonuç:** ✅ AdminOnly policy çalışıyor. Admin tüm siteleri görebiliyor, normal user engellenmiş.

---

## TEST 21: AUTH-004 - Admin GetSiteById Yetkisi ✅

**Kategori:** AUTH | **Öncelik:** MEDIUM

**Senaryo:** Admin rolündeki user `GET /api/sites/{id}` ile başka bir kullanıcının sitesini görebiliyor mu?

**Kod İncelemesi:**

Endpoint (`src/Web/Endpoints/Sites.cs:38-44`):
```csharp
[Authorize(Policies.AdminOnly)]
public async Task<SiteDto?> GetSite(
    ISender sender,
    Guid id)
{
    return await sender.Send(new GetSiteByIdQuery(id));
}
```

Handler (`src/Application/Sites/Queries/GetSiteById/GetSiteById.cs:20-49`):
- Sadece `Id` parametresi alıyor (line 9)
- UserId kontrolü yok (line 22-26)
- Ownership kontrolü yapılmıyor

**Manuel Test:**
- Normal user token ile başka kullanıcının sitesi → **401 Unauthorized** (AdminOnly policy engelledi)
- Admin token ile başka kullanıcının sitesi → **200 OK**, site bilgisi döndü

**Sonuç:** ✅ AdminOnly policy çalışıyor. Admin başkasının sitesini görebiliyor, normal user engellenmiş.

---

## TEST 22: EDGE-001 - Site Oluştur Ama Device Eklemeden Paylaş ✅

**Kategori:** EDGE | **Öncelik:** LOW

**Senaryo:** Device olmayan site paylaşılabilir mi?

**Kod İncelemesi:**

CreateInvitation handler (`src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:42-49`):
- Site kontrolü var
- **Setup/Device kontrolü yok**
- Soft delete kontrolü var
- Ownership kontrolü var

**Manuel Test:**
- Device olmadan site oluşturuldu ve paylaşıldı
- UserB shared site'ı görebildi

**Sonuç:** ✅ Edge case çalışıyor, device zorunluluğu yok.

---

## TEST 23: EDGE-002 - Setup Var Ama SetupDevice Yok ✅

**Kategori:** EDGE | **Öncelik:** LOW

**Senaryo:** Orphan setup edge case - Setup kaydı var ama SetupDevice null durumu.

**Kod İncelemesi:**

CreateSetup handler (`src/Application/Setups/Commands/CreateSetup/CreateSetup.cs:35-138`):
- Device QRCode veya SerialNumber **zorunlu** (line 39-43)
- ThingsBoard'dan device bulunur (line 68-80)
- Local DB'de device bulunur (line 83-93)
- Setup oluşturulur (line 102-108)
- **SetupDevice her zaman oluşturulur** (line 111-117)

Setup entity (`src/Domain/Entities/Setup.cs:12`):
```csharp
public SetupDevice? SetupDevice { get; set; } // nullable
```

**Sonuç:** ✅ CreateSetup ile orphan setup oluşamaz (device zorunlu). Entity nullable olduğu için SetupDevice silinirse orphan setup oluşabilir ama normal akışta olmaz.

---

## TEST 24: EDGE-003 - Device Zaten Kullanımda ✅

**Kategori:** EDGE | **Öncelik:** LOW

**Senaryo:** Bir device sadece bir site'e bağlanabilir.

**Kod İncelemesi:**

CreateSetup handler (`src/Application/Setups/Commands/CreateSetup/CreateSetup.cs:95-99`):
```csharp
// Check if device is already in use
if (await _context.SetupDevices.AnyAsync(f => f.DeviceId == device.Id, cancellationToken))
{
    throw new System.ComponentModel.DataAnnotations.ValidationException("This device is already in use.");
}
```

**Manuel Test:**
- Aynı device ile ikinci setup oluşturulmaya çalışıldı
- "This device is already in use" hatası alındı

**Sonuç:** ✅ Device uniqueness kontrolü çalışıyor.

---

## TEST 25: EDGE-004 - Site'ın Birden Fazla Setup'ı ✅

**Kategori:** EDGE | **Öncelik:** LOW

**Senaryo:** Bir site sadece bir setup'a sahip olabilir.

**Kod İncelemesi:**

CreateSetup handler (`src/Application/Setups/Commands/CreateSetup/CreateSetup.cs:58-65`):
```csharp
// Check if site already has a setup
var existingSetup = await _context.Setups
    .FirstOrDefaultAsync(s => s.SiteId == request.SiteId, cancellationToken);

if (existingSetup != null)
{
    throw new System.ComponentModel.DataAnnotations.ValidationException("This site already has a setup. A site can only have one setup.");
}
```

**Manuel Test:**
- Aynı site'e ikinci setup oluşturulmaya çalışıldı
- Kod engelledi

**Sonuç:** ✅ Site başına tek setup kontrolü çalışıyor. Birden fazla setup özelliği henüz uygulamaya eklenmemiş, ilerleyen zamanlarda eklenebilir.

---

## TEST 26: EDGE-005 - Email ile Kullanıcı Yok Ama Davet Gönder ⚠️

**Kategori:** EDGE | **Öncelik:** MEDIUM

**Senaryo:** Sistemde olmayan kullanıcıya davet gönder → kullanıcı kaydolur → daveti kabul eder (InvitedUserId null flow).

**Kod İncelemesi:**

CreateInvitation handler (`src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:51-79`):
```csharp
// Email ile kullanıcı ara
var invitedUserId = await _identityService.GetUserIdByEmailAsync(request.Email);

var invitation = new Invitation
{
    Email = request.Email,
    InvitedUserId = invitedUserId,  // <- null olabilir
    ...
};
```

AcceptInvitation handler (`src/Application/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs:46-70`):
```csharp
var invitation = await _context.Invitations
    .FirstOrDefaultAsync(i =>
        i.Token == request.Token &&
        i.Status == InvitationStatus.Pending &&
        (i.Email == currentUserEmail || i.InvitedUserId == currentUserId),  // <- Email match yeterli
        cancellationToken)

// Eğer InvitedUserId boşsa, şimdi güncelle
if (string.IsNullOrEmpty(invitation.InvitedUserId))
{
    invitation.InvitedUserId = currentUserId;
}
```

**Manuel Test:**
- Sistemde olmayan email'e davet gönderildi
- Davet başarıyla oluşturuldu

**Bulgu:** Kod `InvitedUserId` null durumunu destekliyor.

⚠️ **Yan Sorun**: Davet maili içinde 6 haneli aktivasyon kodu gönderiliyor ama kayıtsız kullanıcı bu kodu kullanamıyor. Aktivasyon kodu mail template'inde boşuna yer kaplıyor.

**Öneriler:**
1. Aktivasyon kodu mail template'inden kaldırılmalı veya sadece kayıtlı kullanıcılara gönderilmeli
2. Kayıtsız kullanıcılar için farklı mail template kullanılmalı ("Önce kayıt olun" mesajı ile)

**Sonuç:** ⚠️ Flow çalışıyor ama UX problemi var (gereksiz aktivasyon kodu gönderiliyor).

---

## TEST 27: EDGE-006 - Davet Kabul Edilmişken Tekrar Kabul Etme ⚠️

**Kategori:** EDGE | **Öncelik:** LOW

**Senaryo:** Kullanıcı daveti kabul ettikten sonra aynı token ile tekrar kabul etmeye çalışırsa ne olur?

**Kod İncelemesi:**

AcceptInvitation handler (`src/Application/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs:46-52`):
```csharp
var invitation = await _context.Invitations
    .FirstOrDefaultAsync(i =>
        i.Token == request.Token &&
        i.Status == InvitationStatus.Pending &&  // <- Sadece Pending durumu
        (i.Email == currentUserEmail || i.InvitedUserId == currentUserId),
        cancellationToken)
    ?? throw new Common.Exceptions.NotFoundException(nameof(Invitation), request.Token);
```

Kabul edildikten sonra `Status` değişir (`Accepted`), ikinci denemede Pending invitation bulunamaz ve `NotFoundException` fırlatılır.

**Manuel Test:**
- Davet kabul edildi
- Aynı token ile tekrar kabul etmeye çalışıldı
- "Failed" mesajı alındı

**Bulgu:** Kod çalışıyor, `Status == Pending` kontrolü var.

⚠️ **UX Problemi**: Hata mesajı user-friendly değil. "Invitation not found" yerine **"This invitation has already been accepted"** gibi açıklayıcı mesaj olmalı.

**Sonuç:** ⚠️ Kod doğru çalışıyor ama hata mesajı iyileştirilmeli.

---

## TEST 28: PERF-001 - GetSitesByUser N+1 Query Problemi ❌

**Kategori:** PERF | **Öncelik:** LOW

**Senaryo:** GetSitesByUser query'sinde N+1 query problemi var mı?

**Kod İncelemesi:**

GetSitesByUser handler (`src/Application/Sites/Queries/GetSitesByUser/GetSitesByUser.cs:21-113`):

Query akışı:
1. UserSites tablosu → `ToListAsync()` (1 query)
2. Sites + Setup + SetupDevice → `.Include().ThenInclude()` eager loading (1 query)
3. **Foreach loop** (line 35-96): Her site için DeviceEnergyData query (line 49-55)

```csharp
foreach (var site in sites)  // N site varsa
{
    // Her site için ayrı query! ❌
    var lastEnergyData = await _context.DeviceEnergyData
        .Where(x => x.ThingsBoardDeviceId == deviceId && ...)
        .OrderByDescending(x => x.DeviceLocalTime)
        .FirstOrDefaultAsync(cancellationToken);
}
```

**N+1 Query Problemi:**
- 10 site → 1 + 1 + 10 = **12 query**
- 100 site → 1 + 1 + 100 = **102 query**

Her site için ayrı veritabanı sorgusu atılıyor. 100 site varsa 100 kez veritabanına gidip geliyor.

**Performans Etkisi:**
- Şu an: 100 site × 10ms = **~1 saniye**
- Düzeltilirse: 2 query × 10ms = **~20ms** (50 kat hızlı)

**Optimizasyon:** Tüm DeviceEnergyData'ları tek sorguda çek (batch query), Dictionary'e al, loop içinde memory'den oku.

**Sonuç:** ❌ N+1 query problemi var. Kod çalışıyor ama çok site varsa performans sorunu oluşturur.

---

## TEST 29: PERF-002 - CheckDeviceAccess Nested Any() Optimizasyonu ⚠️

**Kategori:** PERF | **Öncelik:** LOW

**Senaryo:** CheckDeviceAccess query'sinde nested Any() query optimizasyonu gerekli mi?

**Kod İncelemesi:**

CheckDeviceAccess handler (`src/Application/Devices/Queries/CheckDeviceAccess/CheckDeviceAccess.cs:42-48`):

```csharp
var hasAccess = await _context.SetupDevices
    .AnyAsync(sd => sd.DeviceId == request.DeviceId &&
                   _context.Setups.Any(s => s.Id == sd.SetupId &&              // <- Nested Any #1
                                          _context.Sites.Any(site => site.Id == s.SiteId &&     // <- Nested Any #2
                                                                   (site.CreatedBy == _user.Id ||
                                                                    _context.UserSites.Any(us => us.SiteId == site.Id && us.UserId == _user.Id)))),   // <- Nested Any #3
              cancellationToken);
```

**3 Seviye Nested Subquery:**

EF Core bunu şu SQL'e çevirir:
```sql
SELECT EXISTS(
  SELECT 1 FROM SetupDevices WHERE DeviceId = @deviceId
  AND EXISTS(
    SELECT 1 FROM Setups WHERE Id = SetupId
    AND EXISTS(
      SELECT 1 FROM Sites WHERE Id = SiteId
      AND (CreatedBy = @userId OR EXISTS(
        SELECT 1 FROM UserSites WHERE SiteId = SiteId AND UserId = @userId
      ))
    )
  )
)
```

4 tane iç içe `EXISTS` subquery. Database engine bunu optimize etmekte zorlanabilir.

**Performans Etkisi:**
- Büyük data setlerinde yavaşlayabilir
- Index kullanımı sınırlı
- Query optimizer zorlanır

**Optimizasyon:** JOIN kullan, nested subquery yerine direkt JOIN yap.

**Sonuç:** ⚠️ 3 seviye nested Any() var. Kod çalışıyor ama büyük data setlerinde performans problemi olabilir.

---

## TEST 30: PERF-003 - CheckSiteAccess Nested Any() Optimizasyonu ✅

**Kategori:** PERF | **Öncelik:** LOW

**Senaryo:** CheckSiteAccess query'sinde nested Any() query optimizasyonu gerekli mi?

**Kod İncelemesi:**

CheckSiteAccess handler (`src/Application/Sites/Queries/CheckSiteAccess/CheckSiteAccess.cs:41-45`):

```csharp
var hasAccess = await _context.Sites
    .AnyAsync(site => site.Id == request.SiteId &&
                     (site.CreatedBy == _user.Id ||
                      _context.UserSites.Any(us => us.SiteId == site.Id && us.UserId == _user.Id)),   // <- Nested Any
              cancellationToken);
```

**1 Seviye Nested Subquery:**

SQL çıktısı:
```sql
SELECT EXISTS(
  SELECT 1 FROM Sites WHERE Id = @siteId
  AND (CreatedBy = @userId OR EXISTS(
    SELECT 1 FROM UserSites WHERE SiteId = @siteId AND UserId = @userId
  ))
)
```

**Performans Analizi:**
- Sadece 1 seviye nested subquery
- CheckDeviceAccess'e göre çok daha basit (3 seviye yerine 1 seviye)
- Database engine kolayca optimize eder
- Index varsa (SiteId, UserId) hızlı çalışır

**Sonuç:** ✅ 1 seviye nested Any() var, kabul edilebilir. Performans problemi beklenmez.

---

## TEST 31: NOTIF-001 - Davet Gönderilince Notification Oluşuyor ✅

**Kategori:** NOTIF | **Öncelik:** LOW

**Senaryo:** CreateInvitation çalıştırıldığında DB'de notification kaydı oluşuyor mu?

**Kod İncelemesi:**

CreateInvitation handler (`src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:87-101`):

```csharp
// Notification kaydı oluştur
var notification = new Notification
{
    Id = Guid.NewGuid(),
    Email = request.Email,
    UserId = invitedUserId,  // Kayıtsız kullanıcı için null olabilir
    InvitationId = invitation.Id,
    Message = $"Kullanıcı size {site.Name} evini paylaştı.",
    IsRead = false,
    Created = utcNow
};

_context.Notifications.Add(notification);
await _context.SaveChangesAsync(cancellationToken);
```

**Sonuç:** ✅ Notification kaydı oluşturuluyor. CreateInvitation çalıştığında DB'ye Notification ekleniyor.

---

## TEST 32: NOTIF-002 - Email Gönderiliyor Mu? ✅

**Kategori:** NOTIF | **Öncelik:** LOW

**Senaryo:** CreateInvitation çalıştırıldığında SMTP servisi üzerinden email gönderiliyor mu?

**Kod İncelemesi:**

CreateInvitation handler (`src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:103-110`):
```csharp
// Email gönder
var currentUserName = await _identityService.GetUserNameAsync(currentUserId);
await _emailService.SendInvitationEmailAsync(
    request.Email,
    currentUserName ?? "Kullanıcı",
    site.Name,
    token,
    cancellationToken);
```

InvitationEmailService (`src/Infrastructure/Services/InvitationEmailService.cs:21-47`):
- `IEmailSender` kullanarak email gönderiyor
- HTML template oluşturuluyor
- 6 haneli token email içinde gönderiliyor
- Log kaydı tutuluyor

SmtpEmailSender (`src/Infrastructure/Services/SmtpEmailSender.cs:21-78`):
- MailKit kütüphanesi kullanılıyor
- SMTP bağlantısı kuruluyor (line 66)
- Authentication yapılıyor (line 71)
- Email gönderiliyor (line 74)

**Manuel Test:**
- Davet gönderildi
- Email başarıyla geldi

**Sonuç:** ✅ Email başarıyla gönderiliyor. SMTP servis çalışıyor.

---

## TEST 33: NOTIF-003 - Notification Okundu İşaretleme ✅

**Kategori:** NOTIF | **Öncelik:** LOW

**Senaryo:** Notification okundu olarak işaretlendiğinde DB'de IsRead flag güncelleniyor mu?

**Kod İncelemesi:**

MarkNotificationAsRead handler (`src/Application/Invitations/Commands/MarkNotificationAsRead/MarkNotificationAsRead.cs:27-40`):

```csharp
var notification = await _context.Notifications
    .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == currentUserId, cancellationToken)
    ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

notification.IsRead = true;  // IsRead flag güncelleniyor

await _context.SaveChangesAsync(cancellationToken);
```

**Authorization Kontrolü:**
- `n.UserId == currentUserId` kontrolü var (line 32)
- Kullanıcı sadece kendi notification'ını işaretleyebiliyor

**Sonuç:** ✅ IsRead flag güncelleniyor. Authorization kontrolü var, kod doğru çalışıyor.

---

## TEST 34: API-001 - UpdateSite Validation Çalışıyor Mu? ✅

**Kategori:** API | **Öncelik:** LOW

**Senaryo:** UpdateSite command'ında FluentValidation çalışıyor mu? Validator class var mı?

**Kod İncelemesi:**

UpdateSiteCommandValidator (`src/Application/Sites/Commands/UpdateSite/UpdateSiteCommandValidator.cs:7-48`):

```csharp
RuleFor(v => v.Id)
    .NotEmpty()
    .WithMessage("Id is required.");

RuleFor(v => v.Name)
    .NotEmpty()
    .WithMessage("Name is required.")
    .MaximumLength(200)
    .WithMessage("Name must not exceed 200 characters.");

RuleFor(v => v.Country)
    .MaximumLength(100);

RuleFor(v => v.City)
    .MaximumLength(100);

RuleFor(v => v.District)
    .MaximumLength(100);

RuleFor(v => v.AddressLine)
    .MaximumLength(500);

RuleFor(v => v.Latitude)
    .InclusiveBetween(-90, 90)
    .WithMessage("Latitude must be between -90 and 90.");

RuleFor(v => v.Longitude)
    .InclusiveBetween(-180, 180)
    .WithMessage("Longitude must be between -180 and 180.");

RuleFor(v => v.Timezone)
    .NotEmpty()
    .WithMessage("Timezone is required.")
    .MaximumLength(50);
```

**Validation Kuralları:**
- Id: Zorunlu
- Name: Zorunlu, Max 200 karakter
- Country/City/District: Max 100 karakter
- AddressLine: Max 500 karakter
- Latitude: -90 ile 90 arası (geçerli koordinat)
- Longitude: -180 ile 180 arası (geçerli koordinat)
- Timezone: Zorunlu, Max 50 karakter

**Not:** UpdateSite endpoint kullanıcıya henüz sunulmamış, kullanımda değil.

**Sonuç:** ✅ Validator var ve kapsamlı kurallar tanımlanmış. FluentValidation çalışıyor.

---

## TEST 35: API-002 - CreateInvitation Validation Çalışıyor Mu? ⚠️

**Kategori:** API | **Öncelik:** LOW

**Senaryo:** CreateInvitation command'ında FluentValidation çalışıyor mu? Validator class var mı?

**Kod İncelemesi:**

CreateInvitationCommandValidator (`src/Application/Invitations/Commands/CreateInvitation/CreateInvitationCommandValidator.cs:7-15`):

```csharp
RuleFor(v => v.Email)
    .NotEmpty().WithMessage("Email zorunludur.")
    .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

RuleFor(v => v.SiteId)
    .NotEmpty().WithMessage("Site ID zorunludur.");
```

**Validation Kuralları:**
- Email: Zorunlu, geçerli email format
- SiteId: Zorunlu

⚠️ **Eksik Validation'lar:**

Business logic kontrolleri handler'da yapılıyor, validator'da yok (early validation için validator'a eklenebilir):
- Self-invitation kontrolü yok (SEC-005)
- Duplicate pending invitation kontrolü yok (SEC-003)
- Duplicate UserSite kontrolü yok (SEC-004)
- Guest role kontrolü yok (SEC-006)

Bu kontroller şu an handler'da yapılıyor ama validation aşamasında engellenebilir (daha erken hata dönebilir).

**Not:** Daha önce yapılan SEC testlerinde (TEST 7-12) bu kontrollerin handler'da yapıldığı tespit edilmişti.

**Sonuç:** ⚠️ Validator var ama basit. Sadece Email ve SiteId kontrolü yapılıyor. Business logic validation'ları handler'da yapılıyor.

---

## TEST 36: API-003 - Endpoint Routing Doğru Mu? ✅

**Kategori:** API | **Öncelik:** LOW

**Senaryo:** Endpoint'ler düzgün map edilmiş mi? 404 hatası alınmıyor mu? (Smoke test)

**Kod İncelemesi:**

UpdateSite endpoint (`src/Web/Endpoints/Sites.cs:26`):
```csharp
.MapPut(UpdateSite, "mobile/update-site/{id}")
```
Route: `PUT /api/Sites/mobile/update-site/{id}`

CreateInvitation endpoint (`src/Web/Endpoints/Invitations.cs:21`):
```csharp
.MapPost(CreateInvitation, "create")
```
Route: `POST /api/Invitations/create`

**Endpoint Mapping:**
- UpdateSite: `PUT /api/Sites/mobile/update-site/{id}` (Henüz kullanımda değil)
- CreateInvitation: `POST /api/Invitations/create` (Kullanımda)

**Sonuç:** ✅ Endpoint'ler doğru map edilmiş, routing çalışıyor.

---

## TEST 37: INT-001 - ThingsBoard Device Bulunamaz Senaryosu ✅

**Kategori:** INT | **Öncelik:** LOW

**Senaryo:** CreateSetup sırasında ThingsBoard'da device bulunamazsa ne oluyor? Error handling var mı?

**Kod İncelemesi:**

CreateSetup handler (`src/Application/Setups/Commands/CreateSetup/CreateSetup.cs:67-80`):

```csharp
// Find device in ThingsBoard first
var tbDevice = await _tbConnector.FindDeviceAsync(
    request.DeviceSerialNumber,
    request.DeviceQRCode,
    cancellationToken);

if (tbDevice == null)
{
    var deviceIdentifier = !string.IsNullOrEmpty(request.DeviceQRCode)
        ? $"QR Code: {request.DeviceQRCode}"
        : $"Serial Number: {request.DeviceSerialNumber}";

    throw new ArgumentException($"Device not found in ThingsBoard with {deviceIdentifier}");
}
```

**Error Handling:**
- ThingsBoard'da device bulunamazsa `ArgumentException` fırlatılıyor
- Hata mesajı açıklayıcı: "Device not found in ThingsBoard with {QRCode/SerialNumber}"
- Kullanıcıya hangi parametreyle arama yapıldığı belirtiliyor

**Sonuç:** ✅ Error handling var, kullanıcıya açıklayıcı hata mesajı dönüyor.

---

## TEST 38: INT-002 - ThingsBoard'da Var Ama Local DB'de Yok ✅

**Kategori:** INT | **Öncelik:** LOW

**Senaryo:** ThingsBoard'da device var ama local database'de yok (orphan device). Edge case handling nasıl?

**Kod İncelemesi:**

CreateSetup handler (`src/Application/Setups/Commands/CreateSetup/CreateSetup.cs:82-93`):

```csharp
// Find device in local database by ThingsBoard ID
var device = await _context.Devices
    .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == tbDevice.Id && !d.IsDeleted, cancellationToken);

if (device == null)
{
    var deviceIdentifier = !string.IsNullOrEmpty(request.DeviceQRCode)
        ? $"QR Code: {request.DeviceQRCode}"
        : $"Serial Number: {request.DeviceSerialNumber}";

    throw new ArgumentException($"Device not found in local database with {deviceIdentifier}");
}
```

**Error Handling:**
- ThingsBoard'da bulundu ama local DB'de yoksa `ArgumentException` fırlatılıyor
- Hata mesajı: "Device not found in local database with {QRCode/SerialNumber}"
- Admin önce `CreateDevice` endpoint'i ile device'ı local DB'ye eklemelidir

**Sonuç:** ✅ Edge case handling var, kullanıcıya açıklayıcı hata mesajı dönüyor.

---

## TEST 39: INT-003 - Weather API Entegrasyonu Çalışıyor Mu? ✅

**Kategori:** INT | **Öncelik:** LOW

**Senaryo:** Weather API'den hava durumu verisi çekiliyor mu? API entegrasyonu doğru çalışıyor mu?

**Kod İncelemesi:**

CreateDeviceEnergyData handler (`src/Application/DeviceData/Commands/CreateDeviceEnergyData/CreateDeviceEnergyData.cs:86-94`):

```csharp
// Hava durumu bilgilerini al
WeatherData? currentWeather = null;
if (site?.Latitude.HasValue == true && site.Longitude.HasValue == true)
{
    currentWeather = await _weatherService.GetCurrentWeatherAsync(
        site.Latitude.Value,
        site.Longitude.Value,
        cancellationToken);
}
```

WeatherApiService (`src/Infrastructure/Services/WeatherApiService.cs:29-71`):
- HttpClient ile WeatherAPI.com'a istek atılıyor
- API Key appsettings'den okunuyor (`WeatherApi:ApiKey`)
- Success kontrolü var (line 39-43)
- Hata durumunda `null` dönüyor (exception fırlatmaz)
- Exception handling var (line 66-70)
- Log kaydı tutuluyor

**Error Handling:**
- API başarısız → `null` döner (sistem devam eder)
- Deserialization başarısız → `null` döner
- Exception oluşursa → `null` döner, log yazılır
- Site koordinatları yoksa API çağrılmaz

**Graceful Degradation:**
Weather API başarısız olsa bile sistem çalışmaya devam eder, sadece hava durumu verisi olmaz.

**Ek Not:** Heartbeat kontrolü RPC üzerinden Node-RED'den yapılıyor (inverter connectivity), DB'ye kaydedilmiyor. Frontend için real-time kontrol.

**Sonuç:** ✅ Weather API entegrasyonu çalışıyor, hata durumunda graceful degradation var.

---

## TEST 40: SQL-001 - EF Core Parametreli Query Kullanımı (SQL Injection Güvenliği) ✅

**Kategori:** SQL | **Öncelik:** LOW

**Senaryo:** EF Core LINQ kullanımı SQL injection'dan koruyor mu? Raw SQL query var mı? Parametreli query kullanılıyor mu?

**Kod İncelemesi:**

**1. Raw SQL Taraması:**
```bash
# Arama: FromSqlRaw, ExecuteSqlRaw, FromSql, ExecuteSql
Sonuç: Hiçbir kullanım bulunamadı ✅
```

**2. String Concatenation Taraması:**
```bash
# Arama: String concatenation ile query oluşturma (.Where(... + ...), vb.)
Sonuç: Hiçbir kullanım bulunamadı ✅
```

**3. LINQ Kullanım Örnekleri:**

CreateInvitation (`src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs:43-48`):
```csharp
var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.Id == request.SiteId &&
                             !s.IsDeleted &&
                             (s.UserId == currentUserId ||
                              _context.UserSites.Any(us => us.SiteId == s.Id && us.UserId == currentUserId)),
                         cancellationToken)
```

GetSites (`src/Application/Sites/Queries/GetSites/GetSites.cs:62-84`):
```csharp
if (!string.IsNullOrEmpty(request.Name))
{
    query = query.Where(x => x.Name.Contains(request.Name));
}

if (!string.IsNullOrEmpty(request.Country))
{
    query = query.Where(x => x.Country != null && x.Country.Contains(request.Country));
}

if (!string.IsNullOrEmpty(request.AddressLine))
{
    query = query.Where(x => x.AddressLine != null && x.AddressLine.ToLower().Contains(request.AddressLine.ToLower()));
}
```

**EF Core SQL Çıktısı:**

LINQ expression:
```csharp
.Where(x => x.Name.Contains(request.Name))
```

EF Core tarafından oluşturulan SQL (parametreli):
```sql
SELECT * FROM Sites WHERE Name LIKE @p0
-- @p0 parametresi = '%{request.Name}%'
```

**Güvenlik Analizi:**

✅ **Raw SQL kullanımı yok**: `FromSqlRaw`, `ExecuteSqlRaw` hiçbir yerde kullanılmamış

✅ **String concatenation yok**: SQL query'ler string concatenation ile oluşturulmamış

✅ **LINQ kullanılıyor**: Tüm database işlemleri EF Core LINQ ile yapılıyor

✅ **Parametreli query'ler**: EF Core LINQ expression'ları otomatik olarak parametreli SQL'e çeviriyor

✅ **User input güvenli**: User input direkt SQL'e gitmiyor, LINQ parametresi olarak geçiyor

**SQL Injection Koruması:**

EF Core LINQ kullanımı sayesinde:
- Tüm user input'lar parametreli query'lerde kullanılıyor
- SQL injection attack vektörleri engellenmiş
- String escape işlemleri EF Core tarafından otomatik yapılıyor

**Sonuç:** ✅ SQL Injection'a karşı tam korumalı. Tüm query'ler EF Core LINQ ile parametreli olarak çalışıyor. Raw SQL kullanımı yok.

---

## TEST 41: FLOW-001 - Yeni Kullanıcı Onboarding Akışı ✅

**Kategori:** FLOW | **Öncelik:** MEDIUM

**Senaryo:** Yeni kullanıcı kayıt olup dashboard'a erişene kadar tam akış.

**Akış Adımları:**
1. Kullanıcı kayıt olur (Register)
2. Login yapar (JWT token alır)
3. GetUserStatus çağrısı → `NeedsSite` döner
4. CreateSite ile site oluşturur
5. GetUserStatus çağrısı → `NeedsSetup` döner
6. CreateSetup ile device ekler (ThingsBoard + Local DB)
7. GetUserStatus çağrısı → `Complete` döner
8. Dashboard'a erişir, enerji datalarını görür

**Manuel Test:**
- Akış baştan sona test edildi
- Kullanıcı hedef noktaya (dashboard) ulaştı

**Tespit Edilen Ufak Buglar:**
- BUG-001: GetUserStatus sadece `Sites.UserId` kontrolü yapıyor, `UserSites` tablosunu kontrol etmiyor (shared site'ları görmüyor)

**Sonuç:** ✅ Akış genel olarak başarılı, kullanıcı dashboard'a erişebiliyor. Auth eksiklikleri var ama kritik değil.

---

## TEST 42: FLOW-002 - Davetiye Akışı (Guest Onboarding) ✅

**Kategori:** FLOW | **Öncelik:** HIGH

**Senaryo:** Site sahibi başka kullanıcıyı davet ediyor, guest user site'a erişiyor.

**Akış Adımları:**
1. UserA site oluşturur ve device ekler
2. UserA CreateInvitation ile UserB'yi davet eder
3. Email gönderilir (6 haneli token ile)
4. UserB notification alır (DB + email)
5. UserB AcceptInvitation ile daveti kabul eder
6. UserSite kaydı oluşur (Guest role ile)
7. UserB GetSitesByUser ile shared site'ı görür
8. UserB dashboard'a erişir, shared site'ın enerji datalarını görür

**Manuel Test:**
- Akış baştan sona test edildi
- Guest kullanıcı shared site'a erişebildi
- Email başarıyla geldi
- Dashboard dataları görüldü

**Tespit Edilen Ufak Buglar:**
- SEC-005: Self-invitation kontrolü yok (kullanıcı kendine davet gönderebiliyor)
- SEC-006: Guest role kontrolü yok (Guest kullanıcı başka kullanıcıları davet edebiliyor)
- BUG-002: UpdateSite authorization yok (Guest güncelleyebiliyor)

**Sonuç:** ✅ Akış genel olarak başarılı, guest kullanıcı shared site'a erişebiliyor. Auth eksiklikleri var ama kritik değil.

---

## TEST 43: FLOW-003 - Davetiye Reddetme Akışı

**Kategori:** FLOW | **Öncelik:** LOW

**Senaryo:** Kullanıcı davetiyeyi reddediyor.

**Akış Adımları:**
1. UserA UserB'yi davet eder
2. UserB DeclineInvitation endpoint'ini çağırır
3. Invitation status `Declined` olarak güncellenir

**Not:** DeclineInvitation endpoint'i mevcut durumda kontrol edilmedi. Gerekirse eklenebilir.

---

## 📋 Önemli Notlar

### Device Operation - Gerçek Hayat Senaryosu

**Durum:**
- 2.21 kW ile bataryadan enerji çekildi ve direkt load'a verildi
- Batarya 10 dk sonra kendini kapattı
- Inverter **F56_DC_low_voltage** hatası verdi
- Load bir ara kesti ama tekrar geri geldi
- Mode: Zero export to load
- TOU: Disable

**Analiz:**
Batarya düşük voltaj koruması devreye girdi. Inverter F56 hata kodu ile batarya düşük voltajını bildirdi. Load kesintisi yaşandı ama sistem kendini toparlayarak load'u tekrar devreye aldı. Zero export to load modunda çalışırken batarya kapasitesi yetmediğinde bu senaryo gerçekleşebilir.

**İyileştirme Önerileri:**
- Dashboard'da inverter hata kodları (F56, F57, vb.) gösterilmeli
- Battery SOC %10'un altına düştüğünde kullanıcıya uyarı verilmeli
- Load kesintisi olduğunda notification gönderilmeli
- Inverter state/error logging mekanizması eklenmeli

---

## Genel Değerlendirme

### ✅ Başarılı Testler
- **BUG-001:** Uygulama çalışıyor (frontend farklı endpoint kullanıyor)

### ❌ Başarısız Testler
- **BUG-002:** Kritik güvenlik açığı (authorization yok)

### ⚠️ Uyarılar
1. **BUG-001:** Kod mantık hatası var, düzeltilmeli
2. **BUG-002:** ACİL düzeltme gerekli, production'da risk!

---

## 🎯 Öncelikli Aksiyonlar

### 1. ACİL 
- ✅ **BUG-002 düzelt:** UpdateSite'a authorization ekle
- ✅ **Test et:** Diğer Update command'larını da kontrol et (UpdateSetup, UpdateDevice, vs.)

### 2. Yakında 
- ⚠️ **BUG-001 düzelt:** GetUserStatus'e UserSites kontrolü ekle
- 📝 **Refactor:** Tüm Update/Delete command'larında authorization pattern'i standartlaştır

### 3. İyileştirme
- 📚 **Authorization Helper:** Ortak authorization logic'i helper method'a taşı
- 🧪 **Unit Test:** Authorization senaryoları için test ekle

---

## 📝 Notlar

- Frontend şu an **Invitations endpoint**'i kullanarak shared site'ları gösteriyor
- GetUserStatus endpoint'i hatalı ama uygulama çalışıyor (frontend workaround yapmış)
- Guest kullanıcılar şu an sistem genelinde çok fazla yetkiye sahip
- Tüm CRUD operasyonları için authorization review'i yapılmalı

---

**Sonraki Adım:** HIGH öncelikli testlere devam edilecek

**Rapor Tarihi:** 2026-01-06
**Rapor Versiyonu:** 1.0
