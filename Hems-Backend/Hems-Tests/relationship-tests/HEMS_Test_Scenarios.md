# HEMS Backend - Test Senaryoları

**Proje:** HEMS (Home Energy Management System) Backend
**Tarih:** 2026-01-05
**Branch:** stage
**Toplam Test:** 45 senaryo

---

## 📊 Öncelik Dağılımı

| Öncelik | Adet | Açıklama |
|---------|------|----------|
| 🔴 **CRITICAL** | 2 | Hemen düzeltilmeli - sistem kullanılamaz |
| 🟠 **HIGH** | 7 | Yakında düzeltilmeli - güvenlik/data riski |
| 🟡 **MEDIUM** | 10 | Gelecek sprint'te düzeltilebilir |
| 🟢 **LOW** | 26 | İlerleyen zamanlarda düzeltilebilir |

---

## 🔴 CRITICAL - Kritik Öncelik (2)

| Test ID | Kategori | Senaryo | Test Adımları | Beklenen Sonuç | Mevcut Durum | Açıklama | Test Durumu | Dosya/Satır |
|---------|----------|---------|---------------|----------------|--------------|----------|-------------|-------------|
| **BUG-001** | GetUserStatus | Guest kullanıcı shared site'ı görmüyor | 1. UserA bir site oluşturur<br>2. UserA, UserB'yi site'a davet eder<br>3. UserB daveti kabul eder<br>4. UserB login olur ve GetUserStatus çağrılır | UserB'nin shared site bilgisi dönmeli, Status: Complete olmalı | BUG: UserB'nin shared site'ı görmüyor, Status: NeedsSite dönüyor çünkü GetUserStatus sadece 'Sites.UserId' kontrol ediyor, UserSites tablosuna bakmıyor | GetUserStatus query'si sadece kendi oluşturduğu siteleri gösteriyor, shared siteleri görmüyor. UserSites tablosuna da bakması lazım. | | GetUserStatus.cs:26-27 |
| **BUG-002** | Authorization | UpdateSite - Authorization ve Role kontrolü yok (Guest de güncelleyebiliyor) | 1. UserA bir site oluşturur (SiteA)<br>2. UserB login olur veya UserB Guest olarak SiteA'ya eklenmiş<br>3. UserB, SiteA'nın ID'si ile UpdateSite çağırır | Authorization hatası dönmeli (UserB SiteA'nın sahibi değil veya Guest) | BUG: Herhangi bir kullanıcı herhangi bir site'ı güncelleyebilir! Handler'da authorization kontrolü yok. Guest kullanıcı bile güncelleyebilir. | LOGIC-001 ile birleştirildi. Hem authorization kontrolü yok hem de Guest role kontrolü yok. Sadece Owner güncelleyebilmeli. | | UpdateSite.cs:30-49 |

---

## 🟠 HIGH - Yüksek Öncelik (7)

| Test ID | Kategori | Senaryo | Test Adımları | Beklenen Sonuç | Mevcut Durum | Açıklama | Test Durumu | Dosya/Satır |
|---------|----------|---------|---------------|----------------|--------------|----------|-------------|-------------|
| **BUG-003** | Authorization | UpdateDevice - Endpoint policy test edilmeli | 1. Admin bir device oluşturur<br>2. Normal UserA login olur<br>3. UserA, device'ı UpdateDevice ile güncellemeye çalışır | Authorization hatası dönmeli (Endpoint AdminOnly policy ile korunmuş) | Endpoint [Authorize(AdminOnly)] ile korunmuş AMA handler'da hiçbir kontrol yok. Policy düzgün çalışıyor mu test edilmeli | LOGIC-002'den farklı - bu device update. Policy runtime'da çalışıyor mu test et. | | UpdateDevice.cs:40-83 |
| **SEC-004** | Invitation | Accepted davetten sonra tekrar davet (Duplicate UserSite riski!) | 1. UserA, UserB'yi SiteA'ya davet eder<br>2. UserB daveti kabul eder (Status: Accepted, UserSites'a eklenir)<br>3. UserA tekrar UserB'yi SiteA'ya davet eder | Hata dönmeli: 'Bu kullanıcı zaten bu site'e erişime sahip' | BUG: Sadece 'Pending' invitation kontrol ediliyor! UserB'ye tekrar davet gönderilebilir ve AcceptInvitation çağrılırsa UserSites'da DUPLICATE kayıt oluşabilir | CRITICAL BUG: UserSites'da duplicate kayıt oluşabilir! CreateInvitation'da UserSites kontrolü de yapılmalı. | | CreateInvitation.cs:54-65 |
| **SEC-006** | Invitation | Guest kullanıcı davet gönderebiliyor (Sadece Owner gönderebilmeli) | 1. UserA bir site oluşturur (SiteA)<br>2. UserB'yi Guest olarak SiteA'ya ekle<br>3. UserB (Guest), SiteA için UserC'yi davet etmeye çalışır | Hata dönmeli: Sadece Owner davet gönderebilir | Kod kontrol ediyor: 's.UserId == currentUserId \|\| UserSites.Any()' - Guest de davet gönderebilir! | LOGIC-003 ile birleştirildi. Guest davet gönderememeli, sadece Owner (s.UserId == currentUserId) gönderebilmeli. | | CreateInvitation.cs:46-49 |
| **EDGE-007** | Invitation | GetInvitationsBySite - Başkasının sitesinin invitation'larını görme | 1. UserA bir site oluşturur (SiteA)<br>2. UserB login olur (SiteA'ya erişimi yok)<br>3. UserB, SiteA için GetInvitationsBySite çağırır | Authorization hatası dönmeli (UserB, SiteA'nın sahibi veya guest'i değil) | Kodu kontrol edilmeli - GetInvitationsBySite handler'ında authorization var mı? | Handler kodu incelenmeli ve authorization eklenmeli | | Endpoints/Invitations.cs:53-58 & Handler |
| **LOGIC-002** | Guest Role | Guest kullanıcı setup güncelleyebilir mi? | 1. UserA bir site oluşturur ve setup ekler<br>2. UserB'yi Guest olarak ekle<br>3. UserB setup'ı güncellemeye çalışır (UpdateSetup) | Guest güncelleyememeli (sadece Owner güncelleyebilmeli) veya business requirement'a göre karar ver | UpdateSetup handler'da Role kontrolü YOK! Sadece access var mı kontrol ediliyor (shared site olsa bile güncelleyebilir) | BUG-003'ten farklı (bu setup update). Business logic: Guest setup güncelleyebilir mi? Karar verilmeli. | | UpdateSetup.cs:30-43 |
| **DATA-002** | Data Integrity | Invitation var ama Site silinmiş (AcceptInvitation kontrolü yok!) | 1. UserA bir site oluşturur ve UserB'yi davet eder<br>2. UserA site'ı siler (IsDeleted=true)<br>3. UserB daveti kabul etmeye çalışır | Hata: 'Site bulunamadı' veya 'Davetiye geçersiz' (silinmiş site'e eklenememeli) | BUG: AcceptInvitation'da Site kontrolü YOK! UserB silinmiş site'e eklenebilir (UserSites'a eklenebilir) | Kritik data integrity sorunu. AcceptInvitation'da Site IsDeleted kontrolü eklenmeli. | | AcceptInvitation.cs:34-104 |
| **AUTH-001** | Authorization Policy | AdminOnly policy runtime'da çalışıyor mu? | 1. Normal user login olur (user token al)<br>2. Admin-only endpoint'e istek atar (ör: GET /api/devices/{id})<br>3. Authorization header'da user token gönder | 403 Forbidden dönmeli (AdminOnly policy devreye girmeli) | Endpoint'te [Authorize(Policies.AdminOnly)] var, RUNTIME'da çalışıyor mu test edilmeli | Açıklama: Endpoint attribute var ama runtime'da policy gerçekten çalışıyor mu? Integration test ile doğrula. | | Devices.cs:47-53 |
| **FLOW-002** | Invitation Flow | Davetiye tam akışı (guest onboarding) | 1. UserA site oluşturur ve device ekler<br>2. UserA UserB'yi davet eder (CreateInvitation)<br>3. UserB notification alır (email + in-app)<br>4. UserB daveti kabul eder (AcceptInvitation)<br>5. UserB GetSitesByUser ile shared site'ı görür<br>6. UserB shared site'a erişebilir (dashboard/data) | Akış sorunsuz çalışmalı, UserB shared site'ı kullanabilmeli | End-to-end test edilmeli - BUG-001 ve SEC-004 düzeltilmeli önce | NOT: Düzeltme sonrası test edilecek. BUG-001 (GetUserStatus) ve SEC-004 (duplicate UserSite) düzeltilmeden çalışmayabilir. | | N/A - End-to-end |

---

## 🟡 MEDIUM - Orta Öncelik (10)

| Test ID | Kategori | Senaryo | Açıklama | Test Durumu | Dosya/Satır |
|---------|----------|---------|----------|-------------|-------------|
| **BUG-004** | GetUserStatus | Admin kullanıcı için GetUserStatus davranışı | Karar: Admin site oluşturmamalı ama tüm siteleri admin endpoint'lerinden görebilmeli. GetUserStatus admin için özel davranış göstermeli mi? | | GetUserStatus.cs:21-96 |
| **SEC-001** | Invitation | Token collision riski - 6 haneli token yeterli mi? | Token: Random.Next(100000, 999999). Aynı anda 10K+ davetiye gönderilirse collision olabilir mi? Birthday paradox test et. | | CreateInvitation.cs:115-135 |
| **SEC-003** | Invitation | Aynı kullanıcıya aynı anda birden fazla pending davet | SEC-004'ten FARKLI: Bu aynı anda birden fazla pending davet senaryosu. Business logic: İzin verilmeli mi? | | CreateInvitation.cs:54-65 |
| **SEC-005** | Invitation | Kendine davet gönderme | Email kontrolü yap: invitedUserId == currentUserId ise reject et | | CreateInvitation.cs:38-136 |
| **SEC-007** | Authorization | DeleteSite - Admin kontrolü yok | Admin IsInRoleAsync kontrolü eklenmeli | | DeleteSite.cs:24-30 |
| **SEC-008** | Authorization | RemoveUserFromSite - Admin kontrolü yok | Admin IsInRoleAsync kontrolü eklenmeli | | RemoveUserFromSite.cs:36-42 |
| **SEC-009** | Invitation | Kendini shared site'a device olarak ekleyebiliyor | Yeni test - kullanıcı feedback'i: 'adam kendini de device sharing'de ekleyebiliyor'. Kod incelenmeli. | | N/A - İlgili handler bulunmalı |
| **EDGE-005** | Invitation | Email ile kullanıcı yok ama davet gönder (InvitedUserId null) | Flow test: Sistemde olmayan kullanıcıya davet → kullanıcı kaydolur → daveti kabul eder | | CreateInvitation.cs:51-79 & AcceptInvitation.cs:66-70 |
| **LOGIC-006** | Guest Role | Guest kendini shared site'dan çıkarabilir mi? Owner ne yapabilir? | Açıklama: UserB kendini çıkarabilmeli mantıklı. Owner kendini çıkarabilir mi? (Site sahibi site'den çıkabilir mi?) Business logic belirle. | | RemoveUserFromSite.cs:27-64 |
| **DATA-003** | Data Integrity | Setup var ama Site silinmiş (orphan setup) | EF Core cascade delete configuration kontrol et | | N/A - EF Configuration |
| **DATA-004** | Data Integrity | Device silinirse SetupDevice ne olur? (soft delete sonrası tekrar kullanılabilir mi?) | Açıklama: Soft delete sonrası device tekrar kullanılabilir mi emin değilim. Business logic belirle. Cascade delete gerekli mi? | | N/A - DeleteDevice handler |
| **AUTH-002** | Authorization Policy | DeviceOnly policy çalışıyor mu? | Açıklama: Device authentication ayrı JWT token. User token ile device endpoint'e giderse reject edilmeli. | | N/A - Device endpoints |
| **FLOW-001** | User Flow | Yeni kullanıcı tam akışı (onboarding) | NOT: BUG-001 ve diğer fixler sonrası test yapılmalı. Onboarding flow kritik. | | N/A - End-to-end |

---

## 🟢 LOW - Düşük Öncelik (26)

### Security Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **SEC-002** | Expired invitation kontrolü (24 saat) | Kod mevcut - çalışıyor mu test et | |

### Edge Cases

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **EDGE-001** | Site oluştur ama device eklemeden paylaş | Device olmayan site paylaşılabilir mi test et | |
| **EDGE-002** | Setup var ama SetupDevice yok | Orphan setup edge case | |
| **EDGE-003** | Device zaten kullanımda | Bir device sadece bir site'e bağlanabilir | |
| **EDGE-004** | Site'ın birden fazla setup'ı | Bir site sadece bir setup'a sahip olabilir | |
| **EDGE-006** | Davet kabul edilmişken tekrar kabul etme (2. attempt) | Kullanıcı feedback: 2. defa gidince 'failed' diyor ikisinde de, 'already accepted' demeli. User-friendly hata mesajı ekle. | |

### Guest Role Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **LOGIC-004** | Guest kullanıcı diğer guest'i kaldırabilir mi? | Kod doğru - test et ve doğrula | |
| **LOGIC-005** | Guest kullanıcı site silebilir mi? | Kod doğru - test et ve doğrula | |

### Data Integrity Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **DATA-001** | UserSite kaydı var ama Site silinmiş (soft delete) | Kod doğru - test et ve doğrula | |

### Performance Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **PERF-001** | GetSitesByUser - N+1 query problemi var mı? | SQL profiler ile query count kontrol et | |
| **PERF-002** | CheckDeviceAccess - Nested Any() query optimizasyonu | Query plan analiz et | |
| **PERF-003** | CheckSiteAccess - Nested Any() query optimizasyonu | Query plan analiz et | |

### Authorization Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **AUTH-003** | Admin GetSites ile tüm siteleri görebiliyor mu? | Admin access test | |
| **AUTH-004** | Admin GetSiteById ile başkasının sitesini görebiliyor mu? | Admin access test | |

### Notification Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **NOTIF-001** | Davet gönderilince notification oluşuyor mu? | DB'de notification kaydı oluşuyor mu kontrol et | |
| **NOTIF-002** | Email gönderildi mi? (SMTP servis) | Email service mock'la veya test SMTP ile doğrula | |
| **NOTIF-003** | Notification okundu işaretleme | DB'de IsRead flag güncelleniyor mu kontrol et | |

### Flow Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **FLOW-003** | Davetiye reddetme akışı | Decline flow test | |

### API Contract Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **API-001** | UpdateSite validation çalışıyor mu? (FluentValidation) | Validator class var mı kontrol et ve test et | |
| **API-002** | CreateInvitation validation çalışıyor mu? | Validator class var mı kontrol et ve test et | |
| **API-003** | Endpoint routing doğru mu? (404 olmamalı) | Endpoint mapping test (smoke test) | |

### Integration Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **INT-001** | ThingsBoard'da device bulunamaz senaryosu | ThingsBoard connector mock'la ve 'device not found' senaryosu test et | |
| **INT-002** | ThingsBoard'da var ama local DB'de yok | ThingsBoard'da var ama DB'de yok edge case | |
| **INT-003** | Weather API entegrasyonu çalışıyor mu? | Weather API mock'la veya test API key ile doğrula | |

### Security Tests

| Test ID | Senaryo | Açıklama | Test Durumu |
|---------|---------|----------|-------------|
| **SQL-001** | EF Core parametreli query kullanıyor mu? (SQL Injection güvenliği) | EF Core LINQ kullanımı SQL injection'dan koruyor - security scan yap | |

---

## 📝 Test Durumu İşaretleme

Test yaptıktan sonra **Test Durumu** kolonuna şunları yazabilirsin:
- ✅ **Geçti** - Test başarılı
- ❌ **Kaldı** - Test başarısız, bug bulundu
- ⏳ **Devam Ediyor** - Test devam ediyor
- ⚠️ **Kısmi** - Kısmen çalışıyor, bazı durumlar fail
- 🔄 **Yeniden Test** - Düzeltme sonrası tekrar test edilmeli
- ⏭️ **Atlandı** - Test atlandı (sebep yaz)

---

## 🎯 Öncelikli Aksiyon Listesi

1. ✅ **BUG-001 düzelt** - Guest kullanıcılar sistemi kullanamıyor
2. ✅ **BUG-002 düzelt** - UpdateSite authorization açığı
3. ✅ **SEC-004 düzelt** - Duplicate UserSite riski
4. ✅ **SEC-006 düzelt** - Guest davet gönderebiliyor
5. ⏳ **LOGIC-002 karar ver** - Guest setup güncelleyebilir mi?
6. ⏳ **DATA-002 düzelt** - Silinmiş site'e invitation
7. ⏳ **EDGE-007 incele** - GetInvitationsBySite authorization
8. ⏳ **BUG-003 test et** - UpdateDevice endpoint policy çalışıyor mu?
9. ⏳ **AUTH-001 test et** - AdminOnly policy runtime test
10. ⏳ **FLOW-002 test et** - Düzeltmeler sonrası guest onboarding akışı

---

## 📊 Detaylı Test Verileri

Detaylı test adımları, beklenen sonuçlar ve tüm alanlar için CSV dosyasına bakın:
📄 **HEMS_Test_Plani.csv**

---

**Son Güncelleme:** 2026-01-05
**Günceleyen:** Claude Code
**Versiyon:** 1.0
