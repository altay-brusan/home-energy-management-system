# API Endpoints Documentation

Bu dokuman `src/api/client/config.ts` dosyasindaki tum API endpoint'lerini ve kullanim durumlarini icerir.

**Base URL:** `https://hems-api-stg.oklidis.com.tr/api`

---

## Ozet Istatistikler

| Metrik | Deger |
|--------|-------|
| Toplam Endpoint | 174 |
| Kullanilan | 165 (%94.8) |
| Kullanilmayan | 9 (%5.2) |
| Endpoint Gruplari | 17 |

---

## AUTH - Kimlik Dogrulama

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| LOGIN | `/Users/login` | Kullaniliyor | Kullanici giris islemi |
| REGISTER | `/Users/register` | Kullaniliyor | Yeni kullanici kaydi |
| REFRESH | `/Users/refresh` | Kullaniliyor | Token yenileme islemi |
| USER_STATUS | `/Auth/user-status` | Kullaniliyor | Kullanici dogrulama durumu |
| USER_STATUS_MOBILE | `/Auth/mobile/get-user-status` | Kullaniliyor | Mobil kullanici durumu kontrolu |
| FORGOT_PASSWORD | `/Users/forgotPassword` | Kullaniliyor | Sifre sifirlama istegi |
| RESET_PASSWORD | `/Users/resetPassword` | Kullaniliyor | Sifre sifirlama tamamlama |
| CONFIRM_EMAIL | `/Users/confirmEmail` | Kullaniliyor | E-posta dogrulama |
| RESEND_CONFIRMATION | `/Users/resendConfirmationEmail` | Kullaniliyor | Dogrulama e-postasi yeniden gonderme |
| USER_INFO | `/Users/userInfo2` | Kullaniliyor | Kullanici profil bilgileri |
| MANAGE_2FA | `/Users/manage/2fa` | **KULLANILMIYOR** | Iki faktorlu dogrulama yonetimi |
| MANAGE_INFO | `/Users/manage/info` | Kullaniliyor | Kullanici yonetim bilgileri |
| UPDATE_PROFILE | `/Users/update-profile` | Kullaniliyor | Profil guncelleme |

---

## SITES - Siteler/Evler

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/Sites` | Kullaniliyor | Tum siteleri sayfalama ile listele |
| GET_BY_ID | `/Sites/:id` | Kullaniliyor | ID'ye gore site getir |
| GET_USER_SITES | `/Sites/mobile/get-sites` | Kullaniliyor | Mobil icin kullanici sitelerini getir |
| CREATE | `/Sites/mobile/create-site` | Kullaniliyor | Yeni site olustur |
| UPDATE | `/Sites/mobile/update-site/:id` | Kullaniliyor | Site guncelle |
| DELETE_MOBILE | `/Sites/mobile/delete-site/:id` | Kullaniliyor | Site sil (mobil) |
| DELETE_ADMIN | `/Sites/:id` | Kullaniliyor | Site sil (admin) |

---

## SETUPS - Kurulumlar

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/Setups` | Kullaniliyor | Tum kurulumlari listele |
| GET_BY_ID | `/Setups/:id` | Kullaniliyor | ID'ye gore kurulum getir |
| GET_SETUP_DEVICE | `/Setups/mobile/get-setup-device` | Kullaniliyor | Site icin kurulum cihazini getir |
| CREATE | `/Setups/mobile/create-setup` | Kullaniliyor | Yeni kurulum olustur |
| UPDATE | `/Setups/mobile/update-setup/:id` | Kullaniliyor | Kurulum guncelle |
| DELETE_MOBILE | `/Setups/mobile/delete-setup/:id` | Kullaniliyor | Kurulum sil (mobil) |
| DELETE_ADMIN | `/Setups/:id` | Kullaniliyor | Kurulum sil (admin) |

---

## DEVICES - Cihazlar

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/Devices` | Kullaniliyor | Tum cihazlari listele |
| GET_BY_ID | `/Devices/:id` | Kullaniliyor | ID'ye gore cihaz getir |
| CREATE | `/Devices` | Kullaniliyor | Yeni cihaz olustur |
| UPDATE | `/Devices/:id` | Kullaniliyor | Cihaz guncelle |
| DELETE | `/Devices/:id` | Kullaniliyor | Cihaz sil |
| LOGIN | `/Devices/login` | Kullaniliyor | Cihaz kimlik dogrulamasi |

---

## DEVICE_COMPONENTS - Cihaz Bilesenleri

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_BY_DEVICE | `/DeviceComponents/device/:deviceId` | Kullaniliyor | Cihaza ait bilesenleri getir |
| GET_BY_ID | `/DeviceComponents/:id` | Kullaniliyor | ID'ye gore bilesen getir |
| CREATE | `/DeviceComponents` | Kullaniliyor | Yeni bilesen olustur |
| UPDATE | `/DeviceComponents/:id` | Kullaniliyor | Bilesen guncelle |
| DELETE | `/DeviceComponents/:id` | Kullaniliyor | Bilesen sil |
| GET_DEVICE_SUMMARY | `/DeviceComponents/DeviceSummary` | Kullaniliyor | Canli verilerle cihaz ozeti |
| GET_COMPONENT_DETAILS | `/DeviceComponents/Details/:componentId` | Kullaniliyor | Detayli bilesen bilgisi |

---

## DEVICE_DATA - Cihaz Verileri

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/DeviceData` | Kullaniliyor | Cihaz verilerini sayfalama ile getir |
| CREATE | `/DeviceData` | Kullaniliyor | Tekil cihaz verisi olustur |
| CREATE_ENERGY | `/DeviceData/energy` | Kullaniliyor | Enerji spesifik veri olustur |
| BULK_CREATE | `/DeviceData/bulk` | Kullaniliyor | Toplu veri olusturma |

---

## DASHBOARD - Kontrol Paneli

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_MOBILE | `/Dashboard/mobile/get-dashboard` | Kullaniliyor | Mobil dashboard verisi |
| GET_COMPONENT_DATA | `/Dashboard/mobile/device/:deviceId/component/:componentId/data` | Kullaniliyor | Bilesen spesifik veri |
| HERO | `/Dashboard/hero` | Kullaniliyor | Ana metrikler (baslik, alt baslik) |
| SOC | `/Dashboard/Soc` | Kullaniliyor | Batarya sarj durumu (State of Charge) |
| SOH | `/Dashboard/Soh` | Kullaniliyor | Batarya saglik durumu (State of Health) |
| TODAY_SUMMARY | `/Dashboard/TodayEnergySummary` | Kullaniliyor | Gunluk enerji ozeti |
| POWER_AND_ENERGY_METRICS | `/Dashboard/PowerAndEnergyMetrics` | Kullaniliyor | Guc ve enerji metrikleri |
| GET_DEVICE_WITH_DATA | `/Dashboard/device/GetWithData` | Kullaniliyor | Tum bilesen verileriyle cihaz |
| GET_COMPONENT_LIVE_DATA | `/Dashboard/device/component/getComponentLiveData` | Kullaniliyor | Gercek zamanli bilesen verisi |
| BATTERY_COMPONENT | `/Dashboard/graphs/Components/Battery` | Kullaniliyor | Batarya bilesen metrikleri |
| INVERTER_COMPONENT | `/Dashboard/graphs/Components/Inverter` | Kullaniliyor | Inverter bilesen metrikleri |
| ENERGY_FLOW | `/Dashboard/graphs/EnergyFlow` | Kullaniliyor | Kaynaklar arasi enerji akisi |
| ENERGY_PRODUCTION | `/Dashboard/graphs/EnergyProduction` | Kullaniliyor | Enerji uretim verisi |
| ENERGY_CONSUMPTION | `/Dashboard/graphs/EnergyConsumption` | Kullaniliyor | Enerji tuketim verisi |
| POWER_METRICS | `/Dashboard/graphs/PowerMetrics` | Kullaniliyor | Guc metrikleri |
| BATTERY | `/Dashboard/graphs/Battery` | Kullaniliyor | Batarya verisi |
| CHARGE_DISCHARGE | `/Dashboard/graphs/ChargeDischarge` | Kullaniliyor | Batarya sarj/desarj oranlari |
| GRID_PERFORMANCE | `/Dashboard/GridPerformance` | Kullaniliyor | Sebeke ithalat/ihracat performansi |
| GRID_PERFORMANCE_SUMMARY | `/Dashboard/GridPerformanceSummary` | Kullaniliyor | Sebeke performans ozeti |
| GET_WEATHER_FORECAST | `/WeatherForecasts/:siteId` | Kullaniliyor | Site icin hava tahmini |
| ENERGY_REVENUE | `/Dashboard/EnergyRevenue` | Kullaniliyor | Enerji gelir verisi |

---

## ROLES - Rol Yonetimi

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/RoleManagement` | **KULLANILMIYOR** | Tum rolleri listele |
| CREATE | `/RoleManagement` | **KULLANILMIYOR** | Yeni rol olustur |
| DELETE | `/RoleManagement/:roleName` | **KULLANILMIYOR** | Rol sil |
| ASSIGN | `/RoleManagement/assign` | **KULLANILMIYOR** | Kullaniciya rol ata |
| GET_USER_ROLES | `/RoleManagement/user/:userId` | **KULLANILMIYOR** | Kullanici rollerini getir |

> **Not:** ROLES grubu tamamen kullanilmamaktadir. Gelecekte planlanmis bir ozellik veya kalinti kod olabilir.

---

## INVITATIONS - Davetler

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| CREATE | `/Invitations/create` | Kullaniliyor | Cihaz paylasimi daveti olustur |
| GET_BY_SITE | `/Invitations/by-site/:siteId` | Kullaniliyor | Site davetlerini getir |
| ACCEPT | `/Invitations/accept` | Kullaniliyor | Daveti kabul et |
| DECLINE | `/Invitations/decline` | Kullaniliyor | Daveti reddet |
| DELETE | `/Invitations/:invitationId` | Kullaniliyor | Daveti sil |
| REMOVE_USER | `/Invitations/remove-user` | Kullaniliyor | Kullaniciyi siteden kaldir |
| USER_HOME_SITES | `/Invitations/user-home-sites` | **KULLANILMIYOR** | Kullanici ev siteleri |
| GET_NOTIFICATIONS | `/Invitations/notifications` | **KULLANILMIYOR** | Davet bildirimlerini getir |
| MARK_NOTIFICATION_READ | `/Invitations/notifications/:notificationId/mark-read` | **KULLANILMIYOR** | Bildirimi okundu isaretle |

---

## DEVICE_NOTIFICATIONS - Cihaz Bildirimleri

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| CREATE | `/DeviceNotifications` | Kullaniliyor | Cihaz bildirimi olustur |
| GET_ALL | `/DeviceNotifications` | Kullaniliyor | Tum bildirimleri sayfalama ile getir |
| GET_DELETED | `/DeviceNotifications/deleted` | Kullaniliyor | Silinen bildirimleri getir |
| GET_TYPES | `/DeviceNotifications/types` | Kullaniliyor | Bildirim tiplerini getir |
| MARK_READ | `/DeviceNotifications/:id/mark-read` | Kullaniliyor | Bildirimi okundu isaretle |
| MARK_ALL_READ | `/DeviceNotifications/mark-all-read/:siteId` | Kullaniliyor | Site icin tum bildirimleri okundu isaretle |
| DELETE | `/DeviceNotifications/:id` | Kullaniliyor | Bildirimi sil |
| RESTORE | `/DeviceNotifications/:id/restore` | Kullaniliyor | Silinen bildirimi geri yukle |
| PERMANENT_DELETE | `/DeviceNotifications/:id/permanent` | Kullaniliyor | Bildirimi kalici olarak sil |

---

## TODO_ITEMS - Yapilacaklar

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/TodoItems` | Kullaniliyor | Yapilacaklari sayfalama ile getir |
| CREATE | `/TodoItems` | Kullaniliyor | Yeni yapilacak olustur |
| UPDATE | `/TodoItems/:id` | Kullaniliyor | Yapilacak guncelle |
| UPDATE_DETAIL | `/TodoItems/UpdateDetail/:id` | Kullaniliyor | Yapilacak detaylarini guncelle |
| DELETE | `/TodoItems/:id` | Kullaniliyor | Yapilacak sil |

---

## TODO_LISTS - Yapilacak Listeleri

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/TodoLists` | Kullaniliyor | Tum yapilacak listelerini getir |
| CREATE | `/TodoLists` | Kullaniliyor | Yeni liste olustur |
| UPDATE | `/TodoLists/:id` | Kullaniliyor | Liste guncelle |
| DELETE | `/TodoLists/:id` | Kullaniliyor | Liste sil |

---

## WEATHER_FORECASTS - Hava Tahminleri

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_BY_SITE | `/WeatherForecasts/:siteId` | Kullaniliyor | Site icin hava tahminlerini getir |

---

## LOCATIONS - Konumlar

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_COUNTRIES | `/Locations` | Kullaniliyor | Tum ulkeleri getir |
| GET_CITIES_BY_COUNTRY | `/Locations/cities/:countryId` | Kullaniliyor | Ulkeye gore sehirleri getir |

---

## PARAMETER_GROUPS - Parametre Gruplari

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/ParameterGroups` | Kullaniliyor | Tum parametre gruplarini getir |
| GET_BY_ID | `/ParameterGroups/:id` | Kullaniliyor | ID'ye gore parametre grubu getir |
| CREATE | `/ParameterGroups` | Kullaniliyor | Parametre grubu olustur |
| UPDATE | `/ParameterGroups/:id` | Kullaniliyor | Parametre grubu guncelle |
| DELETE | `/ParameterGroups/:id` | Kullaniliyor | Parametre grubu sil |

---

## PARAMETERS - Parametreler

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/Parameters` | Kullaniliyor | Tum parametreleri getir |
| GET_BY_ID | `/Parameters/:id` | Kullaniliyor | ID'ye gore parametre getir |
| CREATE | `/Parameters` | Kullaniliyor | Parametre olustur |
| UPDATE | `/Parameters/:id` | Kullaniliyor | Parametre guncelle |
| DELETE | `/Parameters/:id` | Kullaniliyor | Parametre sil |

---

## PARAMETER_VALUE_SETTING_SCHEMES - Parametre Deger Ayar Semalari

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/ParameterValueSettingSchemes` | Kullaniliyor | Tum ayar semalarini getir |
| GET_TREE | `/ParameterValueSettingSchemes/tree` | Kullaniliyor | Agac formatinda semalar |
| CREATE | `/ParameterValueSettingSchemes` | Kullaniliyor | Ayar semasi olustur |
| UPDATE | `/ParameterValueSettingSchemes/:id` | Kullaniliyor | Ayar semasi guncelle |
| DELETE | `/ParameterValueSettingSchemes/:id` | Kullaniliyor | Ayar semasi sil |

---

## SETUP_DEVICE_PARAMETER_VALUES - Kurulum Cihaz Parametre Degerleri

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| GET_ALL | `/SetupDeviceParameterValues` | Kullaniliyor | Tum parametre degerlerini getir |
| GET_TREE | `/SetupDeviceParameterValues/tree` | Kullaniliyor | Agac formatinda parametre degerleri |
| CREATE | `/SetupDeviceParameterValues` | Kullaniliyor | Parametre degeri olustur |
| UPDATE | `/SetupDeviceParameterValues/:id` | Kullaniliyor | Parametre degeri guncelle |
| DELETE | `/SetupDeviceParameterValues/:id` | Kullaniliyor | Parametre degeri sil |

---

## DEVICE_SETTINGS - Cihaz Ayarlari

| Endpoint | Path | Kullanim Durumu | Aciklama |
|----------|------|-----------------|----------|
| CHANGE_EXPORT_STRATEGY | `/DeviceSettings/ChangeExportStrategy` | Kullaniliyor | Enerji ihracat stratejisini degistir |
| GET_TREE | `/DeviceSettings/device/:deviceId/tree` | Kullaniliyor | Cihaz ayarlari agaci |
| UPDATE_PARAMETERS | `/DeviceSettings/device/:deviceId/parameters` | Kullaniliyor | Cihaz parametrelerini guncelle |
| GET_MODES | `/DeviceSettings/modes` | Kullaniliyor | Mevcut inverter modlarini getir |
| GET_DEVICE_MODE | `/DeviceSettings/device/:deviceId/mode` | Kullaniliyor | Mevcut cihaz modunu getir |
| UPDATE_DEVICE_MODE | `/DeviceSettings/device/:deviceId/mode` | Kullaniliyor | Cihaz modunu guncelle |
| GET_DEVICE_PRICE | `/DeviceSettings/device/:deviceId/price` | Kullaniliyor | Cihaz fiyat ayarlarini getir |
| UPDATE_DEVICE_PRICE | `/DeviceSettings/device/:deviceId/price` | Kullaniliyor | Cihaz fiyat ayarlarini guncelle |

---

## Kullanilmayan Endpoint'ler Ozeti

| Grup | Endpoint | Path |
|------|----------|------|
| AUTH | MANAGE_2FA | `/Users/manage/2fa` |
| ROLES | GET_ALL | `/RoleManagement` |
| ROLES | CREATE | `/RoleManagement` |
| ROLES | DELETE | `/RoleManagement/:roleName` |
| ROLES | ASSIGN | `/RoleManagement/assign` |
| ROLES | GET_USER_ROLES | `/RoleManagement/user/:userId` |
| INVITATIONS | USER_HOME_SITES | `/Invitations/user-home-sites` |
| INVITATIONS | GET_NOTIFICATIONS | `/Invitations/notifications` |
| INVITATIONS | MARK_NOTIFICATION_READ | `/Invitations/notifications/:notificationId/mark-read` |

---

*Son guncelleme: 2026-01-09*
