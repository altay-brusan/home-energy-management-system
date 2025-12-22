# Device Settings Karşılaştırma Raporu

Bu rapor, API'den dönen `devicesettings-tree.json` ayarları ile uygulamadaki `CustomModeScreen` ve `inverterModes.types.ts` ayarlarını karşılaştırmaktadır.

---

## Özet

| Kategori | API'de Mevcut | Uygulamada Mevcut | Eksik/Fazla |
|----------|---------------|-------------------|-------------|
| Time of Use | 49 parametre | 42 parametre | 7 eksik |
| Battery Settings | 8 parametre | 11 parametre | 3 fazla (uygulama) |
| System Work Mode | 7 parametre | 7 parametre | Eşleşme farklı |
| Generator & Smart Load | 14 parametre | 8 parametre | Smart Load yok |
| Maintenance (Admin) | 26+ parametre | 0 parametre | Admin ayarları yok |

---

## 1. TIME OF USE Karşılaştırması

### API'deki Yapı (devicesettings-tree.json)
```
TimeOfUse
├── ActiveDays (7 gün - Boolean)
├── TimeOfUseEnabled (Boolean)
└── TimeSlot1-6 (her biri için):
    ├── Start (Text - HH:MM)
    ├── End (Text - HH:MM)
    ├── GridCharge (Boolean)
    ├── GenCharge (Boolean)
    ├── MaxDischarge (Decimal - W)
    ├── MinBatterySOC (Decimal - %)
    └── MinBatteryVoltage (Decimal - V)
```

### Uygulamadaki Yapı (TimeOfUsePeriod)
```typescript
interface TimeOfUsePeriod {
  id: TimeOfUsePeriodId;
  startTime: string;      // ✅ Eşleşiyor
  endTime: string;        // ✅ Eşleşiyor
  gridCharge: OnOffStatus; // ✅ Eşleşiyor
  genCharge: OnOffStatus;  // ✅ Eşleşiyor
  targetSOC: number;       // ⚠️ Farklı isim (API: MinBatterySOC)
  maxDischargePower: number; // ✅ Eşleşiyor
}
```

### Karşılaştırma Tablosu - Time of Use

| API Parametre Adı | Uygulama Karşılığı | Durum | Açıklama |
|-------------------|-------------------|-------|----------|
| `TimeOfUseEnabled` | `timeOfUse.enabled` | ✅ | Eşleşiyor |
| `Monday-Sunday` | `activeDays.monday-sunday` | ✅ | Eşleşiyor |
| `TimeSlotXStart` | `periods[x].startTime` | ✅ | Eşleşiyor |
| `TimeSlotXEnd` | `periods[x].endTime` | ✅ | Eşleşiyor |
| `TimeSlotXGridCharge` | `periods[x].gridCharge` | ✅ | Eşleşiyor |
| `TimeSlotXGenCharge` | `periods[x].genCharge` | ✅ | Eşleşiyor |
| `TimeSlotXMaxDischarge` | `periods[x].maxDischargePower` | ✅ | Eşleşiyor |
| `TimeSlotXMinBatterySOC` | `periods[x].targetSOC` | ⚠️ | İsim farklı ama mantıksal eşleşme |
| `TimeSlotXMinBatteryVoltage` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |

### Eksik Parametre Detayı
- **MinBatteryVoltage**: Her zaman dilimi için minimum batarya voltajı (V cinsinden). Uygulamada bu ayar bulunmuyor.

---

## 2. BATTERY SETTINGS Karşılaştırması

### API'deki Yapı
```
BatterySettings
├── ChargeSources
│   ├── GridCharging
│   │   ├── GridChargingEnabled
│   │   ├── GridChargingStartSOC
│   │   └── GridChargingCurrent
│   └── GeneratorCharging
│       ├── GeneratorChargingEnabled
│       ├── GeneratorChargingStartSOC
│       ├── GeneratorChargingCurrent
│       ├── GeneratorMaxRunTime
│       └── GeneratorCoolingTime
└── ProtectionThresholds
    ├── ShutdownSOC
    ├── LowBatteryWarning
    └── RestartSOC
```

### Karşılaştırma Tablosu - Battery

| API Parametre Adı | Uygulama Karşılığı | Durum | Açıklama |
|-------------------|-------------------|-------|----------|
| `ShutdownSOC` | `battery.shutdownSOC` | ✅ | Eşleşiyor |
| `LowBatteryWarning` | `battery.lowBattSOC` | ✅ | Eşleşiyor |
| `RestartSOC` | `battery.restartSOC` | ✅ | Eşleşiyor |
| `GridChargingEnabled` | `grid.gridCharge` | ✅ | Farklı kategoride ama eşleşiyor |
| `GridChargingStartSOC` | `grid.gridChargeSOC` | ✅ | Farklı kategoride ama eşleşiyor |
| `GridChargingCurrent` | `grid.gridChargeCurrent` | ✅ | Farklı kategoride ama eşleşiyor |
| `GeneratorChargingEnabled` | `generator.genCharge` | ✅ | Farklı kategoride ama eşleşiyor |
| `GeneratorChargingStartSOC` | `generator.genStartSOC` | ✅ | Farklı kategoride ama eşleşiyor |
| `GeneratorChargingCurrent` | `generator.genChargeCurrent` | ✅ | Farklı kategoride ama eşleşiyor |
| `GeneratorMaxRunTime` | `generator.genMaxRunTime` | ✅ | Farklı kategoride ama eşleşiyor |
| `GeneratorCoolingTime` | `generator.genDownTime` | ⚠️ | İsim farklı (Cooling vs Down) |
| ❌ API'DE YOK | `battery.batteryType` | 🟡 | **API'DE EKSİK** |
| ❌ API'DE YOK | `battery.batteryCapacity` | 🟡 | **API'DE EKSİK** |
| ❌ API'DE YOK | `battery.maxACharge` | 🟡 | **API'DE EKSİK** |
| ❌ API'DE YOK | `battery.maxADischarge` | 🟡 | **API'DE EKSİK** |
| ❌ API'DE YOK | `battery.chargingSOC` | 🟡 | **API'DE EKSİK** |
| ❌ API'DE YOK | `battery.floatVoltage` | 🟡 | **API'DE EKSİK** |
| ❌ API'DE YOK | `battery.absorptionVoltage` | 🟡 | **API'DE EKSİK** |
| ❌ API'DE YOK | `battery.equalizationVoltage` | 🟡 | **API'DE EKSİK** |

### Kritik Notlar
1. **batteryType, batteryCapacity, maxACharge, maxADischarge**: Bu değerler muhtemelen Admin seviyesinde (`MaintenanceSettings`) veya cihazdan otomatik okunan değerler olabilir.
2. **Voltaj ayarları** (float, absorption, equalization): Bunlar Admin seviyesinde veya cihaz tarafından otomatik ayarlanan değerler olabilir.

---

## 3. SYSTEM WORK MODE Karşılaştırması

### API'deki Yapı
```
SystemWorkMode
├── ExportStrategy
│   └── ExportStrategySelection (SellingFirst/ZeroExportToLoad/ZeroExportToCT)
├── EnergyPriority
│   └── EnergyPrioritySelection (BatteryFirst/LoadFirst)
├── InverterMode
│   ├── SolarSell
│   ├── MaxSellPower
│   ├── ZeroExportLimit
│   └── ForceOffGrid
└── GridPeakShaving
    ├── GridPeakShavingEnabled
    └── GridPeakShavingLimitPower
```

### Karşılaştırma Tablosu - Work Mode

| API Parametre Adı | Uygulama Karşılığı | Durum | Açıklama |
|-------------------|-------------------|-------|----------|
| `ExportStrategySelection` | `workMode.workMode` | ⚠️ | Değer formatı farklı |
| `EnergyPrioritySelection` | `workMode.energyPattern` | ⚠️ | Değer formatı farklı |
| `SolarSell` | `workMode.solarSell` | ✅ | Eşleşiyor |
| `MaxSellPower` | `workMode.maxSellPower` | ✅ | Eşleşiyor |
| `ZeroExportLimit` | `workMode.zeroExportPower` | ⚠️ | İsim farklı |
| `ForceOffGrid` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |
| `GridPeakShavingEnabled` | `workMode.gridPeakShaving` | ✅ | Eşleşiyor |
| `GridPeakShavingLimitPower` | `workMode.gridPeakShavingPower` | ✅ | Eşleşiyor |

### Değer Format Farkları

| API Değeri | Uygulama Değeri |
|------------|-----------------|
| `SellingFirst` | `selling_first` |
| `ZeroExportToLoad` | `zero_export_to_load` |
| `ZeroExportToCT` | `zero_export_to_ct` |
| `BatteryFirst` | `batt_first` |
| `LoadFirst` | `load_first` |

### Eksik Özellik
- **ForceOffGrid**: Zorla off-grid moduna geçiş. Uygulamada bu ayar bulunmuyor.

---

## 4. GENERATOR & SMART LOAD Karşılaştırması

### API'deki Yapı
```
GeneratorAndSmartLoad
├── GeneratorSettings
│   ├── GeneratorCharge
│   ├── GeneratorGridCharge
│   ├── GeneratorStartSOC
│   └── GeneratorChargeCurrent
├── GeneratorPeakShaving
│   ├── GeneratorPeakShavingEnabled
│   └── GeneratorPeakShavingLimitPower
└── SmartLoad
    ├── SmartLoadEnabled
    ├── SmartLoadOffSOC
    ├── SmartLoadOnSOC
    ├── SmartLoadPVPowerThreshold
    ├── SmartLoadOpenDelay
    └── SmartLoadOnGridAlwaysOn
```

### Karşılaştırma Tablosu - Generator

| API Parametre Adı | Uygulama Karşılığı | Durum | Açıklama |
|-------------------|-------------------|-------|----------|
| `GeneratorCharge` | `generator.genCharge` | ✅ | Eşleşiyor |
| `GeneratorGridCharge` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |
| `GeneratorStartSOC` | `generator.genStartSOC` | ✅ | Eşleşiyor |
| `GeneratorChargeCurrent` | `generator.genChargeCurrent` | ✅ | Eşleşiyor |
| `GeneratorPeakShavingEnabled` | `generator.genPeakShaving` | ✅ | Eşleşiyor |
| `GeneratorPeakShavingLimitPower` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |
| ❌ API'DE YOK | `generator.genSignal` | 🟡 | **API'DE EKSİK** |
| ❌ API'DE YOK | `generator.genRatedPower` | 🟡 | **API'DE EKSİK** |

### Karşılaştırma Tablosu - Smart Load (TAMAMEN EKSİK)

| API Parametre Adı | Uygulama Karşılığı | Durum | Açıklama |
|-------------------|-------------------|-------|----------|
| `SmartLoadEnabled` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |
| `SmartLoadOffSOC` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |
| `SmartLoadOnSOC` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |
| `SmartLoadPVPowerThreshold` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |
| `SmartLoadOpenDelay` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |
| `SmartLoadOnGridAlwaysOn` | ❌ YOK | 🔴 | **UYGULAMADA EKSİK** |

---

## 5. ADVANCED SETTINGS Karşılaştırması

### Uygulama Yapısı
```typescript
interface AdvancedSettings {
  signalIslandMode: OnOffStatus;
  bmsErrStop: OnOffStatus;
  backupDelay: number;
  ctRatio: string;
  asymmetricPhaseFeeding: OnOffStatus;
}
```

### Karşılaştırma Tablosu

| Uygulama Parametresi | API Karşılığı | Durum | Açıklama |
|---------------------|---------------|-------|----------|
| `signalIslandMode` | ❌ YOK (User level) | 🟡 | Admin seviyesinde olabilir |
| `bmsErrStop` | ❌ YOK (User level) | 🟡 | Admin seviyesinde olabilir |
| `backupDelay` | ❌ YOK | 🔴 | **API'DE EKSİK** |
| `ctRatio` | `ExternalCTRatio` (Admin) | ⚠️ | Admin seviyesinde mevcut |
| `asymmetricPhaseFeeding` | ❌ YOK | 🔴 | **API'DE EKSİK** |

---

## 6. MAINTENANCE SETTINGS (Admin Seviyesi)

API'de `roleType: "Admin"` olan tüm ayarlar. **Bunlar uygulamada hiç gösterilmiyor.**

| Parametre | Açıklama | Uygulamada |
|-----------|----------|------------|
| `BatteryControlMode` | Voltage/Capacity/No Battery | ❌ YOK |
| `LithiumBatteryWakeUpEnabled` | Li batarya uyandırma | ❌ YOK |
| `BatteryResistance` | İç direnç (mΩ) | ❌ YOK |
| `ChargeEfficiency` | Şarj verimliliği (%) | ❌ YOK |
| `GridStandard` | Şebeke standardı | ❌ YOK |
| `GridFrequency` | Şebeke frekansı (Hz) | ❌ YOK |
| `OutputVoltageLevel` | Çıkış voltajı | ❌ YOK |
| `GridVoltageHigh/Low` | Voltaj koruma | ❌ YOK |
| `GridFrequencyHigh/Low` | Frekans koruma | ❌ YOK |
| `ReconnectionTime` | Yeniden bağlanma süresi | ❌ YOK |
| `PowerFactorTarget` | Güç faktörü | ❌ YOK |
| `ArcFaultDetectionEnabled` | Ark hatası algılama | ❌ YOK |
| `IslandProtectionEnabled` | Ada koruma | ❌ YOK |
| `ParallelEnabled/Role/Phase` | Paralel ayarları | ❌ YOK |
| `ModbusAddress` | Modbus adresi | ❌ YOK |
| `ExternalCTRatio` | Harici CT oranı | ❌ YOK |
| `LithiumBatteryLossFaultEnabled` | Li batarya kayıp hatası | ❌ YOK |
| `ExternalRelayFeatureEnabled` | Harici röle | ❌ YOK |
| `ForceSmartLoadOn` | Zorla Smart Load açık | ❌ YOK |

---

## Sonuç ve Öneriler

### 🔴 Kritik Eksiklikler (Uygulamada Yok)

1. **Smart Load Ayarları**: Tüm Smart Load parametreleri eksik
   - SmartLoadEnabled, SmartLoadOffSOC, SmartLoadOnSOC
   - SmartLoadPVPowerThreshold, SmartLoadOpenDelay
   - SmartLoadOnGridAlwaysOn

2. **MinBatteryVoltage**: Time of Use zaman dilimlerinde minimum voltaj ayarı yok

3. **ForceOffGrid**: Zorla off-grid modu ayarı yok

4. **GeneratorGridCharge**: Jeneratörden şebekeye şarj özelliği yok

5. **GeneratorPeakShavingLimitPower**: Jeneratör peak shaving güç limiti yok

### 🟡 Dikkat Edilmesi Gerekenler

1. **Değer Format Farkları**: API camelCase, uygulama snake_case kullanıyor
   - Dönüşüm gerekli: `ZeroExportToLoad` → `zero_export_to_load`

2. **Admin Ayarları**: 26+ admin seviyesi parametre uygulamada gösterilmiyor
   - Bu kasıtlı olabilir (sadece teknik personel için)

3. **Kategori Farklılıkları**:
   - API'de Grid Charging → BatterySettings altında
   - Uygulamada → GridSettings altında

### 🟢 Doğru Eşleşenler

- Time of Use temel yapısı (6 zaman dilimi)
- Active Days (haftanın günleri)
- Temel SOC ayarları (shutdown, lowBatt, restart)
- Grid/Generator charging temel parametreleri
- Peak shaving enable/disable

---

## Önerilen Aksiyon Planı

1. **Öncelik 1 - Smart Load**: SmartLoad ayarlarını uygulamaya ekle
2. **Öncelik 2 - MinBatteryVoltage**: TimeOfUse period'larına voltage ekle
3. **Öncelik 3 - ForceOffGrid**: Work mode ayarlarına ekle
4. **Öncelik 4 - Değer Dönüşümü**: API ↔ Uygulama arası mapper fonksiyonları
5. **Öncelik 5 - Admin Panel**: İsteğe bağlı, admin ayarları için ayrı ekran

---

*Rapor Tarihi: 2025-12-22*
*Karşılaştırılan Dosyalar:*
- `devicesettings-tree.json` (API Response)
- `src/api/types/inverterModes.types.ts`
- `src/app/screens/inverterModes/CustomModeScreen.tsx`
- `src/store/inverterModeStore.ts`
