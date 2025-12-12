# Node-RED Remote Flow Management via ThingsBoard RPC

## 📋 Genel Bakış

Bu sistem, ThingsBoard RPC (Remote Procedure Call) üzerinden EC100 cihazlarındaki Node-RED flow'larını uzaktan yönetmenizi sağlar.

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Kullanıcı   │────▶│  ThingsBoard │────▶│    EC100     │────▶│   Node-RED   │
│  (API/GUI)   │     │  (RPC Proxy) │     │  (Gateway)   │     │   (Flows)    │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
```

## 🔧 Gereksinimler

### EC100 Tarafı
- Node-RED çalışıyor olmalı
- `RPC_FlowManager` flow'u import edilmiş olmalı
- ThingsBoard MQTT bağlantısı aktif olmalı

### ThingsBoard Tarafı
- Cihaz tanımlı ve aktif olmalı
- Device Access Token yapılandırılmış olmalı

---

## 📡 Kullanılabilir RPC Metodları

### Tek Flow İşlemleri (Önerilen)

| Metod | Açıklama | Timeout Riski |
|-------|----------|---------------|
| `addFlow` | Yeni bir flow tab'ı ekler | ✅ Düşük |
| `updateFlow` | Mevcut bir flow'u günceller | ✅ Düşük |
| `removeFlow` | Tek bir flow'u siler | ✅ Düşük |

### Toplu İşlemler

| Metod | Açıklama | Timeout Riski |
|-------|----------|---------------|
| `getFlows` | Tüm flow'ları listeler | ⚠️ Orta |
| `setFlows` | Tüm flow'ları değiştirir | 🔴 Yüksek |
| `deleteFlows` | Tüm flow'ları siler | 🔴 Tehlikeli |

---

## 🚀 Kullanım Örnekleri

### Kimlik Doğrulama (JWT Token Alma)

```powershell
$response = Invoke-RestMethod -Uri "https://YOUR_TB_URL/api/auth/login" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"username":"YOUR_USERNAME","password":"YOUR_PASSWORD"}'

$token = $response.token
$headers = @{"X-Authorization" = "Bearer $token"}
```

### getFlows - Flow'ları Listele

```powershell
$result = Invoke-RestMethod `
  -Uri "https://YOUR_TB_URL/api/rpc/twoway/DEVICE_ID" `
  -Method Post `
  -ContentType "application/json" `
  -Headers $headers `
  -Body '{"method": "getFlows", "params": {}}'

# Sadece tab'ları göster
$result.flows | Where-Object { $_.type -eq "tab" } | Select-Object id, label
```

**Yanıt:**
```json
{
  "success": true,
  "flowCount": 8,
  "flows": [...]
}
```

### addFlow - Yeni Flow Ekle

```powershell
# Dosyadan oku
$rawFlow = Get-Content "path/to/flow.json" -Raw | ConvertFrom-Json

# Tab node'unu bul
$tabNode = $rawFlow | Where-Object { $_.type -eq "tab" }

# Diğer node'ları al (config'ler hariç)
$nodes = $rawFlow | Where-Object { 
  $_.type -ne "tab" -and 
  $_.type -notlike "*-broker" -and 
  $_.type -notlike "*-client" 
}

# addFlow formatına çevir
$body = @{
  method = "addFlow"
  params = @{
    flow = @{
      id = $tabNode.id
      label = $tabNode.label
      disabled = $tabNode.disabled
      info = $tabNode.info
      nodes = @($nodes)
    }
  }
} | ConvertTo-Json -Depth 10 -Compress

# Gönder
Invoke-RestMethod `
  -Uri "https://YOUR_TB_URL/api/rpc/twoway/DEVICE_ID" `
  -Method Post `
  -ContentType "application/json" `
  -Headers $headers `
  -Body $body
```

**Yanıt:**
```json
{
  "success": true,
  "message": "Flow added successfully",
  "flowId": "abc123..."
}
```

### updateFlow - Flow Güncelle

```powershell
$body = @{
  method = "updateFlow"
  params = @{
    id = "FLOW_TAB_ID"
    flow = @{
      label = "Updated Flow Name"
      nodes = @(...)
    }
  }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
  -Uri "https://YOUR_TB_URL/api/rpc/twoway/DEVICE_ID" `
  -Method Post `
  -ContentType "application/json" `
  -Headers $headers `
  -Body $body
```

**Yanıt:**
```json
{
  "success": true,
  "message": "Flow updated successfully",
  "flowId": "FLOW_TAB_ID"
}
```

### removeFlow - Flow Sil

```powershell
$body = '{"method": "removeFlow", "params": {"id": "FLOW_TAB_ID"}}'

Invoke-RestMethod `
  -Uri "https://YOUR_TB_URL/api/rpc/twoway/DEVICE_ID" `
  -Method Post `
  -ContentType "application/json" `
  -Headers $headers `
  -Body $body
```

**Yanıt:**
```json
{
  "success": true,
  "message": "Flow removed successfully",
  "flowId": "FLOW_TAB_ID"
}
```

### setFlows - Toplu Güncelleme

⚠️ **DİKKAT:** Bu metod TÜM flow'ları değiştirir!

```powershell
# Önce mevcut flow'ları al
$current = Invoke-RestMethod ... -Body '{"method": "getFlows", "params": {}}'

# Yeni flow'u ekle
$newFlow = Get-Content "new_flow.json" -Raw | ConvertFrom-Json
$merged = @($current.flows) + @($newFlow)

# Gönder
$body = @{
  method = "setFlows"
  params = @{
    deploymentType = "full"
    flows = $merged
  }
} | ConvertTo-Json -Depth 10 -Compress

Invoke-RestMethod `
  -Uri "https://YOUR_TB_URL/api/rpc/twoway/DEVICE_ID" `
  -Method Post `
  -ContentType "application/json" `
  -Headers $headers `
  -Body $body `
  -TimeoutSec 120
```

### deleteFlows - Tümünü Sil

🔴 **TEHLİKELİ:** Bu metod FlowManager dahil TÜM flow'ları siler!

```powershell
# ÖNCE BACKUP AL!
$backup = Invoke-RestMethod ... -Body '{"method": "getFlows", "params": {}}'
$backup.flows | ConvertTo-Json -Depth 10 | Out-File "backup.json"

# Sil
Invoke-RestMethod `
  -Uri "https://YOUR_TB_URL/api/rpc/twoway/DEVICE_ID" `
  -Method Post `
  -ContentType "application/json" `
  -Headers $headers `
  -Body '{"method": "deleteFlows", "params": {}}'
```

**Recovery:** deleteFlows sonrası RPC çalışmaz! Node-RED UI'dan manuel import gerekir.

---

## 🛡️ Güvenlik Notları

1. **FlowManager Koruması:** `removeFlow` metodu `rpc_flow_manager_tab` ID'sini silmeyi reddeder.

2. **Backup:** Kritik işlemlerden önce mutlaka `getFlows` ile backup alın.

3. **Timeout:** Büyük flow'larda `setFlows` timeout olabilir. `addFlow` kullanmayı tercih edin.

4. **OneWay RPC:** Cevap beklemeden işlem yapmak için:
   ```
   /api/rpc/oneway/DEVICE_ID  (twoway yerine)
   ```

---

## 📁 Flow JSON Formatı

### Standart Node-RED Export Formatı
```json
[
  {
    "id": "flow_tab_id",
    "type": "tab",
    "label": "Flow Name",
    "disabled": false,
    "info": "Description"
  },
  {
    "id": "node_id",
    "type": "inject",
    "z": "flow_tab_id",
    "name": "Node Name",
    "x": 100,
    "y": 100,
    "wires": [["next_node_id"]]
  }
]
```

### addFlow Params Formatı
```json
{
  "method": "addFlow",
  "params": {
    "flow": {
      "id": "optional_custom_id",
      "label": "Flow Name",
      "disabled": false,
      "info": "Description",
      "nodes": [...],
      "configs": [...]
    }
  }
}
```

---

## 🔄 Tipik İş Akışları

### Yeni Cihaz Kurulumu
1. EC100'e base flow'ları yükle (FlowManager dahil)
2. ThingsBoard'da cihazı tanımla
3. `addFlow` ile uygulama flow'larını ekle

### Flow Güncelleme
1. `getFlows` ile mevcut durumu al (backup)
2. `updateFlow` ile tek flow'u güncelle
3. Doğrula

### Toplu Deployment (100+ cihaz)
1. Her cihaz için aynı `addFlow` çağrısını yap
2. ThingsBoard Rule Chain ile otomatikleştir
3. Sonuçları logla

---

## 🐛 Sorun Giderme

| Sorun | Çözüm |
|-------|-------|
| 504 Timeout | `addFlow` kullan veya `oneway` RPC |
| "Flow not found" | Flow ID'yi `getFlows` ile doğrula |
| MQTT disconnected | EC100 network bağlantısını kontrol et |
| Token expired | Yeni JWT token al |

---

## 📚 İlgili Linkler

- [Node-RED Admin API](https://nodered.org/docs/api/admin/)
- [ThingsBoard RPC](https://thingsboard.io/docs/user-guide/rpc/)
- [MQTT Protocol](https://mqtt.org/)

---

**Versiyon:** 2.0  
**Tarih:** Aralık 2024  
**Yazar:** nmn