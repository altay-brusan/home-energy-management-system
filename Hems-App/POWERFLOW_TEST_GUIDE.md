# PowerFlow Streaming Test Guide

## 🧪 Test Yöntemleri

### Yöntem 1: Debug Component (Görsel Test) ⭐ ÖNERİLEN

Debug component'i Admin sayfasına ekledik.

**Adımlar:**
1. Uygulamayı başlat: `npm start` + `npm run android/ios`
2. Admin sayfasına git (MainTabs → Admin)
3. Sayfayı en alta scroll et
4. **"🔬 PowerFlow Stream Test"** panelini gör
5. Device ID gir (GUID formatında)
6. **"▶️ Start Stream"** butonuna bas
7. Canlı veri gelmeye başlayacak:
   - Grid Power
   - Solar Power
   - Battery Power
   - Load Power
   - Battery SOC

**Ne göreceksin:**
- ✅ **Status**: Connection ve Stream durumu
- ✅ **Live Data**: Her 5 saniyede güncellenecek veriler
- ✅ **Error**: Hata varsa burada görünür
- ✅ **Live Indicator**: 🔴 Streaming aktifken

**Test için Device ID nereden bulunur?**
```typescript
// Dashboard'dan bir device seç ve ID'sini kopyala
// Veya backend'den test device ID'si al
// Örnek format: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

---

### Yöntem 2: Console Log Test

React Native Debugger veya Metro bundler console'dan takip et.

**Adımlar:**
1. Metro bundler console'u aç (`npm start`)
2. Debug component'i kullanarak stream başlat
3. Console'da şunları göreceksin:

```
[PowerFlowService] Starting stream for device: xxx
[PowerFlowService] Connection state: Connecting
[PowerFlowService] Connection state: Connected
[PowerFlowStream] Data received: { gridPower: 1500, solarPower: 3000, ... }
[PowerFlowStream] Data received: { gridPower: 1520, solarPower: 3100, ... }
```

**Hata durumunda:**
```
[PowerFlowService] Error: SignalR connection failed
[PowerFlowService] Connection state: Failed
```

---

### Yöntem 3: Manuel Service Test

Direkt service'i import edip test et (App.tsx veya herhangi bir ekranda).

**Test kodu:**
```typescript
import { useEffect } from 'react';
import { powerFlowService } from '@/services/powerFlowService';

// Herhangi bir component içinde
useEffect(() => {
  const testDeviceId = 'YOUR-DEVICE-GUID-HERE';

  const test = async () => {
    try {
      console.log('🧪 Starting PowerFlow test...');

      const cleanup = await powerFlowService.startStream(
        testDeviceId,
        5, // 5 saniye interval
        // onData
        (data) => {
          console.log('✅ PowerFlow Data:', {
            timestamp: data.timestamp,
            gridPower: data.gridPower,
            solarPower: data.solarPower,
            batteryPower: data.batteryPower,
            loadPower: data.loadPower,
            batterySOC: data.batterySOC,
          });
        },
        // onError
        (error) => {
          console.error('❌ PowerFlow Error:', error.message);
        },
        // onComplete
        () => {
          console.log('🏁 PowerFlow stream completed');
        }
      );

      // 30 saniye sonra durdur
      setTimeout(() => {
        console.log('⏹️ Stopping test stream...');
        cleanup();
      }, 30000);

    } catch (error) {
      console.error('❌ Test failed:', error);
    }
  };

  test();

  return () => {
    powerFlowService.stopAllStreams();
  };
}, []);
```

---

### Yöntem 4: React Native Debugger Network Tab

SignalR WebSocket trafiğini gör.

**Adımlar:**
1. React Native Debugger aç: `open "rndebugger://set-debugger-loc?host=localhost&port=8081"`
2. Network tab'ı aç
3. Stream'i başlat
4. WebSocket connection'ları gör:
   - WS connection to `powerFlowHub`
   - Streaming messages (StreamPowerFlow)
   - Data frames (PowerFlowDto)

---

## 🔍 Kontrol Listesi

### Backend Hazır mı?

- [ ] PowerFlowHub deploy edilmiş mi?
- [ ] Hub URL doğru mu? (`powerFlowService.ts:46`)
  ```typescript
  private readonly HUB_URL = "https://hems-api-dev.oklidis.com.tr/powerFlowHub";
  ```
- [ ] `StreamPowerFlow` method implement edilmiş mi?
- [ ] Authentication çalışıyor mu?

### Frontend Test

- [ ] Debug component Admin sayfasında görünüyor mu?
- [ ] Device ID girince "Start Stream" butonu aktif mi?
- [ ] Stream başladığında:
  - [ ] Connection state "Connected" oldu mu?
  - [ ] Stream state "streaming" oldu mu?
  - [ ] Live indicator (🔴) görünüyor mu?
  - [ ] Data gelmeye başladı mı?
  - [ ] Timestamp her 5 saniyede güncelleniyor mu?

### Beklenen Davranışlar

✅ **Stream başlatıldığında:**
- Connection "Connecting" → "Connected"
- Stream state "idle" → "streaming"
- 5 saniye içinde ilk data gelir

✅ **App background'a geçince:**
- Stream otomatik durur
- Connection state "Disconnected"

✅ **App foreground'a dönünce:**
- Stream otomatik başlar (eğer enabled=true)
- Yeniden bağlanır

✅ **Component unmount olunca:**
- Stream otomatik durur
- Connection kapatılır (eğer başka aktif stream yoksa)

---

## ❌ Sık Karşılaşılan Hatalar

### 1. "No access token found"
**Çözüm:** Login ol, token'ın geçerli olduğundan emin ol

### 2. "SignalR connection failed"
**Nedenleri:**
- Backend PowerFlowHub'ı yok/kapalı
- Hub URL yanlış
- Network connectivity sorunu
- CORS hatası

**Çözüm:**
```typescript
// Hub URL'ini kontrol et
console.log(powerFlowService.HUB_URL); // Doğru URL'i yazdır
```

### 3. "Stream error" / Veri gelmiyor
**Nedenleri:**
- Device ID yanlış/mevcut değil
- Backend'de device için veri yok
- Interval parametresi backend'de desteklenmiyor

**Debug:**
```typescript
// Backend log'larını kontrol et
// Device ID'yi doğrula
// Farklı bir device ID dene
```

### 4. Stream başladı ama veri gelmiyor
**Çözüm:**
- Backend'de `yield return dto;` çağrısının yapıldığından emin ol
- Backend log'larına bak
- Network tab'da WebSocket frame'lerini kontrol et

### 5. "Connection state: Failed"
**Çözüm:**
- Token refresh gerekiyor olabilir → Logout/login yap
- Backend health check yap
- Hub URL ping'le

---

## 📊 Başarılı Test Örneği

```
🧪 Starting PowerFlow test...
[PowerFlowService] Connecting to hub...
[PowerFlowService] Connection state: Connecting
[PowerFlowService] Connection state: Connected
[PowerFlowService] Starting stream for device: a1b2c3d4-...
✅ PowerFlow Data: {
  timestamp: "2025-11-26T10:30:00Z",
  gridPower: 1500.5,
  solarPower: 3200.0,
  batteryPower: -800.0,  // Discharging
  loadPower: 2900.0,
  batterySOC: 65.5,
  batteryStatus: "discharging",
  gridStatus: "importing"
}
✅ PowerFlow Data: {
  timestamp: "2025-11-26T10:30:05Z",
  gridPower: 1520.0,
  solarPower: 3150.0,
  batteryPower: -750.0,
  loadPower: 2920.0,
  batterySOC: 65.3,
  batteryStatus: "discharging",
  gridStatus: "importing"
}
⏹️ Stopping test stream...
🏁 PowerFlow stream completed
[PowerFlowService] Connection state: Disconnected
```

---

## 🎯 Next Steps

Test başarılı olduktan sonra:

1. **Debug component'i kaldır** (AdminPage.tsx'ten)
2. **Gerçek device detail ekranlarına entegre et**:
   - InverterDetailScreen
   - BatteryDetailScreen
   - SmartMeterDetailScreen
3. **PowerFlowDto alanlarını backend'e göre güncelle** (gerekirse)
4. **Production hub URL'ini ayarla** (environment variable)
5. **Error handling iyileştir**
6. **Loading states ekle**

---

## 🆘 Yardım

Sorun mu yaşıyorsun?

1. **Backend'e sor:**
   - PowerFlowHub deploy edilmiş mi?
   - Endpoint URL nedir?
   - Test device ID nedir?
   - PowerFlowDto structure nedir?

2. **Frontend debug:**
   - Console log'ları paylaş
   - Connection state'i göster
   - Error message'ı kopyala

3. **Network debug:**
   - React Native Debugger Network tab
   - WebSocket frame'lerini incele
   - Proxy/VPN kapalı mı kontrol et
