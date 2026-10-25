# HEMS Backend - Gelecek Test Senaryoları

Bu doküman, HEMS Backend projesinde ileride test edilmesi önerilen gerçek hayat senaryolarını içermektedir. Bu senaryolar şu anki 43 test senaryosuna ek olarak, sistemin production ortamında karşılaşabileceği durumları kapsamaktadır.

---

## 📡 Network & Connectivity Tests

### FUTURE-001: WiFi Şifre Değişikliği
**Senaryo:** Ev WiFi şifresi değiştiğinde EC100 cihazının otomatik bağlantısı güncelliyor mu?
**Öncelik:** HIGH
**Kategori:** Network
**Test Adımları:**
1. EC100 cihazı WiFi'ye bağlı durumda
2. Router WiFi şifresi değiştirilir
3. EC100 bağlantısı kesilir
4. Kullanıcı yeni şifreyi nasıl girecek?
**Beklenen:** Kullanıcıya WiFi yeniden konfigürasyon mekanizması sunulmalı (AP mode, WPS, vb.)

### FUTURE-002: Device Offline Durumu
**Senaryo:** Device offline olduğunda dashboard ne gösteriyor?
**Öncelik:** HIGH
**Kategori:** Network
**Beklenen:**
- "Device Offline" durumu gösterilmeli
- Son bilinen data timestamp ile gösterilmeli
- Notification gönderilmeli (device 5 dk offline kalırsa)

### FUTURE-003: Local Data Buffer & Sync
**Senaryo:** Internet kesildiğinde device local'de data biriktiriyor mu, sonra sync ediyor mu?
**Öncelik:** MEDIUM
**Kategori:** Network
**Test Adımları:**
1. Device online, data gönderiliyor
2. Internet bağlantısı kesilir
3. 1 saat boyunca device çalışır
4. Internet bağlantısı geri gelir
**Beklenen:** 1 saatlik data bulk olarak sync edilmeli

### FUTURE-004: ThingsBoard Connection Timeout
**Senaryo:** ThingsBoard bağlantısı kesildiğinde timeout süresi ne kadar?
**Öncelik:** MEDIUM
**Kategori:** Integration
**Beklenen:**
- Timeout süresi 30 saniye olmalı
- Retry mekanizması olmalı (exponential backoff)
- Log kaydı tutulmalı

### FUTURE-005: Weather API Downtime
**Senaryo:** Weather API down olduğunda dashboard hala çalışıyor mu?
**Öncelik:** LOW
**Kategori:** Integration
**Durum:** ✅ Zaten test edildi (INT-003), graceful degradation var

---

## 👥 Concurrent User & Race Conditions

### FUTURE-006: Multiple Admin Login
**Senaryo:** Aynı anda 2 admin login olması durumunda sıkıntı yaşar mıyız?
**Öncelik:** HIGH
**Kategori:** Concurrency
**Test Adımları:**
1. Admin1 login olur
2. Admin2 aynı anda login olur
3. Her ikisi de CRUD operations yapar
**Beklenen:** Session conflict olmamalı, her admin bağımsız çalışmalı

### FUTURE-007: High Concurrent User Load
**Senaryo:** Aynı anda 10 guest user dashboard'a erişirse performans nasıl?
**Öncelik:** MEDIUM
**Kategori:** Performance
**Test Adımları:**
1. 10 guest user aynı anda login olur
2. Dashboard'a erişir
3. Response time ölçülür
**Beklenen:** Response time 2 saniyenin altında olmalı

### FUTURE-008: Concurrent Site Update
**Senaryo:** 2 kullanıcı aynı anda aynı site'ı güncellerse data kaybı var mı?
**Öncelik:** HIGH
**Kategori:** Concurrency
**Test Adımları:**
1. User1 site adını "Site A" yapar
2. User2 aynı anda site adını "Site B" yapar
3. Son durum kontrol edilir
**Beklenen:** Last write wins (EF Core concurrency token ile kontrol edilmeli)

### FUTURE-009: Duplicate Setup Creation
**Senaryo:** Device aynı anda birden fazla kuruluma (setup) eklenmeye çalışılırsa ne oluyor?
**Öncelik:** MEDIUM
**Kategori:** Concurrency
**Durum:** Kod kontrol edildi (CreateSetup.cs:95-99), "device already in use" kontrolü var

### FUTURE-010: Race Condition on Data Insert
**Senaryo:** Aynı timestamp'te 2 farklı data gelirse hangisi kaydediliyor?
**Öncelik:** HIGH
**Kategori:** Concurrency
**Test Adımları:**
1. Device aynı timestamp'te 2 farklı PowerFlow data gönderir
2. Database kontrol edilir
**Beklenen:** Upsert logic devreye girmeli, son gelen data kaydedilmeli (CreateDeviceEnergyData zaten upsert yapıyor)

---

## 🕐 Time, Timezone & Data Consistency

### FUTURE-011: Device Clock Change
**Senaryo:** Cihazın saati değiştiğinde cihazın grafikleri nasıl güncellenecek?
**Öncelik:** HIGH
**Kategori:** Time
**Test Adımları:**
1. Device saati 2025-01-07 10:00
2. Data kaydediliyor
3. Device saati 2025-01-07 09:00'a alınıyor
4. Yeni data kaydediliyor
**Beklenen:** Server UTC timestamp kullanmalı, device local time sadece gösterim için

### FUTURE-012: Timezone Change
**Senaryo:** Timezone değiştiğinde eski data nasıl gösteriliyor?
**Öncelik:** MEDIUM
**Kategori:** Time
**Test Adımları:**
1. Site timezone: "Europe/Istanbul"
2. Data kaydediliyor
3. Site timezone: "America/New_York" olarak değiştiriliyor
4. Dashboard grafik kontrol ediliyor
**Beklenen:** Eski data yeni timezone'a göre convert edilmeli

### FUTURE-013: Daylight Saving Time
**Senaryo:** Daylight saving time (yaz saati) değişikliğinde data kaybı var mı?
**Öncelik:** MEDIUM
**Kategori:** Time
**Test Tarihi:** Mart/Ekim (yaz saati değişim zamanı)
**Beklenen:** UTC storage sayesinde data kaybı olmamalı

### FUTURE-014: Time Sync Mismatch
**Senaryo:** Device local time ile server UTC time senkronizasyon sorunu var mı?
**Öncelik:** HIGH
**Kategori:** Time
**Test Adımları:**
1. Device saati 1 saat ileri
2. Data gönderiliyor
3. Server timestamp ile karşılaştırılıyor
**Beklenen:** Server UTC timestamp kullanmalı, device timestamp sadece referans

### FUTURE-015: Device Clock Drift
**Senaryo:** Device 1 saat geriden geliyorsa (saat hatası) data nasıl kaydediliyor?
**Öncelik:** MEDIUM
**Kategori:** Time
**Beklenen:** Server timestamp kullanılmalı, device timestamp validation yapılmalı (±1 saat tolerance)

---

## ⚡ Device Operation & Error Handling

### FUTURE-016: Inverter Error Code Display
**Senaryo:** Inverter hata kodu verdiğinde (F56_DC_low_voltage, F57, vb.) dashboard'da gösteriliyor mu?
**Öncelik:** HIGH
**Kategori:** Device
**İyileştirme:**
- Dashboard'da inverter error code widget ekle
- Error code açıklamaları göster
- Notification gönder

### FUTURE-017: Battery SOC Zero
**Senaryo:** Battery SOC %0'a düştüğünde sistem nasıl davranıyor?
**Öncelik:** HIGH
**Kategori:** Device
**Beklenen:**
- Battery cutoff voltage koruması devreye girmeli
- Load inverter'dan beslenmeli (grid fallback)
- Dashboard'da uyarı gösterilmeli

### FUTURE-018: Grid Blackout Data Logging
**Senaryo:** Grid kesintisi sırasında (blackout) data kaydı devam ediyor mu?
**Öncelik:** HIGH
**Kategori:** Device
**Test Adımları:**
1. Grid online, inverter çalışıyor
2. Grid kesilir
3. Battery'den besleniyor
4. Data ThingsBoard'a gönderiliyor mu?
**Beklenen:** Device WiFi bağlantısı varsa data göndermeye devam etmeli

### FUTURE-019: Inverter Restart Recovery
**Senaryo:** Inverter restart attığında data kaybı oluyor mu?
**Öncelik:** MEDIUM
**Kategori:** Device
**Test Adımları:**
1. Inverter çalışıyor, data gönderiliyor
2. Inverter restart edilir
3. Restart sonrası data kontrolü
**Beklenen:** Cumulative data korunmalı, restart öncesi son değerler kayıtlı olmalı

### FUTURE-020: Peak Load Overload
**Senaryo:** Load demand çok yüksek olduğunda (peak load) inverter overload hatası veriyor mu?
**Öncelik:** MEDIUM
**Kategori:** Device
**Test Adımları:**
1. Inverter rated power: 5kW
2. Load demand: 6kW
3. Sistem tepkisi kontrol edilir
**Beklenen:** Overload protection devreye girmeli, notification gönderilmeli

### FUTURE-021: Battery SOH Warning
**Senaryo:** Battery SOH (State of Health) %50'nin altına düştüğünde uyarı var mı?
**Öncelik:** LOW
**Kategori:** Device
**Beklenen:**
- Dashboard'da SOH gösterilmeli
- %50 altında uyarı verilmeli
- Battery replacement önerisi gösterilmeli

---

## 💾 Data Integrity & Long-Term Operation

### FUTURE-022: Long-Term Offline Recovery
**Senaryo:** Device 1 ay offline kaldıktan sonra online olursa ne oluyor?
**Öncelik:** MEDIUM
**Kategori:** Data Integrity
**Test Adımları:**
1. Device 1 ay offline
2. Device online olur
3. Bulk data sync var mı?
**Beklenen:** 1 aylık data loss olabilir (local storage kapasitesi sınırlı)

### FUTURE-023: Large Scale Performance
**Senaryo:** Dashboard 100+ site varken yavaşlıyor mu?
**Öncelik:** HIGH
**Kategori:** Performance
**Durum:** ⚠️ PERF-001'de tespit edildi - N+1 query problemi var, optimize edilmeli

### FUTURE-024: Long-Term Data Query Performance
**Senaryo:** 1 yıllık data biriktiğinde query performansı nasıl?
**Öncelik:** MEDIUM
**Kategori:** Performance
**Test Adımları:**
1. Database'de 1 yıllık DeviceEnergyData biriktir (simülasyon)
2. Dashboard grafik query time ölçülür
**Beklenen:** Index'ler optimize edilmeli (DeviceId, DataType, DeviceLocalTime)

### FUTURE-025: Data Aggregation
**Senaryo:** Grafik data 1 saatlik/günlük/aylık interval'lerde aggregate ediliyor mu?
**Öncelik:** MEDIUM
**Kategori:** Performance
**İyileştirme:**
- Hourly/Daily/Monthly aggregate table'lar oluştur
- Background job ile aggregate hesapla
- Dashboard'da aggregate data kullan

### FUTURE-026: Large Database Backup
**Senaryo:** Database 10GB'ı aştığında backup/restore süresi ne kadar?
**Öncelik:** LOW
**Kategori:** Operations
**Test:** pg_dump ile backup alınıp restore süresi ölçülür
**Beklenen:** Backup < 10 dakika, Restore < 30 dakika

---

## 🔐 Security & Token Management

### FUTURE-027: JWT Token Expiry Handling
**Senaryo:** JWT token expire olduktan sonra frontend ne yapıyor?
**Öncelik:** HIGH
**Kategori:** Security
**Test Adımları:**
1. User login olur (token alır)
2. Token expire olur (1 saat sonra)
3. API request atılır
**Beklenen:** Frontend auto-refresh token mekanizması olmalı

### FUTURE-028: Device Token Theft
**Senaryo:** Device token çalınırsa ne oluyor?
**Öncelik:** HIGH
**Kategori:** Security
**İyileştirme:**
- Token revoke mekanizması ekle
- Device fingerprinting ekle (ThingsBoardDeviceId + IP validation)
- Suspicious activity detection

### FUTURE-029: Admin Role Revocation
**Senaryo:** Admin yetkisi kaldırıldığında mevcut session'ı hemen kapanıyor mu?
**Öncelik:** MEDIUM
**Kategori:** Security
**Test Adımları:**
1. User admin rolüne sahip, login olmuş
2. Admin yetkisi kaldırılır
3. User admin endpoint'e istek atar
**Beklenen:** 403 Forbidden almalı (role check runtime'da çalışmalı)

### FUTURE-030: Email Change Impact
**Senaryo:** Email değişikliği sonrası pending invitation'lar ne oluyor?
**Öncelik:** LOW
**Kategori:** Security
**Test Adımları:**
1. UserA UserB'yi davet eder (email: old@example.com)
2. UserB email'ini değiştirir (new@example.com)
3. UserB daveti kabul etmeye çalışır
**Beklenen:** Email match başarısız olur, InvitedUserId match ile kabul edilebilmeli

### FUTURE-031: Brute Force Protection
**Senaryo:** 100 kez yanlış şifre girilirse hesap kilitleniyor mu?
**Öncelik:** HIGH
**Kategori:** Security
**İyileştirme:**
- ASP.NET Core Identity lockout kullan
- 5 başarısız denemede 15 dakika lockout
- Rate limiting ekle

### FUTURE-032: Device Serial Brute Force
**Senaryo:** Device SerialNumber/QRCode brute force denenirse rate limiting var mı?
**Öncelik:** MEDIUM
**Kategori:** Security
**İyileştirme:**
- DeviceLogin endpoint'ine rate limiting ekle
- IP bazlı throttling (10 req/min)
- Failed attempt logging

---

## 🔄 Multi-Device & Scalability

### FUTURE-033: Multiple Devices Per Site
**Senaryo:** 1 site'ta 2 device varsa dashboard nasıl gösteriyor?
**Öncelik:** HIGH
**Kategori:** Feature
**Durum:** Şu an 1 site = 1 setup = 1 device destekleniyor
**İyileştirme:** Multi-device support ekle (1 site = N setup = N device)

### FUTURE-034: Device Replacement
**Senaryo:** Device değiştirildiğinde (eskisi çıkarılıp yenisi eklendiğinde) eski data ne oluyor?
**Öncelik:** MEDIUM
**Kategori:** Data Integrity
**Test Adımları:**
1. Device A ekli, 1 ay data biriktirmiş
2. Device A çıkarılır, Device B eklenir
3. Dashboard kontrol edilir
**Beklenen:** Eski data korunmalı, device geçmişi gösterilmeli

### FUTURE-035: Device Sharing Across Sites
**Senaryo:** Aynı device farklı site'lara eklenmeye çalışılırsa ne oluyor?
**Öncelik:** LOW
**Kategori:** Validation
**Durum:** ✅ EDGE-003'te test edildi, engelleniyor ("device already in use")

### FUTURE-036: Admin Panel Scalability
**Senaryo:** 1000 site olduğunda admin panel performansı nasıl?
**Öncelik:** MEDIUM
**Kategori:** Performance
**Test:** Load test (JMeter, k6)
**Beklenen:** Pagination optimize edilmeli, lazy loading kullanılmalı

---

## 📧 Notification & Email

### FUTURE-037: SMTP Server Downtime
**Senaryo:** SMTP server down olduğunda invitation email gönderilemiyor, sistem nasıl davranıyor?
**Öncelik:** MEDIUM
**Kategori:** Integration
**Durum:** Exception fırlatılıyor (CreateInvitation.cs:105-110)
**İyileştirme:**
- Email queue mekanizması ekle (Hangfire job)
- Retry policy ekle
- Failed email logging

### FUTURE-038: Email Bounce Handling
**Senaryo:** Email bounce olursa (geçersiz email) ne oluyor?
**Öncelik:** LOW
**Kategori:** Integration
**İyileştirme:**
- SMTP bounce notification dinle
- Invitation status'u "Failed" yap
- User'a bildirim gönder

### FUTURE-039: Spam Protection
**Senaryo:** 1 kullanıcıya 100 davetiye gönderilirse spam koruması var mı?
**Öncelik:** MEDIUM
**Kategori:** Security
**İyileştirme:**
- Rate limiting ekle (1 user max 10 invitation/day)
- Duplicate email kontrolü ekle

### FUTURE-040: Notification Cleanup
**Senaryo:** Notification 1 ay okunmadıysa otomatik siliniyor mu?
**Öncelik:** LOW
**Kategori:** Maintenance
**İyileştirme:** Background job ekle (Hangfire), 30 gün sonra IsRead=false olanları sil

### FUTURE-041: Push Notification
**Senaryo:** Push notification entegrasyonu var mı?
**Öncelik:** MEDIUM
**Kategori:** Feature
**İyileştirme:** Firebase Cloud Messaging (FCM) ekle

---

## 📊 Data Export & Reporting

### FUTURE-042: Energy Data Export
**Senaryo:** 1 yıllık enerji verisi export edilebiliyor mu?
**Öncelik:** MEDIUM
**Kategori:** Feature
**İyileştirme:**
- CSV export endpoint ekle
- Excel export (EPPlus library)
- Date range filter ekle

### FUTURE-043: Real-Time Dashboard Update
**Senaryo:** Dashboard real-time update var mı yoksa manuel refresh mi gerekiyor?
**Öncelik:** HIGH
**Kategori:** Feature
**Durum:** SignalR hub var (DeviceNotificationHub), enerji data için extend edilmeli

### FUTURE-044: Automated Reports
**Senaryo:** Günlük/haftalık/aylık enerji raporları otomatik mail ile gönderilebiliyor mu?
**Öncelik:** LOW
**Kategori:** Feature
**İyileştirme:**
- Hangfire recurring job ekle
- PDF report generator (iTextSharp)
- Email ile gönder

### FUTURE-045: Energy Saving Calculation
**Senaryo:** Energy saving hesaplaması yapılıyor mu?
**Öncelik:** MEDIUM
**Kategori:** Feature
**İyileştirme:**
- Grid cost vs Solar cost karşılaştırması
- Monthly savings report
- ROI calculator

---

## 🔄 Backup, Recovery & Disaster Scenarios

### FUTURE-046: Database Restore Test
**Senaryo:** Database backup alındıktan sonra restore test edildi mi?
**Öncelik:** HIGH
**Kategori:** Operations
**Test:**
- pg_dump ile backup al
- Test DB'ye restore et
- Data integrity kontrol et

### FUTURE-047: Soft Delete Recovery
**Senaryo:** Site silinip geri yüklense (soft delete'ten recovery) setup'ı da geri geliyor mu?
**Öncelik:** LOW
**Kategori:** Data Integrity
**Test Adımları:**
1. Site soft delete edilir (IsDeleted=true)
2. Site restore edilir (IsDeleted=false)
3. Setup ve Device linkler kontrol edilir
**Beklenen:** Cascade restore mekanizması olmalı

### FUTURE-048: Orphan Record Cleanup
**Senaryo:** Invitation silinse ama UserSite kaydı kalsa ne oluyor?
**Öncelik:** LOW
**Kategori:** Data Integrity
**İyileştirme:** Background job ile orphan record cleanup

### FUTURE-049: Transaction Rollback
**Senaryo:** PostgreSQL crash olursa transaction rollback doğru çalışıyor mu?
**Öncelik:** MEDIUM
**Kategori:** Data Integrity
**Test:** PostgreSQL container kill et, transaction durumunu kontrol et

### FUTURE-050: Cache Layer Consistency
**Senaryo:** Redis/cache layer eklenirse data consistency nasıl sağlanacak?
**Öncelik:** LOW
**Kategori:** Performance
**İyileştirme:**
- Cache invalidation strategy belirle
- Cache-aside pattern kullan
- TTL optimize et

---

## 📱 Mobile App Specific

### FUTURE-051: Deep Link Invitation
**Senaryo:** Deep link ile davetiye kabul akışı çalışıyor mu?
**Öncelik:** HIGH
**Kategori:** Mobile
**Durum:** Email template'de intent:// scheme var (InvitationEmailService.cs:54)
**Test:** Android/iOS'ta deep link test et

### FUTURE-052: Offline Mode
**Senaryo:** Offline modda dashboard açılıyor mu?
**Öncelik:** MEDIUM
**Kategori:** Mobile
**İyileştirme:**
- Local cache (SQLite)
- Offline indicator göster
- Sync when online

### FUTURE-053: Background Notification
**Senaryo:** Background'da iken notification geliyor mu?
**Öncelik:** MEDIUM
**Kategori:** Mobile
**Test:** App background'dayken invitation gönder, notification geldi mi kontrol et

### FUTURE-054: Battery Saver Mode
**Senaryo:** Düşük pil modunda data sync kapatılıyor mu?
**Öncelik:** LOW
**Kategori:** Mobile
**İyileştirme:** Background sync frequency'yi düşür (1 saat → 6 saat)

---

## 🔌 API & Integration

### FUTURE-055: ThingsBoard Dispatcher Kind Validation
**Senaryo:** ThingsBoard dispatcher kind (SOC, PowerFlow, vb.) yanlış gelirse ne oluyor?
**Öncelik:** MEDIUM
**Kategori:** Integration
**Test Adımları:**
1. ThingsBoard'dan yanlış kind ile data gelir (ör: "InvalidKind")
2. Backend reaction kontrol edilir
**Beklenen:** Validation yapılmalı, unknown kind loglansın

### FUTURE-056: Node-RED RPC Timeout
**Senaryo:** Node-RED RPC timeout süresi ne kadar?
**Öncelik:** MEDIUM
**Kategori:** Integration
**Test:** GetLivePowerFlow endpoint'ine istek at, Node-RED cevap vermezse timeout süresi ölç
**Beklenen:** 30 saniye timeout, graceful error handling

### FUTURE-057: Weather API Rate Limit
**Senaryo:** Weather API rate limit aşılırsa ne oluyor?
**Öncelik:** LOW
**Kategori:** Integration
**Test:** 1000 req/hour göndererek rate limit test et
**Beklenen:** 429 Too Many Requests, graceful degradation

### FUTURE-058: Third-Party Analytics
**Senaryo:** Third-party analytics entegrasyonu var mı?
**Öncelik:** LOW
**Kategori:** Feature
**İyileştirme:**
- Google Analytics ekle
- User behavior tracking
- Error tracking (Sentry)

---

## 💼 Business Logic & Edge Cases

### FUTURE-059: Owner Role Transfer
**Senaryo:** Site Owner rolü başka birine devredilebiliyor mu?
**Öncelik:** LOW
**Kategori:** Feature
**İyileştirme:**
- TransferOwnership command ekle
- Eski Owner → Guest role yap
- Yeni Owner → Owner role yap

### FUTURE-060: Role Hierarchy
**Senaryo:** Guest kullanıcı Owner'dan daha fazla site'a erişebilir mi?
**Öncelik:** LOW
**Kategori:** Business Logic
**Test:** Guest 10 site'e erişsin, Owner 1 site'e sahip olsun, dashboard'ları karşılaştır

### FUTURE-061: Device Warranty Tracking
**Senaryo:** Device warranty bilgisi tutuluyor mu?
**Öncelik:** LOW
**Kategori:** Feature
**İyileştirme:**
- Device.WarrantyExpiryDate ekle
- Dashboard'da warranty status göster
- Expiry notification gönder

### FUTURE-062: Energy Tariff Management
**Senaryo:** Energy tariff (TOU pricing) değişikliği yapılabiliyor mu?
**Öncelik:** MEDIUM
**Kategori:** Feature
**İyileştirme:**
- EnergyTariff entity ekle (peak/off-peak pricing)
- Dashboard'da cost calculation göster
- Monthly bill estimate

---

## 📝 Test Senaryoları Özeti

**Toplam Yeni Senaryo:** 62 adet

**Öncelik Dağılımı:**
- HIGH: 18 senaryo
- MEDIUM: 28 senaryo
- LOW: 16 senaryo

**Kategori Dağılımı:**
- Network & Connectivity: 5
- Concurrency: 5
- Time & Timezone: 5
- Device Operation: 6
- Data Integrity: 5
- Security: 6
- Scalability: 4
- Notification: 5
- Reporting: 4
- Backup & Recovery: 5
- Mobile: 4
- Integration: 4
- Business Logic: 4

**Önerilen Test Sırası:**
1. HIGH öncelikli network/security testleri
2. Device operation testleri (gerçek cihaz ile)
3. Concurrency & performance testleri (load test)
4. Feature geliştirme gerektiren testler (son aşama)

---

**Rapor Tarihi:** 2026-01-07
**Rapor Versiyonu:** 1.0
