# Required i18n Translations for Device Detail Redesign

## English (en)

```json
{
  "deviceDetail": {
    "inverter": {
      "title": "Inverter",
      "subtitle": "Power conversion system"
    },
    "battery": {
      "title": "Battery",
      "packCount": "{{count}} battery pack included",
      "packCount_plural": "{{count}} battery packs included",
      "noBatteries": "No batteries available"
    },
    "connectedDevices": {
      "title": "Connected Devices"
    },
    "pvPanel": {
      "description": "Solar panel power generation"
    },
    "gateway": {
      "description": "Communication gateway"
    },
    "smartMeter": {
      "title": "Smart Meter",
      "description": "Energy consumption monitoring"
    }
  },
  "batteryList": {
    "title": "Battery Packs",
    "serialNumber": "Serial Number",
    "empty": "No battery packs found"
  },
  "batteryDetail": {
    "title": "Battery Details",
    "stateOfCharge": "State of Charge",
    "stats": "Battery Statistics",
    "soh": "State of Health (SOH)",
    "voltage": "Voltage",
    "current": "Current",
    "cellVoltage": "Cell Voltage",
    "maxCellVoltage": "Max Cell Voltage",
    "minCellVoltage": "Min Cell Voltage",
    "avgCellVoltage": "Average Cell Voltage",
    "temperature": "Temperature",
    "maxCellTemp": "Max Cell Temperature",
    "minCellTemp": "Min Cell Temperature",
    "avgTemp": "Average Temperature",
    "deviceInfo": "Device Information",
    "model": "Model",
    "serialNumber": "Serial Number",
    "firmwareVersion": "Firmware Version",
    "lastUpdate": "Last Update"
  },
  "inverterDetail": {
    "title": "Inverter Details",
    "activePower": "Active Power",
    "powerMetrics": "Power Metrics",
    "reactivePower": "Reactive Power",
    "powerFactor": "Power Factor",
    "insulationResistance": "Insulation Resistance",
    "phaseData": "Phase Data",
    "phaseLabel": "Phase {{id}}",
    "voltage": "Voltage",
    "current": "Current",
    "power": "Power",
    "deviceInfo": "Device Information",
    "serialNumber": "Serial Number",
    "firmwareVersion": "Firmware Version",
    "startupTime": "Last Startup Time",
    "shutdownTime": "Last Shutdown Time"
  },
  "smartMeterDetail": {
    "title": "Smart Meter",
    "comingSoon": "Coming Soon",
    "description": "Smart meter integration and energy monitoring features will be available soon.",
    "features": {
      "title": "Upcoming Features",
      "realTimeMonitoring": "Real-time energy monitoring",
      "energyConsumption": "Detailed consumption analysis",
      "costAnalysis": "Cost tracking and optimization",
      "historicalData": "Historical data and trends"
    }
  }
}
```

## Turkish (tr)

```json
{
  "deviceDetail": {
    "inverter": {
      "title": "İnvertör",
      "subtitle": "Güç dönüştürme sistemi"
    },
    "battery": {
      "title": "Batarya",
      "packCount": "{{count}} batarya paketi dahil",
      "packCount_plural": "{{count}} batarya paketi dahil",
      "noBatteries": "Batarya bulunamadı"
    },
    "connectedDevices": {
      "title": "Bağlı Cihazlar"
    },
    "pvPanel": {
      "description": "Solar panel güç üretimi"
    },
    "gateway": {
      "description": "İletişim ağ geçidi"
    },
    "smartMeter": {
      "title": "Akıllı Sayaç",
      "description": "Enerji tüketimi izleme"
    }
  },
  "batteryList": {
    "title": "Batarya Paketleri",
    "serialNumber": "Seri Numarası",
    "empty": "Batarya paketi bulunamadı"
  },
  "batteryDetail": {
    "title": "Batarya Detayları",
    "stateOfCharge": "Şarj Durumu",
    "stats": "Batarya İstatistikleri",
    "soh": "Sağlık Durumu (SOH)",
    "voltage": "Voltaj",
    "current": "Akım",
    "cellVoltage": "Hücre Voltajı",
    "maxCellVoltage": "Maksimum Hücre Voltajı",
    "minCellVoltage": "Minimum Hücre Voltajı",
    "avgCellVoltage": "Ortalama Hücre Voltajı",
    "temperature": "Sıcaklık",
    "maxCellTemp": "Maksimum Hücre Sıcaklığı",
    "minCellTemp": "Minimum Hücre Sıcaklığı",
    "avgTemp": "Ortalama Sıcaklık",
    "deviceInfo": "Cihaz Bilgileri",
    "model": "Model",
    "serialNumber": "Seri Numarası",
    "firmwareVersion": "Firmware Sürümü",
    "lastUpdate": "Son Güncelleme"
  },
  "inverterDetail": {
    "title": "İnvertör Detayları",
    "activePower": "Aktif Güç",
    "powerMetrics": "Güç Metrikleri",
    "reactivePower": "Reaktif Güç",
    "powerFactor": "Güç Faktörü",
    "insulationResistance": "İzolasyon Direnci",
    "phaseData": "Faz Verileri",
    "phaseLabel": "Faz {{id}}",
    "voltage": "Voltaj",
    "current": "Akım",
    "power": "Güç",
    "deviceInfo": "Cihaz Bilgileri",
    "serialNumber": "Seri Numarası",
    "firmwareVersion": "Firmware Sürümü",
    "startupTime": "Son Açılış Zamanı",
    "shutdownTime": "Son Kapanış Zamanı"
  },
  "smartMeterDetail": {
    "title": "Akıllı Sayaç",
    "comingSoon": "Yakında",
    "description": "Akıllı sayaç entegrasyonu ve enerji izleme özellikleri yakında kullanıma sunulacak.",
    "features": {
      "title": "Gelecek Özellikler",
      "realTimeMonitoring": "Gerçek zamanlı enerji izleme",
      "energyConsumption": "Detaylı tüketim analizi",
      "costAnalysis": "Maliyet takibi ve optimizasyonu",
      "historicalData": "Geçmiş veri ve trendler"
    }
  }
}
```

## Instructions

Please add these translations to your i18n translation files:
- For English: `src/lib/i18n/locales/en/translation.json` (or similar path)
- For Turkish: `src/lib/i18n/locales/tr/translation.json` (or similar path)

Merge these keys with your existing translations. Make sure to maintain the proper JSON structure and nesting.
