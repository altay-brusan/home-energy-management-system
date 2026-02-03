# HEMS Backend - Sistem Geneli Test Senaryoları

**Not:** Bu dosya invitation/sharing dışındaki sistem testlerini içerir. Ana test planından ayrıdır.

**Durum Kodları:**
- ✅ Test Geçti
- ❌ Test Kaldı
- ⏳ Devam Ediyor
- 📝 Test Yapılacak
- ⚠️ Açıklama Gerekli

---

## 🔧 DEVICE LIFECYCLE (Cihaz Yaşam Döngüsü)

### DL-001: Device Satıldı/Değiştirildi - Sistem Sıfırlama

**Test ID:** DL-001
**Öncelik:** MEDIUM
**Kategori:** Device Lifecycle

**Senaryo:**
UserA inverter'ı sattı, yeni bir inverter aldı. Eski device'ın verileri ne olmalı?

**Test Adımları:**
1. UserA'nın SiteA'sı var, DeviceX bağlı (SetupDevice kaydı mevcut)
2. UserA DeviceX'i sattı (veya değiştirdi)
3. UserA DeviceY'yi (yeni cihaz) sisteme eklemek istiyor
4. DeviceX'in eski verileri kontrol et

**Beklenen Sonuç:**
- DeviceX soft delete yapılmalı (IsDeleted=true)
- DeviceX'in energy data'sı korunmalı (historical data - silmemeli)
- SetupDevice kaydı silinmeli veya orphan kalmamalı
- DeviceY yeni SetupDevice kaydı ile eklenebilmeli
- Dashboard'da sadece DeviceY'nin verileri görünmeli
- Eski device'ın data'sı export edilebilmeli (PDF rapor, historical view)

**Mevcut Durum/Bug Açıklaması:**

**Açıklama/Not:**
Kullanıcı cihaz değiştirdiğinde:
- Historical data kaybolmamalı (compliance, warranty tracking için gerekli)
- Yeni cihaz eski verilerle karışmamalı
- "Önceki cihazlarım" gibi bir özellik düşünülebilir

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** N/A

---

### DL-002: Device Arızalandı/Servis - Offline Period

**Test ID:** DL-002
**Öncelik:** LOW
**Kategori:** Device Lifecycle

**Senaryo:**
Cihaz arızalandı, servise gitti, 2 hafta sonra gelecek. Dashboard ne göstermeli?

**Test Adımları:**
1. UserA'nın DeviceX'i çalışıyor
2. DeviceX arızalandı, servis için gönderildi
3. 2 hafta boyunca device data göndermiyor
4. Dashboard'ı kontrol et

**Beklenen Sonuç:**
- Dashboard "Last Sync: 14 days ago" göstermeli
- Eski data hala görüntülenebilmeli (historical charts)
- "Device offline" veya "No recent data" uyarısı göstermeli
- Device geri gelince (data gelmeye başlayınca) otomatik çalışmaya devam etmeli

**Mevcut Durum/Bug Açıklaması:**

**Açıklama/Not:**
Offline detection mekanizması var mı? Kaç saat/gün veri gelmezse "offline" sayılır?

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** N/A

---

### DL-003: Device Factory Reset - Data Persistence

**Test ID:** DL-003
**Öncelik:** LOW
**Kategori:** Device Lifecycle

**Senaryo:**
Device factory reset yapıldı, sistem historical data'yı koruyor mu?

**Test Adımları:**
1. DeviceX'in 6 aylık verisi var (DeviceEnergyData tablosunda)
2. Device factory reset yapıldı
3. Device yeniden bağlandı (aynı serial number)
4. Eski veriler görünüyor mu?

**Beklenen Sonuç:**
- Historical data korunmalı (DeviceEnergyData tablosu değişmemeli)
- Device credentials değişirse yeni login gerekebilir
- ThingsBoard'da device ID aynı kalmalı
- Dashboard historical charts'da eski veriler görünmeli

**Mevcut Durum/Bug Açıklaması:**

**Açıklama/Not:**
Factory reset fiziksel cihazda yapılır, backend'de data silinmemeli.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** N/A

---

### DL-004: ✅ Aynı Device Birden Fazla Site'de Kullanılamıyor

**Test ID:** DL-004
**Öncelik:** LOW
**Kategori:** Device Lifecycle
**Durum:** ✅ Test Edildi - Kontrol Mevcut

**Senaryo:**
UserA DeviceX'i SiteA'da kullanıyor, UserB aynı device'ı SiteB'ye eklemeye çalışıyor.

**Test Adımları:**
1. UserA DeviceX'i SiteA'ya bağladı (SetupDevice kaydı oluştu)
2. UserB aynı DeviceX'i (serial number ile) SiteB'ye bağlamaya çalışır
3. CreateSetup çağrılır

**Beklenen Sonuç:**
- Hata: "This device is already in use"
- CreateSetup kontrolü var (kod: `CreateSetup.cs:96-99`)

**Mevcut Durum/Bug Açıklaması:**
Kod mevcut:
```csharp
if (await _context.SetupDevices.AnyAsync(f => f.DeviceId == device.Id, cancellationToken))
{
    throw new ValidationException("This device is already in use.");
}
```

**Açıklama/Not:**
Test edildi, kontrol çalışıyor. Bu testi tekrar çalıştırıp doğrula.

**Test Durumu:** ✅ Test Geçti

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** CreateSetup.cs:96-99

---

## ⚡ ENERGY DATA (Enerji Verileri)

### ED-001: Duplicate Energy Data - Timestamp Kontrolü

**Test ID:** ED-001
**Öncelik:** HIGH
**Kategori:** Energy Data

**Senaryo:**
Aynı device aynı timestamp için iki kez data gönderiyor. Upsert çalışıyor mu?

**Test Adımları:**
1. DeviceX saat 10:00'da energy data gönderir (GridImportEnergy: 10 kWh, DataType: DailyCumulativeEnergyData)
2. 5 dakika sonra aynı timestamp (10:00) için tekrar data gönderir (GridImportEnergy: 12 kWh, aynı DataType)
3. Database'i kontrol et: Kaç kayıt var?

**Beklenen Sonuç:**
- **Upsert olmalı:** Mevcut kayıt güncellenmeli (10 kWh → 12 kWh)
- Duplicate kayıt oluşmamalı
- Unique constraint: (DeviceId + Timestamp + DataType) kombinasyonu

**Mevcut Durum/Bug Açıklaması:**
CLAUDE.md'de "upserts record (same device + datetime + dataType = update, else insert)" yazıyor.
CreateDeviceEnergyDataCommand handler'ını incele - upsert logic var mı?

**Açıklama/Not:**
**GÜNCELLENDİ:** Timestamp aynı gelmeli, bunun kontrolü olabilir.
- Primary key veya unique index kontrolü yapılmalı
- EF Core `AddOrUpdate` veya manuel upsert logic

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** CreateDeviceEnergyData handler

---

### ED-002: Negative Energy Values - Validation Listesi

**Test ID:** ED-002
**Öncelik:** MEDIUM
**Kategori:** Energy Data - Validation

**Senaryo:**
Device negatif enerji değerleri gönderiyor. Hangi field'lar negatif olabilir, hangileri olamaz?

**Test Adımları:**
Bu testler manuel olarak çalıştırılmayacak. Sadece validation kurallarının listesi:

**Beklenen Sonuç (Validation Kuralları):**

✅ **Negatif Olabilir:**
- GridImportEnergy / GridExportEnergy (akış yönü)
- BatteryChargeEnergy / BatteryDischargeEnergy (+ veya - işaretli olabilir)

❌ **Negatif Olamaz:**
- PvTotalEnergy (PV üretimi her zaman pozitif veya 0)
- BatterySOC (0-100 arası olmalı)
- BatterySOH (0-100 arası olmalı)
- Voltage, Current (fiziksel kısıtlar)

**Mevcut Durum/Bug Açıklaması:**
Validation kuralları FluentValidation ile eklenmeli:
```csharp
RuleFor(x => x.PvTotalEnergyKWh)
    .GreaterThanOrEqualTo(0)
    .When(x => x.PvTotalEnergyKWh.HasValue);

RuleFor(x => x.BatterySOC)
    .InclusiveBetween(0, 100)
    .When(x => x.BatterySOC.HasValue);
```

**Açıklama/Not:**
**GÜNCELLENDİ:** Sadece liste olarak yazıldı, testleri yazmıyoruz. Validation kuralları kod olarak eklenmeli.

**Test Durumu:** 📝 Validation Kuralları Belge

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** CreateDeviceEnergyDataCommandValidator

---

### ED-003: Future Timestamp Data - Validation

**Test ID:** ED-003
**Öncelik:** MEDIUM
**Kategori:** Energy Data

**Senaryo:**
Device gelecek tarihli timestamp gönderiyor (cihaz saati yanlış).

**Test Adımları:**
1. DeviceX timestamp: 2027-01-01 00:00:00 ile data gönderir (1 yıl ileri)
2. CreateDeviceEnergyData çağrılır
3. Hata alıyor mu?

**Beklenen Sonuç:**
- Validation error: "Invalid timestamp - future date not allowed"
- Tolerance: Max 1 saat ileri (clock skew için)
- Hata sonrası device saati düzeltilmeli (manuel müdahale)

**Mevcut Durum/Bug Açıklaması:**
```csharp
RuleFor(x => x.DeviceLocalTime)
    .LessThanOrEqualTo(DateTime.UtcNow.AddHours(1))
    .WithMessage("Timestamp cannot be in the future");
```

**Açıklama/Not:**
Future timestamp genellikle device saat hatası. Log'lanmalı ve user'a bildirilmeli.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** CreateDeviceEnergyDataCommandValidator

---

### ED-004: ⏳ DataType Case Sensitivity (Düşük Öncelik)

**Test ID:** ED-004
**Öncelik:** LOW
**Kategori:** Energy Data

**Senaryo:**
Device farklı DataType gönderiyor (case farkı veya typo).

**Test Adımları:**
1. Beklenen: "DailyCumulativeEnergyData"
2. Gelen: "dailycumulativeenergydata" veya "DailyCummulativeEnergyData"
3. Sistem kabul ediyor mu?

**Beklenen Sonuç:**
- DataType validation (enum veya constant list)
- Case-insensitive comparison olabilir
- Invalid DataType → reject

**Mevcut Durum/Bug Açıklaması:**
CLAUDE.md: "DataType case sensitivity: DeviceEnergyData queries filter by exact DataType string"

**Açıklama/Not:**
**GÜNCELLENDİ:** ThingsBoard'dan gönderiyor, öncelik düşük ama dursun şimdilik.
DataType enum olarak tanımlanabilir veya constant list.

**Test Durumu:** ⏳ Düşük Öncelik

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** DeviceEnergyData model

---

### ED-005: Missing/Null Energy Fields - Frontend Handling

**Test ID:** ED-005
**Öncelik:** MEDIUM
**Kategori:** Energy Data

**Senaryo:**
Device bazı energy field'larını null gönderiyor. Frontend'e nasıl gönderilmeli?

**Test Adımları:**
1. DeviceX PvTotalEnergyKWh göndermiyor (null)
2. Dashboard query'leri çağrılıyor (GetEnergyProduction, GetHero, vs.)
3. Response'da null field nasıl?

**Beklenen Sonuç:**
**GÜNCELLENDİ:** ThingsBoard'dan null olarak gönderilen parametreler:
- **Seçenek 1:** Hiç gönderilmemeli (field response'da olmamalı)
- **Seçenek 2:** 0 gönderilebilen bir değer ise 0 olarak gönderilmeli

Örnek:
```json
// Seçenek 1 (field yok):
{
  "gridImportEnergy": 10.5,
  "batterySOC": 80
  // pvTotalEnergy yok (null ise)
}

// Seçenek 2 (0 gönder):
{
  "gridImportEnergy": 10.5,
  "batterySOC": 80,
  "pvTotalEnergy": 0
}
```

**Mevcut Durum/Bug Açıklaması:**
Dashboard query'lerinde null coalesce var mı?
```csharp
.Select(x => new EnergyDataDto
{
    PvTotalEnergy = x.PvTotalEnergyKWh ?? 0  // veya null bırakılmalı mı?
})
```

**Açıklama/Not:**
Frontend'de null handling için karar gerekli. Hangi field'lar optional?

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** Dashboard query handlers

---

## 📊 DASHBOARD DATA (Panel Verileri)

### DD-001: Hiç Veri Yokken Dashboard - Bekleme Mesajı

**Test ID:** DD-001
**Öncelik:** MEDIUM
**Kategori:** Dashboard

**Senaryo:**
Yeni device kuruldu, henüz veri göndermedi. Dashboard ne göstermeli?

**Test Adımları:**
1. UserA DeviceX'i yeni kurdu (son 1 saat içinde)
2. Device henüz data göndermedi (veya 1-2 veri var)
3. GetHero, GetEnergyProduction, GetBattery query'leri çağrılıyor
4. Response kontrol et

**Beklenen Sonuç:**
**GÜNCELLENDİ:** "No data available" yerine:
- **Chart'lar için:** "Device kurulduktan 24 saat sonra oluşacak" tarzında mesaj
- **Anlık değerler için:** "Veri bekleniyor..." veya "0"
- Dashboard crash etmemeli (null reference exception)
- Empty charts: Placeholder message göstermeli

Örnek mesajlar:
```
"Enerji üretim grafiği 24 saat içinde oluşacaktır"
"Cihazınız veri göndermeye başladığında burada gösterilecek"
"Son 7 günlük veriler yeterli olduğunda grafik görüntülenecek"
```

**Mevcut Durum/Bug Açıklaması:**
Dashboard handler'lar null/empty response için kontrol yapıyor mu?
Default values dönüyor mu yoksa exception mı fırlatıyor?

**Açıklama/Not:**
Chart type'ına göre farklı bekleme süreleri:
- Daily chart: 24 saat
- Weekly chart: 7 gün
- Monthly chart: 30 gün

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** Dashboard query handlers (GetHero, GetEnergyProduction, etc.)

---

### DD-002: Incomplete Data - Frontend Handling

**Test ID:** DD-002
**Öncelik:** MEDIUM
**Kategori:** Dashboard

**Senaryo:**
Device sadece PV data gönderiyor, battery/grid data yok. Dashboard hesaplamaları doğru mu?

**Test Adımları:**
1. DeviceX sadece PvTotalEnergyKWh gönderiyor
2. BatteryChargeEnergyKWh, GridImportEnergyKWh null
3. Dashboard calculations kontrol et (GetHero, summary calculations)

**Beklenen Sonuç:**
**GÜNCELLENDİ:** ThingsBoard'dan null olarak gönderilen parametreler:
- **Frontend'e:** Hiç gönderilmemeli veya 0 gönderilmeli (yukarıda ED-005'te belirtildi)
- **Dashboard calculations:** Null coalescing: `?? 0` kullanılmalı
- **UI:** "Battery: N/A" veya "Grid: No data" gösterilebilir

Örnek:
```csharp
var totalEnergy = (data.PvTotalEnergyKWh ?? 0) +
                  (data.BatteryChargeEnergyKWh ?? 0) +
                  (data.GridImportEnergyKWh ?? 0);
```

**Mevcut Durum/Bug Açıklaması:**
Tüm dashboard query'leri null-safe mi?

**Açıklama/Not:**
ED-005 ile ilişkili. Null handling stratejisi tutarlı olmalı.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** Dashboard query handlers

---

### DD-003: Date Range Validation - Geçersiz Tarih

**Test ID:** DD-003
**Öncelik:** LOW
**Kategori:** Dashboard

**Senaryo:**
User geçersiz date range seçiyor (start > end veya future date).

**Test Adımları:**
1. GetEnergyProduction(period="daily", startDate="2027-01-01", endDate="2026-01-01")
2. Start date > End date
3. Veya future date seçilmiş

**Beklenen Sonuç:**
- Validation error: "Invalid date range"
- Start date <= End date olmalı
- End date > UtcNow ise reject (future date seçilemez)

**Mevcut Durum/Bug Açıklaması:**
```csharp
RuleFor(x => x.StartDate)
    .LessThanOrEqualTo(x => x.EndDate)
    .WithMessage("Start date must be before or equal to end date");

RuleFor(x => x.EndDate)
    .LessThanOrEqualTo(DateTime.UtcNow)
    .WithMessage("End date cannot be in the future");
```

**Açıklama/Not:**
Dashboard query'lerinde date range parametresi varsa validation eklenmeli.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** Dashboard query validators

---

### DD-004: Dashboard Query Performance - Büyük Veri Seti

**Test ID:** DD-004
**Öncelik:** LOW
**Kategori:** Performance

**Senaryo:**
1 yıllık veri var (105K kayıt), dashboard yavaş yükleniyor.

**Test Adımları:**
1. Device 1 yıldır her 5 dakikada bir data gönderiyor (~105,000 kayıt)
2. GetEnergyProduction(period="yearly") çağrılıyor
3. Query execution time ölç

**Beklenen Sonuç:**
- Query optimization (indexing: DeviceId, Timestamp, DataType)
- Aggregation kullanılmalı (günlük/aylık summary table)
- Response time < 2 saniye

**Mevcut Durum/Bug Açıklaması:**
Yearly query aggregation yapıyor mu yoksa tüm kayıtları mı çekiyor?

**Açıklama/Not:**
Database indexing:
```sql
CREATE INDEX IX_DeviceEnergyData_Lookup
ON DeviceEnergyData(ThingsBoardDeviceId, DeviceLocalTime, DataType);
```

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** DeviceEnergyData table, GetEnergyProduction handler

---

## 🔐 DEVICE AUTHENTICATION

### DA-002: ✅ Multiple Device Same Serial Number (Test Edildi)

**Test ID:** DA-002
**Öncelik:** LOW
**Kategori:** Device Authentication
**Durum:** ✅ Test Edildi - Olmuyor

**Senaryo:**
İki farklı device aynı serial number'a sahip olamaz.

**Test Adımları:**
1. DeviceX serial: "ABC123" ile login
2. DeviceY serial: "ABC123" ile login (farklı fiziksel cihaz)
3. Sistem ne yapıyor?

**Beklenen Sonuç:**
- Serial number unique olmalı
- ThingsBoard device ID kullanılmalı (unique)
- Duplicate serial number engellenmeli

**Mevcut Durum/Bug Açıklaması:**
Test edildi: Device açamıyor (duplicate serial number engelleniyor).

**Açıklama/Not:**
**GÜNCELLENDİ:** Olmuyor, device açamıyor - test ettik. Yine de yazıp "yapıldı" belirtelim.

**Test Durumu:** ✅ Test Geçti

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:** Device login - duplicate serial number engellendi

**Dosya/Satır:** DeviceLogin handler, ThingsBoard connector

---

### DA-003: Device Login Rate Limiting - Hatalı Girişlerde Bekleme

**Test ID:** DA-003
**Öncelik:** MEDIUM
**Kategori:** Device Authentication

**Senaryo:**
Device çok sık login deniyor (brute force veya bug). Rate limiting var mı?

**Test Adımları:**
1. DeviceX 1 dakika içinde 100 kez yanlış credential ile login deniyor
2. Rate limiting devreye giriyor mu?
3. Kaç deneme sonra block?

**Beklenen Sonuç:**
**GÜNCELLENDİ:** Çok hatalı girişte bekleme sınırı:
- Rate limiting: Örn: 5 başarısız deneme sonrası 5 dakika bekleme
- 429 Too Many Requests (veya device-specific error)
- Log: "Too many failed login attempts from device: {SerialNumber}"

**Mevcut Durum/Bug Açıklaması:**
CLAUDE.md'de "rate limiting middleware" var:
```csharp
app.UseMiddleware<DeviceDataRateLimitMiddleware>();
```
Login endpoint'i için de rate limit var mı?

**Açıklama/Not:**
Brute force protection için. AspNetCoreRateLimit paketi kullanılabilir.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** DeviceDataRateLimitMiddleware, DeviceLogin endpoint

---

## 🗑️ SOFT DELETE CASCADE

### SD-001: Site Silinirse Ne Olur? (Cascade Effects Listesi)

**Test ID:** SD-001
**Öncelik:** MEDIUM
**Kategori:** Data Integrity

**Senaryo:**
UserA SiteA'yı siliyor - cascade effects test et.

**Test Adımları:**
1. SiteA silinir (IsDeleted=true)
2. İlişkili kayıtları kontrol et

**Beklenen Sonuç:**
**GÜNCELLENDİ:** User site'ı silince neler silinmesini bekliyoruz:

✅ **Silinmeli (Cascade Soft Delete):**
- Setup (IsDeleted=true)
- SetupDevice (cascade delete veya IsDeleted=true)
- SetupDeviceParameterValue (cascade delete)
- UserSites kayıtları (hard delete)
- Invitations (Status=Expired veya IsDeleted=true)
- Notifications (site ile ilgili) (IsDeleted=true veya hard delete)

❌ **Silinmemeli (Historical Data):**
- Device (başka site'de kullanılabilir)
- DeviceEnergyData (historical data - compliance için gerekli)
- DeviceComponents (device'a ait)

**Mevcut Durum/Bug Açıklaması:**
EF Core cascade delete configuration kontrol et:
```csharp
modelBuilder.Entity<Setup>()
    .HasOne(s => s.Site)
    .WithOne(site => site.Setup)
    .OnDelete(DeleteBehavior.Restrict); // veya Cascade?
```

**Açıklama/Not:**
DATA-003'te de belirtilmiş. Cascade delete chain doğru yapılandırılmalı.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** EF Core configurations, DeleteSite handler

---

### SD-002: Device Soft Delete - Tekrar Kullanılabilir Mi?

**Test ID:** SD-002
**Öncelik:** MEDIUM
**Kategori:** Data Integrity

**Senaryo:**
Admin DeviceX'i soft delete yaptı. Tekrar kullanılabilir mi?

**Test Adımları:**
1. DeviceX IsDeleted=true yapılır (admin tarafından)
2. UserA DeviceX'i yeni setup'a eklemek istiyor (serial number ile)
3. CreateSetup DeviceX'i buluyor mu?

**Beklenen Sonuç:**
**GÜNCELLENDİ:** Device bazlı bak (device data değil):
- **Seçenek 1:** Soft delete = kalıcı. Device bulunamaz, "Device not found or deleted" hatası
- **Seçenek 2:** Restore mekanizması. Admin restore edebilir, sonra kullanılabilir
- **Seçenek 3:** IsDeleted=true device'lar ignore edilir, yeni device eklenmeli

Device query'lerinde:
```csharp
var device = await _context.Devices
    .FirstOrDefaultAsync(d => d.SerialNumber == request.SerialNumber && !d.IsDeleted);
```

**Mevcut Durum/Bug Açıklaması:**
DATA-004'te belirtilmiş. Business decision gerekli.

**Açıklama/Not:**
Soft delete sonrası device tekrar kullanılabilir mi? Yoksa kalıcı silme gibi mi davranmalı?

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** DeleteDevice handler, CreateSetup device lookup

---

## 🎯 BUSINESS LOGIC EDGE CASES

### BL-001: ❓ User Kendini Owner Yapabilir Mi? (Role Escalation)

**Test ID:** BL-001
**Öncelik:** ⚠️ SORU - Arayüzde Böyle Bir Özellik Yok
**Kategori:** Security

**Senaryo:**
UserB (Guest), kendi role'ünü Owner'a değiştirmeye çalışıyor.

**Soru:**
"UserB kendi UserSite kaydını Role=Owner'a update etmeye çalışır" demişsin ama **bunu nasıl yapacak ki?** Arayüzde böyle bir şey yok.

**Olası Senaryolar:**
1. **API doğrudan çağrılırsa** (Postman, curl ile):
   - PUT /api/usersites/{id} endpoint'i var mı?
   - Authorization kontrolü var mı?

2. **Database doğrudan değiştirilirse:**
   - Manuel SQL injection veya DB access
   - (Bu senaryo için backend testi geçersiz)

**Test Adımları:**
Eğer UserSite update endpoint'i varsa:
1. UserA SiteA'yı oluşturdu (Owner)
2. UserB Guest olarak eklendi (UserSite kaydı: Role=Guest)
3. UserB API ile kendi UserSite.Role'ünü Owner'a değiştirmeye çalışır
   ```bash
   PUT /api/usersites/{userBsiteId}
   { "role": "Owner" }
   ```
4. Authorization kontrolü yapılıyor mu?

**Beklenen Sonuç:**
Eğer endpoint varsa:
- 403 Forbidden dönmeli
- Sadece Site Owner (UserA) başkasının role'ünü değiştirebilir
- Admin değiştirebilir

**Mevcut Durum/Bug Açıklaması:**
UserSite update endpoint'i var mı? Handler'ı bul ve authorization kontrolünü incele.

**Açıklama/Not:**
**SORU:** Arayüzde role değiştirme özelliği yok. Bu test gerekli mi?
Eğer API endpoint'i varsa test edilmeli, yoksa bu senaryo geçersiz.

**Test Durumu:** ⚠️ Açıklama Gerekli

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** UserSite update handler (varsa)

---

### BL-002: Owner Kendini Guest Yapabilir Mi? / Site Role Değişiklikleri

**Test ID:** BL-002
**Öncelik:** LOW
**Kategori:** Business Logic

**Senaryo:**
UserA (Owner), kendi role'ünü Guest'e indirmeye çalışıyor.

**Test Adımları:**
1. UserA SiteA'nın Owner'ı
2. UserA kendini Guest yapmaya çalışır (eğer böyle bir özellik varsa)
3. İzin verilmeli mi?

**Beklenen Sonuç:**
**GÜNCELLENDİ:** Yazalım ama bunları sormak lazım aslında, tam bir cevapları yok şu anda. "Şöyle olmalı" diyemiyorum.

**Sorular:**
- Owner kendini Guest yapabilir mi?
- Site'ın en az bir Owner'ı olmalı mı? (last owner kontrolü)
- Eğer yapabilirse, uyarı gösterilmeli mi? ("You will lose admin access")

**Mevcut Durum/Bug Açıklaması:**
Business logic belirsiz. Karar verilmeli.

**Açıklama/Not:**
Low priority - edge case. Gerçek dünyada nadir görülür.

**Test Durumu:** 📝 Business Logic Kararı Bekliyor

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** N/A

---

### BL-003: Son Owner Site'den Çıkarsa Ne Olur?

**Test ID:** BL-003
**Öncelik:** MEDIUM
**Kategori:** Business Logic

**Senaryo:**
Site'ın tek Owner'ı (UserA), kendini site'den çıkarıyor. Site orphan kalır.

**Test Adımları:**
1. SiteA → Owner: UserA, Guest: UserB, UserC
2. UserA kendini çıkarır (RemoveUserFromSite)
3. Site'ın hiç Owner'ı kalmadı

**Beklenen Sonuç:**
**Seçenekler:**
1. Son Owner kendini çıkaramaz (validation error)
2. Site otomatik silinir (soft delete)
3. Guest'lerden biri otomatik Owner olur (mantıksız - manuel seçilmeli)

**Önerilen:** Seçenek 1 - Son Owner kendini çıkaramaz.

**Mevcut Durum/Bug Açıklaması:**
RemoveUserFromSite handler'ında "last owner" kontrolü var mı?
```csharp
var ownerCount = await _context.UserSites
    .CountAsync(us => us.SiteId == siteId && us.Role == UserSiteRole.Owner);

if (ownerCount == 1 && removingUser.Role == UserSiteRole.Owner)
{
    throw new InvalidOperationException("Cannot remove the last owner. Site must have at least one owner.");
}
```

**Açıklama/Not:**
LOGIC-006'da "owner konusunda kararsız" denmiş. Bu test o konuyu netleştirmeli.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** RemoveUserFromSite handler

---

### BL-004: Email Değişikliği - Invitation Geçerliliği

**Test ID:** BL-004
**Öncelik:** LOW
**Kategori:** Business Logic

**Senaryo:**
UserB email'ini değiştirdi, eski invitation geçerli mi?

**Test Adımları:**
1. UserA, userB@old.com'a davetiye gönderdi
2. UserB sistemde email'ini userB@new.com'a değiştirdi
3. UserB eski invitation token'ı ile kabul etmeye çalışır

**Beklenen Sonuç:**
- Invitation'da InvitedUserId set edilmişse, email değişse bile çalışmalı (UserId'ye bağlı)
- Eğer sadece email'e bakılıyorsa (InvitedUserId null), çalışmaz
- AcceptInvitation kontrolü: Email veya UserId match

**Mevcut Durum/Bug Açıklaması:**
AcceptInvitation handler'ı incele:
```csharp
var invitation = await _context.Invitations
    .FirstOrDefaultAsync(i =>
        i.Token == request.Token &&
        i.Status == InvitationStatus.Pending &&
        (i.Email == currentUserEmail || i.InvitedUserId == currentUserId), // OR logic var
        cancellationToken);
```

**Açıklama/Not:**
Kod zaten OR logic kullanıyor, çalışıyor olmalı. Test et ve doğrula.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** AcceptInvitation.cs:46-52

---

## 🧪 INTEGRATION TESTS

### IT-001: ThingsBoard Connection Loss - Status Kontrolü

**Test ID:** IT-001
**Öncelik:** MEDIUM
**Kategori:** Integration

**Senaryo:**
ThingsBoard servisi geçici olarak erişilemez.

**Test Adımları:**
1. Device data göndermeye çalışıyor
2. ThingsBoard API timeout/error (503 Service Unavailable)
3. Retry logic var mı?

**Beklenen Sonuç:**
**GÜNCELLENDİ:** Status kontrolümüz var:
- **Seçenek 1:** Status "offline" yazmalı (device status update)
- **Seçenek 2:** Kaldır (zaten status kontrolü var)

Eğer tutulacaksa:
- Retry: 3 deneme, exponential backoff (1s, 2s, 4s)
- Log: "ThingsBoard connection failed, retrying..."
- Fail sonrası: Queue'ye alınabilir (background job - Hangfire)

**Mevcut Durum/Bug Açıklaması:**
ThingsBoardConnectorService'de retry logic var mı?

**Açıklama/Not:**
Status kontrolü zaten varsa bu test gereksiz olabilir. Karar ver: kaldır veya "status offline yazmalı" diye belirtle.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** ThingsBoardConnectorService

---

### IT-002: PostgreSQL Connection Pool Exhausted

**Test ID:** IT-002
**Öncelik:** LOW
**Kategori:** Performance / Integration

**Senaryo:**
Yüksek yük altında connection pool tükendi.

**Test Adımları:**
1. 1000 concurrent request simüle et
2. Connection pool size: 100 (default veya configured)
3. Ne olur?

**Beklenen Sonuç:**
- Request queue'ye alınır (wait)
- Timeout: "Connection pool exhausted" (PostgreSQL error)
- Graceful error handling (500 Internal Server Error yerine 503 Service Unavailable)

**Mevcut Durum/Bug Açıklaması:**
appsettings.json'da connection string pooling configuration:
```json
"ConnectionStrings": {
  "Hems_BackendDb": "Server=localhost;...;Pooling=true;Maximum Pool Size=100"
}
```

**Açıklama/Not:**
Load test - gerçek production senaryosu. JMeter, K6, Artillery ile test edilebilir.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** appsettings.json (Connection string)

---

### IT-003: SMTP Email Send Failure - Retry Logic

**Test ID:** IT-003
**Öncelik:** MEDIUM
**Kategori:** Integration

**Senaryo:**
Email servisi çalışmıyor, davetiye gönderilemedi.

**Test Adımları:**
1. UserA davetiye gönderiyor (CreateInvitation)
2. SMTP timeout/error (örn: Gmail SMTP down)
3. Invitation kaydı oluşturuldu mu? Email gitmedi ama DB'de var mı?

**Beklenen Sonuç:**
- Invitation DB'ye kaydedilir (email gitmese bile)
- Email gönderimi fail olursa:
  - Log: "Email send failed for invitation {Id}"
  - Background job: Retry email send (Hangfire job)
  - User'a uyarı (optional): "Invitation created but email failed, will retry"

**Mevcut Durum/Bug Açıklaması:**
CreateInvitation handler'da email send try-catch ile sarılı mı?
```csharp
try
{
    await _emailService.SendInvitationEmailAsync(...);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send invitation email");
    // Invitation kaydı yine de commit edilmeli
}
```

**Açıklama/Not:**
Email gönderimi asenkron ve retry mekanizmasına sahip olmalı.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** CreateInvitation.cs:104-110, InvitationEmailService

---

## 📝 VALIDATION & BUSINESS RULES

### VR-001: Site Name Duplicate - Aynı User

**Test ID:** VR-001
**Öncelik:** LOW
**Kategori:** Validation

**Senaryo:**
UserA "Ev" isimli iki site oluşturmaya çalışıyor.

**Test Adımları:**
1. UserA "Ev" isimli site oluşturdu
2. UserA tekrar "Ev" isimli site oluşturmaya çalışır
3. İzin verilmeli mi?

**Beklenen Sonuç:**
**Business Decision:**
- **Seçenek 1:** İzin verilebilir (Ev 1, Ev 2 gibi ayırt edilebilir)
- **Seçenek 2:** Unique constraint: "Site name must be unique for this user"

**Önerilen:** Seçenek 1 - İzin ver (kullanıcı "Yazlık Ev", "Şehir Evi" gibi ayırt edebilir)

**Mevcut Durum/Bug Açıklaması:**
CreateSiteCommandValidator'da unique check var mı?

**Açıklama/Not:**
Low priority - kullanıcı deneyimi ile ilgili karar.

**Test Durumu:**

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** CreateSiteCommandValidator

---

### VR-002: Device QRCode vs SerialNumber - Priority

**Test ID:** VR-002
**Öncelik:** LOW
**Kategori:** Validation

**Senaryo:**
Device hem QRCode hem SerialNumber gönderiyor, ikisi farklı device'a işaret ediyor.

**Test Adımları:**
1. CreateSetup: QRCode="ABC", SerialNumber="XYZ"
2. ThingsBoard'da QRCode → DeviceA, SerialNumber → DeviceB (conflict!)
3. Hangisi kullanılır?

**Beklenen Sonuç:**
**GÜNCELLENDİ:** Şu anlık serial number'a bakıyoruz, QR aktif değil. Ama ilerisi için yazalım.

**Business Logic:**
- **Mevcut:** Serial number priority
- **İleride:** QR code eklenir, o zaman priority belirlenmeli
- **Validation:** İkisi de gönderildiyse, ikisi de aynı device'a işaret etmeli (conflict check)

**Mevcut Durum/Bug Açıklaması:**
CreateSetup handler'da:
```csharp
var tbDevice = await _tbConnector.FindDeviceAsync(
    request.DeviceSerialNumber,  // Priority
    request.DeviceQRCode,         // Secondary
    cancellationToken);
```

**Açıklama/Not:**
Gelecek için placeholder test. QR code aktif olunca gözden geçirilmeli.

**Test Durumu:** 📝 Future Feature

**Test Tarihi:**

**Tester:**

**Test Sonucu Detayı:**

**Dosya/Satır:** CreateSetup.cs:68-80, ThingsBoardConnectorService

---

## 📊 Özet ve Öncelikler

| Test ID | Kategori | Öncelik | Durum | Açıklama |
|---------|----------|---------|-------|----------|
| **DL-001** | Device Lifecycle | MEDIUM | 📝 | Device satıldı - sistem sıfırlama |
| **DL-002** | Device Lifecycle | LOW | 📝 | Device arızalandı - offline period |
| **DL-003** | Device Lifecycle | LOW | 📝 | Factory reset - data persistence |
| **DL-004** | Device Lifecycle | LOW | ✅ | Aynı device birden fazla site'de kullanılamaz (test edildi) |
| **ED-001** | Energy Data | HIGH | 📝 | Duplicate energy data - timestamp kontrolü |
| **ED-002** | Energy Data | MEDIUM | 📝 | Negative values - validation listesi |
| **ED-003** | Energy Data | MEDIUM | 📝 | Future timestamp - validation |
| **ED-004** | Energy Data | LOW | ⏳ | DataType case sensitivity (düşük öncelik) |
| **ED-005** | Energy Data | MEDIUM | 📝 | Null fields - frontend handling |
| **DD-001** | Dashboard | MEDIUM | 📝 | Hiç veri yokken - bekleme mesajı |
| **DD-002** | Dashboard | MEDIUM | 📝 | Incomplete data - frontend handling |
| **DD-003** | Dashboard | LOW | 📝 | Date range validation |
| **DD-004** | Dashboard | LOW | 📝 | Query performance - büyük veri seti |
| **DA-002** | Device Auth | LOW | ✅ | Duplicate serial number (test edildi) |
| **DA-003** | Device Auth | MEDIUM | 📝 | Rate limiting - hatalı girişlerde bekleme |
| **SD-001** | Soft Delete | MEDIUM | 📝 | Site cascade delete chain |
| **SD-002** | Soft Delete | MEDIUM | 📝 | Device soft delete - tekrar kullanılabilir mi |
| **BL-001** | Business Logic | ⚠️ | ⚠️ | Role escalation (arayüzde özellik yok - soru) |
| **BL-002** | Business Logic | LOW | 📝 | Owner kendini Guest yapabilir mi (karar bekliyor) |
| **BL-003** | Business Logic | MEDIUM | 📝 | Son owner çıkarsa ne olur |
| **BL-004** | Business Logic | LOW | 📝 | Email değişikliği - invitation geçerliliği |
| **IT-001** | Integration | MEDIUM | 📝 | ThingsBoard connection loss - status kontrolü |
| **IT-002** | Integration | LOW | 📝 | Connection pool exhausted |
| **IT-003** | Integration | MEDIUM | 📝 | Email send failure - retry logic |
| **VR-001** | Validation | LOW | 📝 | Site name duplicate |
| **VR-002** | Validation | LOW | 📝 | QRCode vs SerialNumber priority (future) |

---

**Toplam Test:** 25 (kaldırılanlardan sonra)
**CRITICAL:** 0
**HIGH:** 1 (ED-001)
**MEDIUM:** 10
**LOW:** 12
**⚠️ Soru:** 1 (BL-001)
**✅ Test Edildi:** 2 (DL-004, DA-002)

---

**Durum Açıklamaları:**
- 📝 = Test yapılacak
- ✅ = Test geçti
- ❌ = Test kaldı
- ⏳ = Devam ediyor / Düşük öncelik
- ⚠️ = Açıklama gerekli

---

**Son Güncelleme:** 2026-01-05
**Versiyon:** 1.0 (Refactored)

---

## 🔍 KOD İNCELEME SONUÇLARI

**İnceleme Tarihi:** 2026-01-05

| Test ID | Kategori | Öncelik | Kod İnceleme Sonucu | Test Durumu | Dosya/Satır |
|---------|----------|---------|---------------------|-------------|-------------|
| **ED-001** | Energy Data | HIGH | ✅ Upsert mekanizması çalışıyor | ✅ Test Geçti | CreateDeviceEnergyData.cs:96-117 |
| **ED-002** | Energy Data | MEDIUM | ✅ Validator doğru (GridImport/Export negatif OLMAMALI) | ✅ Test Geçti | CreateDeviceEnergyDataCommandValidator.cs:20-76 |
| **ED-003** | Energy Data | MEDIUM | ❌ Future timestamp validation YOK | ❌ Eksik | CreateDeviceEnergyDataCommandValidator.cs:16-17 |
| **ED-004** | Energy Data | LOW | 📝 İncelenmedi (düşük öncelik) | ⏳ Düşük Öncelik | DeviceEnergyData model |
| **ED-005** | Energy Data | MEDIUM | - | - | - |
| **DD-001** | Dashboard | MEDIUM | ❌ Handler tamamen kapalı (comment'li) | ❌ Çalışmıyor | GetHeroQueryHandler.cs:1-127 |
| **DD-002** | Dashboard | MEDIUM | ❌ Handler tamamen kapalı (comment'li) | ❌ Çalışmıyor | GetEnergyProductionQueryHandler.cs:1-351 |
| **DD-003** | Dashboard | LOW | ✅ Future date validation var (Year, Month, Week) | ✅ Test Geçti | GetPowerAndEnergyMetricsQueryValidator.cs:25-26 |
| **DD-004** | Dashboard | LOW | 📝 İncelenmedi | 📝 Test Yapılacak | GetEnergyProduction handler |
| **DA-002** | Device Auth | LOW | ✅ Duplicate serial number kontrolü var | ✅ Test Edildi | DeviceLogin handler |
| **DA-003** | Device Auth | MEDIUM | ⚠️ Sadece DeviceData için var (login endpoint için yok) | ⚠️ Kısmi | DeviceDataRateLimitMiddleware.cs:22 |
| **SD-001** | Soft Delete | MEDIUM | ❌ Cascade delete YOK (UserSites, Invitations silinmiyor) | ❌ Eksik | DeleteSite.cs:20-45 |
| **SD-002** | Soft Delete | MEDIUM | ❌ Hard delete yapıyor (soft delete olmalı) | ❌ Yanlış İmplementasyon | DeleteDevice.cs:22-36 |
| **BL-001** | Business Logic | ⚠️ SORU | ⛔ Endpoint yok - Test geçersiz | ⛔ N/A | - |
| **BL-002** | Business Logic | LOW | 📝 İncelenmedi | 📝 Business Logic Kararı | - |
| **BL-003** | Business Logic | MEDIUM | ❌ RemoveUserFromSite command'ı YOK | ❌ Komut Yok | - |
| **BL-004** | Business Logic | LOW | ✅ OR logic var (email veya userId ile çalışır) | ✅ Test Geçti | AcceptInvitation.cs:50 |
| **IT-001** | Integration | MEDIUM | 📝 İncelenmedi | 📝 Test Yapılacak | ThingsBoardConnectorService |
| **IT-002** | Integration | LOW | 📝 İncelenmedi | 📝 Test Yapılacak | appsettings.json |
| **IT-003** | Integration | MEDIUM | ❌ Try/catch YOK, retry mekanizması yok | ❌ Eksik | CreateInvitation.cs:103-110 |
| **VR-001** | Validation | LOW | ❌ Unique name kontrolü YOK | ❌ Eksik | CreateSiteCommandValidator.cs:9-13 |
| **VR-002** | Validation | LOW | 📝 İncelenmedi | 📝 Future Feature | CreateSetup.cs:68-80 |
| **DL-001** | Device Lifecycle | MEDIUM | 📝 İncelenmedi | 📝 Test Yapılacak | - |
| **DL-002** | Device Lifecycle | LOW | 📝 İncelenmedi | 📝 Test Yapılacak | - |
| **DL-003** | Device Lifecycle | LOW | 📝 İncelenmedi | 📝 Test Yapılacak | - |
| **DL-004** | Device Lifecycle | LOW | ✅ Device duplicate kontrolü çalışıyor | ✅ Test Geçti | CreateSetup.cs:96-99 |

---

### 📊 Kod İnceleme İstatistikleri

**Toplam İncelenen:** 16 test
**✅ Çalışıyor:** 6 (ED-001, ED-002, DA-002, DL-004, BL-004, DD-003)
**❌ Eksik/Hatalı:** 8 (ED-003, DD-001, DD-002, SD-001, SD-002, BL-003, VR-001, IT-003)
**⚠️ Kısmi:** 1 (DA-003)
**⛔ Test Geçersiz:** 1 (BL-001 - endpoint yok)
**📝 İncelenmedi:** 9 (DL-001, DL-002, DL-003, IT-001, IT-002, ED-004, BL-002, VR-002, DD-004)

---

### 🚨 Kritik Bulgular

1. **Dashboard Çalışmıyor:** GetHero ve GetEnergyProduction handler'ları tamamen kapalı
2. **Future Timestamp:** Validation eksik, device gelecek tarihli veri gönderebiliyor
3. **Cascade Delete:** Site silinince ilişkili UserSites ve Invitations silinmiyor
4. **Hard Delete:** Device soft delete yerine hard delete yapıyor
5. **Missing Command:** RemoveUserFromSite command'ı hiç yok
6. **Email Send Failure:** Try/catch yok, retry mekanizması yok (email fail olursa exception)
7. **Site Name Unique:** Aynı kullanıcı aynı isimle birden fazla site oluşturabilir
8. **Rate Limit Eksik:** Device login endpoint için rate limiting yok (brute force riski)

---

**ED-002 Validation Kuralları (Netleşti - ✅ Validator Doğru):**

❌ **TÜM FIELD'LAR NEGATİF OLAMAZ:**
- **GridImportEnergy:** Şebekeden alım → Her zaman 0 veya pozitif
- **GridExportEnergy:** Şebekeye satış → Her zaman 0 veya pozitif
- BatteryChargeEnergy / BatteryDischargeEnergy → Her zaman 0 veya pozitif (ayrı field'lar, akış yönü field adından belli)
- PvTotalEnergy (PV üretimi her zaman pozitif veya 0)
- BatterySOC (0-100 arası olmalı)
- BatterySOH (0-100 arası olmalı)
- Voltage, Current (fiziksel kısıtlar)

**Karar:** GridImport ve GridExport **iki ayrı field** olduğu için negatif değere gerek yok. Import pozitif = alım, Export pozitif = satış. Tek bir field "GridPower" olsaydı negatif olabilirdi ama bu mimari doğru.

**Mevcut Durum:** ✅ Validator'da tüm field'lar `GreaterThanOrEqualTo(0)` ile kontrol ediliyor. **DOĞRU! Değiştirme.**
