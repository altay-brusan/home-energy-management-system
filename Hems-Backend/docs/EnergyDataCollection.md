# Energy Data Collection Service

Bu dokümantasyon, HEMS Backend sisteminde enerji verisi toplama servisinin (Energy Data Collection Service) çalışma mantığını, mimarisini ve implementasyon detaylarını açıklar.

## Problem

Cihazlar günlük enerji verilerini 23:59 yerel saatte backend'e gönderir. Ancak cihaz çevrimdışı olduğunda (WiFi kaybı, elektrik kesintisi vb.) tüm günün verisi kaybolur.

## Çözüm

Backend, cihazlardan enerji verilerini proaktif olarak ThingsBoard RPC üzerinden 23:45 yerel saatte çeker. RPC başarısız olursa gece yarısına kadar yeniden deneme yapar ve son çare olarak güç verilerinden tahmin hesaplar.

---

## Mimari

### Bileşenler

```
┌─────────────────────────────────────────────────────────────────┐
│                     Energy Data Collection                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────┐      ┌──────────────────────────┐     │
│  │   Hangfire Job       │      │  EnergyDataCollection    │     │
│  │   (Every 15 min)     │──────│  Service                 │     │
│  └──────────────────────┘      └─────────────┬────────────┘     │
│                                              │                   │
│                                              │                   │
│  ┌───────────────────────────────────────────┴───────────────┐  │
│  │                                                             │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌────────────────┐  │  │
│  │  │ ThingsBoard  │  │ Power Data   │  │ Database       │  │  │
│  │  │ RPC Call     │  │ Estimation   │  │ Persistence    │  │  │
│  │  └──────────────┘  └──────────────┘  └────────────────┘  │  │
│  │                                                             │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Katman Yapısı

| Katman | Dosya | Açıklama |
|--------|-------|----------|
| Application | `IEnergyDataCollectionService.cs` | Servis arayüzü |
| Application | `EnergyDataCollectionModels.cs` | DTO'lar ve modeller |
| Infrastructure | `EnergyDataCollectionService.cs` | Ana servis implementasyonu |
| Infrastructure | `EnergyDataCollectionBackgroundService.cs` | Hangfire job kaydı |

---

## Job Akışı

### Genel Akış

```
Her 15 dakikada bir (Hangfire):
  ├─► Tüm aktif cihazları getir
  │
  └─► Her cihaz için:
        │
        ├─► 1. Cihaz timezone'unu al (Device → SetupDevice → Setup → Site)
        │
        ├─► 2. Cihaz yerel saatini hesapla
        │
        ├─► 3. Yerel saat [23:45, 00:15] aralığında mı?
        │     │
        │     └─► Hayır → Atla
        │
        ├─► 4. Bugün için zaten işlendi mi? (Cache kontrolü)
        │     │
        │     └─► Evet → Atla
        │
        ├─► 5. getDailyEnergyData RPC çağrısı yap
        │     │
        │     ├─► Başarılı → Kaydet (CreatedBy = "Device")
        │     │
        │     └─► Başarısız:
        │           │
        │           ├─► Yerel saat < 00:00 → Sonraki iterasyonda tekrar dene
        │           │
        │           └─► Yerel saat >= 00:00 → Güç verisinden tahmin et
        │                 │
        │                 └─► Kaydet (CreatedBy = "SystemEstimate")
        │
        ├─► 6. getLifetimeEnergyData RPC çağrısı yap
        │     │
        │     ├─► Başarılı → Kaydet (CreatedBy = "Device")
        │     │
        │     └─► Başarısız → Önceki lifetime + günlük tahmin hesapla
        │                      │
        │                      └─► Kaydet (CreatedBy = "SystemEstimate")
        │
        └─► 7. Cache'i güncelle
```

### Toplama Penceresi

| Durum | Aksiyon |
|-------|---------|
| Yerel saat < 23:45 | İşlem yapma, bekle |
| 23:45 ≤ Yerel saat < 00:00 | RPC dene, başarısız olursa bir sonraki iterasyonda tekrar dene |
| 00:00 ≤ Yerel saat < 00:15 | RPC dene, başarısız olursa güç verisinden tahmin et |
| Yerel saat ≥ 00:15 | O gün için pencere kapandı |

---

## RPC Metodları

### getDailyEnergyData

Cihazdan günlük kümülatif enerji verilerini çeker.

**İstek:**
```json
{
  "method": "getDailyEnergyData",
  "params": {},
  "timeout": 10000
}
```

**Yanıt:**
```json
{
  "pvTotalEnergyKWh": 45.5,
  "gridImportEnergyKWh": 12.3,
  "gridExportEnergyKWh": 8.7,
  "batteryChargeEnergyKWh": 15.2,
  "batteryDischargeEnergyKWh": 18.1,
  "loadEnergyKWh": 35.6,
  "generatorEnergyKWh": 0
}
```

### getLifetimeEnergyData

Cihazdan toplam (lifetime) kümülatif enerji verilerini çeker.

**İstek:**
```json
{
  "method": "getLifetimeEnergyData",
  "params": {},
  "timeout": 10000
}
```

**Yanıt:** (getDailyEnergyData ile aynı format)

---

## Enerji Tahmin Algoritması

RPC başarısız olduğunda, günlük enerji `DevicePowerData` tablosundan (5 dakikalık güç okumaları) tahmin edilir.

### Güçten Enerjiye Dönüşüm

Trapezoidal kural kullanılarak:

```
Energy(kWh) = SUM( OrtalamaGüç(W) * AralıkSaat ) / 1000
OrtalamaGüç = (Güç[i-1] + Güç[i]) / 2
```

### İşaret Kuralları

| Alan | Pozitif | Negatif |
|------|---------|---------|
| GridTotalPowerW | İthalat (şebekeden çekme) | İhracat (şebekeye verme) |
| BatteryPowerW | Deşarj | Şarj |

### Algoritma Adımları

1. Hedef tarih için `DevicePowerData` sorgula (00:00-23:59:59 yerel)
2. `DeviceLocalTime`'a göre sırala
3. Her ardışık çift için aralık enerjisini hesapla
4. Grid'i ithalat/ihracat ve bataryayı şarj/deşarj olarak ayır
5. Toplamları 2 ondalık basamağa yuvarla

### Örnek Hesaplama

```
Güç okumaları: [1000W @ 10:00, 1200W @ 10:05, 800W @ 10:10]

Aralık 1 (10:00-10:05):
  OrtGüç = (1000 + 1200) / 2 = 1100W
  Süre = 5 dakika = 0.0833 saat
  Enerji = 1100 * 0.0833 / 1000 = 0.0917 kWh

Aralık 2 (10:05-10:10):
  OrtGüç = (1200 + 800) / 2 = 1000W
  Süre = 5 dakika = 0.0833 saat
  Enerji = 1000 * 0.0833 / 1000 = 0.0833 kWh

Toplam = 0.0917 + 0.0833 = 0.175 kWh
```

---

## Lifetime Enerji Hesaplaması

RPC başarısız olduğunda:

1. Cihaz için en son `LifetimeEnergyData` kaydını sorgula
2. Günlük tahmin değerlerini ekle: `YeniLifetime = ÖncekiLifetime + GünlükEnerji`
3. `CreatedBy = "SystemEstimate"` ile kaydet

---

## Veri Kaynağı İzleme (CreatedBy)

| Değer | Açıklama |
|-------|----------|
| `Device` | Cihazdan RPC ile doğrudan alındı |
| `SystemEstimate` | Güç verisinden backend tarafından hesaplandı |
| `Simulator` | Device Simulator tarafından oluşturuldu |
| `System` | Genel sistem kaydı |

---

## Servis Arayüzü

```csharp
public interface IEnergyDataCollectionService
{
    /// <summary>
    /// Tüm aktif cihazlar için enerji verisi toplama işlemini başlatır.
    /// </summary>
    Task ProcessEnergyDataCollectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Belirli bir cihaz için enerji verisi toplama işlemini başlatır.
    /// </summary>
    Task ProcessDeviceEnergyDataAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// RPC üzerinden günlük enerji verisi çeker.
    /// </summary>
    Task<DailyEnergyRpcResult?> FetchDailyEnergyViaRpcAsync(
        Guid thingsBoardDeviceId,
        CancellationToken ct = default);

    /// <summary>
    /// RPC üzerinden lifetime enerji verisi çeker.
    /// </summary>
    Task<LifetimeEnergyRpcResult?> FetchLifetimeEnergyViaRpcAsync(
        Guid thingsBoardDeviceId,
        CancellationToken ct = default);

    /// <summary>
    /// Güç verisinden günlük enerji tahmini hesaplar.
    /// </summary>
    Task<EstimatedDailyEnergy> EstimateDailyEnergyFromPowerDataAsync(
        Guid thingsBoardDeviceId,
        DateTime date,
        CancellationToken ct = default);

    /// <summary>
    /// Önceki lifetime + günlük tahmin ile lifetime enerji hesaplar.
    /// </summary>
    Task<CalculatedLifetimeEnergy> CalculateLifetimeEnergyAsync(
        Guid thingsBoardDeviceId,
        EstimatedDailyEnergy dailyEnergy,
        CancellationToken ct = default);
}
```

---

## DTO'lar

### DailyEnergyRpcResult

```csharp
public record DailyEnergyRpcResult
{
    public decimal PvTotalEnergyKWh { get; init; }
    public decimal GridImportEnergyKWh { get; init; }
    public decimal GridExportEnergyKWh { get; init; }
    public decimal BatteryChargeEnergyKWh { get; init; }
    public decimal BatteryDischargeEnergyKWh { get; init; }
    public decimal LoadEnergyKWh { get; init; }
    public decimal GeneratorEnergyKWh { get; init; }
}
```

### EstimatedDailyEnergy

```csharp
public record EstimatedDailyEnergy
{
    public decimal PvTotalEnergyKWh { get; init; }
    public decimal GridImportEnergyKWh { get; init; }
    public decimal GridExportEnergyKWh { get; init; }
    public decimal BatteryChargeEnergyKWh { get; init; }
    public decimal BatteryDischargeEnergyKWh { get; init; }
    public decimal LoadEnergyKWh { get; init; }
    public decimal GeneratorEnergyKWh { get; init; }
    public int PowerDataPointsUsed { get; init; }
    public bool IsEstimated { get; init; } = true;
}
```

---

## Hangfire Konfigürasyonu

### Job Kaydı

```csharp
_recurringJobManager.AddOrUpdate(
    "energy-data-collection",
    () => ExecuteCollectionJob(),
    "*/15 * * * *",  // Her 15 dakikada bir
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

### Job Özellikleri

```csharp
[AutomaticRetry(Attempts = 0)]  // Otomatik yeniden deneme yok (kendi retry mantığımız var)
[DisableConcurrentExecution(timeoutInSeconds: 300)]  // Eşzamanlı çalışma engellendi
public async Task ExecuteCollectionJob()
{
    // ...
}
```

### Dashboard

Hangfire Dashboard `/hangfire` endpoint'inde erişilebilir.

- Development: Tüm kullanıcılar erişebilir
- Production: Sadece authenticated kullanıcılar (`HangfireAuthorizationFilter`)

---

## Dosya Konumları

### Yeni Dosyalar

| Dosya | Konum |
|-------|-------|
| IEnergyDataCollectionService.cs | `src/Application/Common/Interfaces/` |
| EnergyDataCollectionModels.cs | `src/Application/Common/Models/` |
| EnergyDataCollectionService.cs | `src/Infrastructure/Services/` |
| EnergyDataCollectionBackgroundService.cs | `src/Infrastructure/Services/` |

### Değiştirilen Dosyalar

| Dosya | Değişiklik |
|-------|------------|
| Web.csproj | Hangfire NuGet paketleri eklendi |
| Infrastructure.csproj | Hangfire.Core paketi eklendi |
| Web/DependencyInjection.cs | Hangfire konfigürasyonu eklendi |
| Web/Program.cs | Hangfire Dashboard ve authorization filter eklendi |
| Infrastructure/DependencyInjection.cs | Servis kayıtları eklendi |
| Domain/Constants/DataTypes.cs | EnergyDataSources sabitleri eklendi |
| CreateDeviceEnergyData.cs | CreatedBy parametresi eklendi |
| EnergyDataJobService.cs (Simulator) | CreatedBy = "Simulator" eklendi |

---

## Test Senaryoları

### Unit Test Senaryoları

1. **Enerji Tahmin Hesaplaması**
   - Boş güç verisi → Sıfır değerler döner
   - Tek veri noktası → Sıfır değerler döner (aralık yok)
   - Normal veri seti → Doğru trapezoidal hesaplama

2. **Lifetime Hesaplaması**
   - Önceki lifetime yok → Sadece günlük değerler
   - Önceki lifetime var → Doğru toplama

3. **Toplama Penceresi Kontrolü**
   - Yerel saat 23:44 → İşlem yapılmaz
   - Yerel saat 23:45 → İşlem yapılır
   - Yerel saat 00:15 → İşlem yapılmaz

### Manuel Test

1. Web projesini Hangfire ile başlat
2. `/hangfire` dashboard'ını kontrol et
3. Job'un her 15 dakikada bir çalıştığını doğrula
4. `DeviceEnergyData` tablosunda doğru `CreatedBy` değerlerini kontrol et

---

## Hata Yönetimi

### RPC Timeout

- Varsayılan timeout: 10 saniye
- Timeout durumunda RPC `null` döner ve tahmin mekanizması devreye girer

### Veritabanı Hataları

- Kayıt sırasında hata → Log kaydı yapılır, işlem bir sonraki iterasyonda tekrar denenir

### Eksik Timezone

- Cihaz timezone'u bulunamazsa UTC varsayılır

---

## Performans Notları

- Her iterasyonda sadece toplama penceresindeki cihazlar işlenir
- Eşzamanlı işlem engeli ile race condition önlenir
- Başarılı işlemler cache'lenir, aynı gün için tekrar işlenmez
- Worker sayısı: `Environment.ProcessorCount * 2`

---

*Bu servis, cihaz çevrimdışı olduğunda bile enerji verilerinin kaybolmamasını sağlar.*
