# PowerFlow SignalR Test - Postman Guide

## ⚠️ Önemli Not

Postman ile **tam SignalR streaming** test edilemez. Ama bağlantı ve authentication test edilebilir.

## 🧪 Test Edilebilecekler

### 1. Hub Negotiation (Bağlantı Testi)

SignalR connection başlamadan önce negotiation yapılır. Bunu test edebiliriz.

**Request:**
```http
POST https://hems-api-dev.oklidis.com.tr/powerFlowHub/negotiate
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
  Content-Type: application/json
```

**Expected Response (200 OK):**
```json
{
  "connectionId": "abc123...",
  "connectionToken": "xyz789...",
  "negotiateVersion": 1,
  "availableTransports": [
    {
      "transport": "WebSockets",
      "transferFormats": ["Text", "Binary"]
    },
    {
      "transport": "ServerSentEvents",
      "transferFormats": ["Text"]
    },
    {
      "transport": "LongPolling",
      "transferFormats": ["Text", "Binary"]
    }
  ]
}
```

**✅ Bu çalışırsa:**
- Hub endpoint doğru
- Authentication çalışıyor
- Backend PowerFlowHub hazır

**❌ Hata alırsan:**
- `401 Unauthorized` → Token yanlış/expired
- `404 Not Found` → Hub URL yanlış veya deploy edilmemiş
- `500 Internal Server Error` → Backend hatası

---

### 2. WebSocket Connection (Postman WebSocket)

Postman'in WebSocket özelliği var ama SignalR protokolü için yeterli değil.

**Adımlar:**
1. Postman'de yeni WebSocket request oluştur
2. URL: `wss://hems-api-dev.oklidis.com.tr/powerFlowHub`
3. Headers ekle:
   ```
   Authorization: Bearer YOUR_TOKEN
   ```
4. Connect

**⚠️ Sorun:**
SignalR WebSocket üzerinde özel protokol kullanır. Manuel mesaj gönderip stream başlatmak zor.

---

## 💡 Önerilen Alternatifler

### Browser Console (En Kolay)

1. `test-scripts/browser-powerflow-test.js` dosyasını aç
2. Tüm içeriği kopyala
3. Chrome DevTools Console'a yapıştır (F12)
4. Çalıştır:
   ```javascript
   testPowerFlow("DEVICE-ID", "JWT-TOKEN", 5)
   ```

### Node.js Script (Profesyonel)

1. SignalR paketi yükle:
   ```bash
   npm install @microsoft/signalr
   ```

2. Script'i çalıştır:
   ```bash
   node test-scripts/node-powerflow-test.js "DEVICE-ID" "JWT-TOKEN" 5
   ```

3. Canlı verileri terminal'de gör

---

## 🔑 JWT Token Nasıl Alınır?

### Yöntem 1: React Native App'ten

1. Uygulamayı aç
2. Login ol
3. React Native Debugger'da:
   ```javascript
   AsyncStorage.getItem('access_token')
   ```

### Yöntem 2: Login API'sinden

Postman'de login endpoint'ini çağır:

```http
POST https://hems-api-dev.oklidis.com.tr/api/auth/login
Content-Type: application/json

{
  "email": "your@email.com",
  "password": "yourpassword"
}
```

Response'dan `accessToken` al.

---

## 📊 Test Device ID

Backend'den test için kullanılabilecek bir device ID al:

```http
GET https://hems-api-dev.oklidis.com.tr/api/sites
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
```

Response'dan bir site seç, onun `deviceId`'sini kullan.

---

## 🧪 Tam Test Akışı (Postman + Node.js)

### 1. Postman - Login
```http
POST /api/auth/login
{
  "email": "test@example.com",
  "password": "password123"
}
```
→ `accessToken` al

### 2. Postman - Get Sites
```http
GET /api/sites
Authorization: Bearer <accessToken>
```
→ `deviceId` al

### 3. Postman - Test Negotiation
```http
POST /powerFlowHub/negotiate
Authorization: Bearer <accessToken>
```
→ ✅ 200 OK olmalı

### 4. Node.js - Stream Test
```bash
node node-powerflow-test.js "<deviceId>" "<accessToken>" 5
```
→ Canlı veri akışı görülmeli

---

## ❌ Sık Hatalar

### 401 Unauthorized
**Çözüm:**
- Token doğru mu kontrol et
- Token expired olmamış mı bak
- Bearer prefix var mı kontrol et: `Bearer eyJ...`

### 404 Not Found
**Çözüm:**
- Hub URL doğru mu: `/powerFlowHub`
- Backend deploy edilmiş mi kontrol et
- Health check endpoint'i var mı dene:
  ```http
  GET https://hems-api-dev.oklidis.com.tr/health
  ```

### 500 Internal Server Error
**Çözüm:**
- Backend log'larına bak
- Hub class'ı doğru implement edilmiş mi
- Device ID valid mi kontrol et

### WebSocket connection failed
**Çözüm:**
- HTTPS/WSS kullan (HTTP/WS değil)
- CORS ayarları doğru mu
- Firewall/Proxy engel mi

---

## 📝 Postman Collection (Export Ready)

```json
{
  "info": {
    "name": "HEMS PowerFlow SignalR Test",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "1. Login",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"email\": \"{{email}}\",\n  \"password\": \"{{password}}\"\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/auth/login",
          "host": ["{{baseUrl}}"],
          "path": ["api", "auth", "login"]
        }
      },
      "response": []
    },
    {
      "name": "2. Get Sites",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{accessToken}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/sites",
          "host": ["{{baseUrl}}"],
          "path": ["api", "sites"]
        }
      },
      "response": []
    },
    {
      "name": "3. PowerFlow Hub - Negotiate",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{accessToken}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/powerFlowHub/negotiate",
          "host": ["{{baseUrl}}"],
          "path": ["powerFlowHub", "negotiate"]
        }
      },
      "response": []
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://hems-api-dev.oklidis.com.tr"
    },
    {
      "key": "email",
      "value": "your@email.com"
    },
    {
      "key": "password",
      "value": "yourpassword"
    },
    {
      "key": "accessToken",
      "value": ""
    }
  ]
}
```

**Kullanım:**
1. Bu JSON'u kopyala
2. Postman → Import → Raw Text
3. Variables'ı güncelle (email, password)
4. "1. Login" çalıştır → accessToken'ı kopyala → Variables'a yapıştır
5. Diğer request'leri çalıştır

---

## 🎯 Sonuç

**Postman ile:**
- ✅ Negotiation test edilir
- ✅ Authentication doğrulanır
- ✅ Hub endpoint kontrol edilir
- ❌ Streaming tam test edilemez

**Streaming için:**
- ✅ Browser Console script kullan
- ✅ Node.js script kullan
- ✅ React Native debug component kullan (en iyi)
