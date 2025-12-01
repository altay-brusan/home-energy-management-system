# 📚 HEMS Backend - Application Katmanı Feature Dokümantasyonu

Bu dokümantasyon, HEMS Backend projesinin Application katmanındaki tüm feature klasörlerini, içerisindeki Command ve Query'lerin işlevlerini ve detaylı açıklamalarını içerir.

## 📋 İçindekiler

- [Auth](#auth)
- [Dashboard](#dashboard)
- [DeviceComponents](#devicecomponents)
- [DeviceData](#devicedata)
- [DeviceNotifications](#devicenotifications)
- [Devices](#devices)
- [Invitations](#invitations)
- [Locations](#locations)
- [Mobile](#mobile)
- [Parameters](#parameters)
- [ParameterGroups](#parametergroups)
- [ParameterValueSettingSchemes](#parametervaluesettingschemes)
- [RoleManagement](#rolemanagement)
- [SetupDeviceParameterValues](#setupdeviceparametervalues)
- [SetupDevices](#setupdevices)
- [Setups](#setups)
- [Sites](#sites)
- [TodoItems](#todoitems)
- [TodoLists](#todolists)
- [Users](#users)
- [WeatherForecasts](#weatherforecasts)

---

## 🔐 Auth

### Queries

#### GetUserStatus
**Dosya Yolu:** `Auth/Queries/GetUserStatus/GetUserStatus.cs`

**Açıklama:** Kullanıcının sistemdeki durumunu kontrol eden query'dir. Kullanıcının Site'ı, Setup'ı ve Device'ı olup olmadığını kontrol eder ve kullanıcıyı uygun sayfaya yönlendirmek için durum bilgisi döner.

**İşlevi:**
- Kullanıcının Site'ı var mı kontrol eder
- Site'ın Setup'ı var mı kontrol eder
- Setup'ın Device'ı var mı kontrol eder
- Duruma göre `UserStatusType` döner:
  - `NeedsSite`: Kullanıcının site'ı yok
  - `NeedsSetup`: Site var ama setup yok
  - `Complete`: Her şey tamam, dashboard'a yönlendirilebilir

**Return Type:** `UserStatusDto`

**Kullanım Senaryosu:** Kullanıcı login olduktan sonra, sistem kullanıcıyı doğru sayfaya yönlendirmek için bu query'yi çağırır.

---

## 📊 Dashboard

### Queries

#### GetHero
**Dosya Yolu:** `Dashboard/Queries/GetHero/GetHeroQuery.cs`

**Açıklama:** Dashboard'un ana hero bölümü için özet bilgileri getiren query'dir. Device'a ait genel durum bilgilerini içerir.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği

**Return Type:** `HeroResponseDto`

**Kullanım Senaryosu:** Dashboard'un üst kısmında gösterilen özet bilgiler (toplam üretim, tüketim, şarj/deşarj durumu vb.)

---

#### GetBattery
**Dosya Yolu:** `Dashboard/Queries/GetBattery/GetBatteryQuery.cs`

**Açıklama:** Device'a ait batarya bilgilerini getiren query'dir. Batarya durumu, kapasite, şarj seviyesi gibi bilgileri içerir.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği

**Return Type:** `BatteryResponseDto`

**Kullanım Senaryosu:** Dashboard'da batarya durumunu göstermek için kullanılır.

---

#### GetSOC (State of Charge)
**Dosya Yolu:** `Dashboard/Queries/GetSoc/GetSocQuery.cs`

**Açıklama:** Bataryanın şarj durumunu (State of Charge) getiren query'dir. Bataryanın ne kadar dolu olduğunu gösterir.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği

**Return Type:** `SocResponseDto`

**Kullanım Senaryosu:** Dashboard'da batarya şarj seviyesini yüzde olarak göstermek için kullanılır.

---

#### GetSOH (State of Health)
**Dosya Yolu:** `Dashboard/Queries/GetSoh/GetSohQuery.cs`

**Açıklama:** Bataryanın sağlık durumunu (State of Health) getiren query'dir. Bataryanın kullanım ömrü ve sağlığı hakkında bilgi verir.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği

**Return Type:** `SohResponseDto`

**Kullanım Senaryosu:** Dashboard'da batarya sağlık durumunu göstermek için kullanılır.

---

#### GetEnergyProduction
**Dosya Yolu:** `Dashboard/Queries/GetEnergyProduction/GetEnergyProductionQuery.cs`

**Açıklama:** Enerji üretim verilerini getiren query'dir. PV (güneş paneli) üretim verilerini zaman dilimine göre (günlük, haftalık, aylık, yıllık) döner.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği
- `Period` (string): Zaman dilimi - "daily", "weekly", "monthly", "yearly"

**Return Type:** `EnergyProductionResponseDto`

**Kullanım Senaryosu:** Dashboard'da enerji üretim grafiğini göstermek için kullanılır.

---

#### GetEnergyConsumption
**Dosya Yolu:** `Dashboard/Queries/GetEnergyConsumption/GetEnergyConsumptionQuery.cs`

**Açıklama:** Enerji tüketim verilerini getiren query'dir. Yük tüketim verilerini zaman dilimine göre döner.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği
- `Period` (string): Zaman dilimi - "daily", "weekly", "monthly", "yearly"

**Return Type:** `EnergyConsumptionResponseDto`

**Kullanım Senaryosu:** Dashboard'da enerji tüketim grafiğini göstermek için kullanılır.

---

#### GetChargeDischarge
**Dosya Yolu:** `Dashboard/Queries/GetChargeDischarge/GetChargeDischargeQuery.cs`

**Açıklama:** Bataryanın şarj ve deşarj verilerini getiren query'dir. Bataryanın ne zaman şarj olduğu ve ne zaman deşarj olduğu bilgilerini içerir.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği

**Return Type:** `ChargeDischargeResponseDto`

**Kullanım Senaryosu:** Dashboard'da batarya şarj/deşarj grafiğini göstermek için kullanılır.

---

#### GetPowerMetrics
**Dosya Yolu:** `Dashboard/Queries/GetPowerMetrics/GetPowerMetricsQuery.cs`

**Açıklama:** Güç metriklerini getiren query'dir. Anlık güç değerlerini (PV üretimi, grid import/export, yük tüketimi vb.) zaman dilimine göre döner.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği
- `Period` (string): Zaman dilimi - "daily", "weekly", "monthly", "yearly"

**Return Type:** `PowerMetricsResponseDto`

**Kullanım Senaryosu:** Dashboard'da güç metrikleri grafiğini göstermek için kullanılır.

---

#### GetEnergyData
**Dosya Yolu:** `Dashboard/Queries/GetEnergyData/GetEnergyDataQuery.cs`

**Açıklama:** Genel enerji verilerini getiren query'dir. Tüm enerji akışı bilgilerini (üretim, tüketim, grid, batarya) içerir.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği

**Return Type:** `EnergyDataDto`

**Kullanım Senaryosu:** Dashboard'da enerji akış diyagramı için kullanılır.

---

#### GetBatteryComponent
**Dosya Yolu:** `Dashboard/Queries/GetBatteryComponent/GetBatteryComponentQuery.cs`

**Açıklama:** Batarya component'ine ait detaylı bilgileri getiren query'dir.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği

**Return Type:** `BatteryComponentDto`

**Kullanım Senaryosu:** Dashboard'da batarya component detaylarını göstermek için kullanılır.

---

#### GetInverterComponent
**Dosya Yolu:** `Dashboard/Queries/GetInverterComponent/GetInverterComponentQuery.cs`

**Açıklama:** Inverter component'ine ait detaylı bilgileri getiren query'dir.

**Parametreler:**
- `DeviceId` (Guid): Device'ın benzersiz kimliği

**Return Type:** `InverterComponentDto`

**Kullanım Senaryosu:** Dashboard'da inverter component detaylarını göstermek için kullanılır.

---

## 🔧 DeviceComponents

### Commands

#### CreateDeviceComponent
**Dosya Yolu:** `DeviceComponents/Commands/CreateDeviceComponent/CreateDeviceComponent.cs`

**Açıklama:** Bir device'a yeni bir component eklemek için kullanılan command'dir. Device'ın parçası olan component'leri (batarya, inverter vb.) oluşturur.

**Parametreler:**
- `DeviceId` (Guid): Component'in ekleneceği device'ın ID'si
- `DeviceType` (DeviceType): Component tipi (Battery, Inverter, PV vb.)
- `Name` (string): Component adı
- `Manufacturer` (string?): Üretici bilgisi
- `ModelNumber` (string?): Model numarası
- `SerialNumber` (string?): Seri numarası
- `Specifications` (JsonDocument): Component özellikleri (JSON formatında)
- `WarrantyPeriod` (int?): Garanti süresi
- `WarrantyUnit` (WarrantyUnit?): Garanti birimi (Day, Month, Year)
- `WarrantyStartDate` (DateTime?): Garanti başlangıç tarihi

**Return Type:** `Guid` (Oluşturulan component'in ID'si)

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Bir device oluşturulurken veya sonradan component eklenirken kullanılır.

---

#### UpdateDeviceComponent
**Dosya Yolu:** `DeviceComponents/Commands/UpdateDeviceComponent/UpdateDeviceComponent.cs`

**Açıklama:** Mevcut bir device component'ini güncellemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Güncellenecek component'in ID'si
- Component bilgileri (CreateDeviceComponent ile aynı alanlar)

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Device component bilgileri değiştirildiğinde kullanılır.

---

#### DeleteDeviceComponent
**Dosya Yolu:** `DeviceComponents/Commands/DeleteDeviceComponent/DeleteDeviceComponent.cs`

**Açıklama:** Bir device component'ini silmek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Silinecek component'in ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Artık kullanılmayan bir component'i sistemden kaldırmak için kullanılır.

---

### Queries

#### GetDeviceComponentById
**Dosya Yolu:** `DeviceComponents/Queries/GetDeviceComponentById/GetDeviceComponentById.cs`

**Açıklama:** ID'ye göre tek bir device component'ini getiren query'dir.

**Parametreler:**
- `Id` (Guid): Component ID'si

**Return Type:** `DeviceComponentDto?`

**Kullanım Senaryosu:** Component detay sayfasında kullanılır.

---

#### GetDeviceComponentsByDeviceId
**Dosya Yolu:** `DeviceComponents/Queries/GetDeviceComponentsByDeviceId/GetDeviceComponentsByDeviceId.cs`

**Açıklama:** Belirli bir device'a ait tüm component'leri getiren query'dir.

**Parametreler:**
- `DeviceId` (Guid): Device ID'si
- `IsDeleted` (bool?): Silinen component'leri de getirmek için opsiyonel parametre

**Return Type:** `List<DeviceComponentDto>`

**Kullanım Senaryosu:** Device detay sayfasında, o device'a ait tüm component'leri listelemek için kullanılır.

---

## 📡 DeviceData

### Commands

#### CreateDeviceData
**Dosya Yolu:** `DeviceData/Commands/CreateDeviceData/CreateDeviceData.cs`

**Açıklama:** Device'dan gelen genel veriyi kaydetmek için kullanılan command'dir. JSON formatında esnek veri saklamaya olanak tanır.

**Parametreler:**
- `DataType` (string): Veri tipi (örneğin "LiveData", "AlarmData" vb.)
- `DeviceId` (Guid): Veriyi gönderen device'ın ID'si
- `RequestDate` (DateTime): Veri tarihi
- `Data` (JsonDocument): Veri içeriği (JSON formatında)

**Return Type:** `int` (Oluşturulan kaydın ID'si)

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Device'dan gelen her türlü veriyi kaydetmek için kullanılır. Esnek bir yapı sunduğu için farklı veri tipleri için kullanılabilir.

---

#### CreateDeviceEnergyData
**Dosya Yolu:** `DeviceData/Commands/CreateDeviceEnergyData/CreateDeviceEnergyData.cs`

**Açıklama:** Device'dan gelen enerji verilerini kaydetmek için kullanılan command'dir. Günlük ve toplam kümülatif enerji verilerini (Today/Total formatında) kaydeder ve hava durumu bilgisini de entegre eder.

**Parametreler:**
- `DataType` (string): Veri tipi - "DailyCumulativeEnergyData" veya "TotalCumulativeEnergyData"
- `DeviceId` (Guid): Device ID'si
- `DataDateTime` (DateTime): Veri tarihi ve saati
- `PvEnergyKWhToday/Total`: PV (güneş paneli) enerjisi
- `GridImportEnergyKWhToday/Total`: Şebekeden alınan enerji
- `GridExportEnergyKWhToday/Total`: Şebekeye verilen enerji
- `GeneratorEnergyKWhToday/Total`: Jeneratör enerjisi
- `BatteryDischargeEnergyKWhToday/Total`: Bataryadan deşarj edilen enerji
- `BatteryChargeEnergyKWhToday/Total`: Bataryaya şarj edilen enerji
- `LoadEnergyKWhToday/Total`: Yük tüketim enerjisi

**Return Type:** `int` (Oluşturulan kaydın ID'si)

**Özellikler:**
- Aynı device, datetime ve data type için mevcut kayıt varsa günceller (upsert mantığı)
- Site koordinatları varsa hava durumu bilgisini WeatherService'den çeker ve kayda ekler
- Device'a bağlı Site bilgisini otomatik olarak bulur

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Device'dan gelen enerji verilerini kaydetmek için kullanılır. Dashboard'daki enerji grafikleri için veri sağlar.

---

#### BulkCreateDeviceData
**Dosya Yolu:** `DeviceData/Commands/BulkCreateDeviceData/BulkCreateDeviceData.cs`

**Açıklama:** Birden fazla device verisini toplu olarak kaydetmek için kullanılan command'dir. Performans için batch insert işlemi yapar.

**Parametreler:**
- `DeviceDataList` (List<CreateDeviceDataCommand>): Kaydedilecek veri listesi

**Return Type:** `int` (Kaydedilen kayıt sayısı)

**Kullanım Senaryosu:** Device'dan toplu veri geldiğinde kullanılır.

---

#### SaveDeviceData
**Dosya Yolu:** `DeviceData/Commands/SaveDeviceData/SaveDeviceData.cs`

**Açıklama:** Device verisini kaydetmek için genel amaçlı command'dir. CreateDeviceData'nın alternatifi olarak kullanılabilir.

**Kullanım Senaryosu:** Farklı kaynaklardan gelen device verilerini kaydetmek için kullanılır.

---

### Queries

#### GetDeviceData
**Dosya Yolu:** `DeviceData/Queries/GetDeviceData/GetDeviceData.cs`

**Açıklama:** Device'a ait verileri getiren query'dir. Filtreleme ve pagination desteği vardır.

**Parametreler:**
- `DeviceId` (Guid): Device ID'si
- `DataType` (string?): Veri tipi filtresi (opsiyonel)
- `StartDate` (DateTime?): Başlangıç tarihi (opsiyonel)
- `EndDate` (DateTime?): Bitiş tarihi (opsiyonel)
- `PageNumber` (int): Sayfa numarası
- `PageSize` (int): Sayfa boyutu

**Return Type:** `PaginatedList<DeviceDataDto>`

**Kullanım Senaryosu:** Device verilerini listelemek için kullanılır.

---

## 🔔 DeviceNotifications

### Commands

#### CreateDeviceNotification
**Dosya Yolu:** `DeviceNotifications/Commands/CreateDeviceNotification/CreateDeviceNotification.cs`

**Açıklama:** Device'dan gelen bildirimleri oluşturmak için kullanılan command'dir. Device tarafından gönderilen alarm, uyarı veya bilgi mesajlarını sisteme kaydeder.

**Parametreler:**
- `DeviceId` (Guid): Bildirim gönderen device'ın ID'si
- `NotificationTypeCode` (string): Bildirim tipi kodu (DeviceNotificationType tablosundan)
- `Timestamp` (DateTimeOffset): Bildirim zamanı
- `AdditionalData` (string): Ek veri (JSON string olarak)

**Return Type:** `DeviceNotificationDto` (Oluşturulan bildirimin DTO'su)

**İşlevi:**
- Device'ı ve ilişkili Setup, Site bilgilerini getirir
- NotificationType'ı koduna göre bulur
- DeviceNotification kaydı oluşturur
- Site'a bağlı olarak kaydedilir

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Device'dan alarm, uyarı veya bilgi mesajları geldiğinde kullanılır.

---

#### DeleteDeviceNotification
**Dosya Yolu:** `DeviceNotifications/Commands/DeleteDeviceNotification/DeleteDeviceNotification.cs`

**Açıklama:** Bir device notification'ı silmek için kullanılan command'dir. Soft delete yapar (IsDeleted = true).

**Parametreler:**
- `Id` (Guid): Silinecek bildirimin ID'si

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Kullanıcı bir bildirimi silmek istediğinde kullanılır.

---

#### PermanentDeleteDeviceNotification
**Dosya Yolu:** `DeviceNotifications/Commands/PermanentDeleteDeviceNotification/PermanentDeleteDeviceNotification.cs`

**Açıklama:** Bir device notification'ı kalıcı olarak silmek için kullanılan command'dir. Hard delete yapar (veritabanından tamamen siler).

**Parametreler:**
- `Id` (Guid): Kalıcı olarak silinecek bildirimin ID'si

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Çöp kutusundan kalıcı olarak silmek için kullanılır.

---

#### RestoreDeviceNotification
**Dosya Yolu:** `DeviceNotifications/Commands/RestoreDeviceNotification/RestoreDeviceNotification.cs`

**Açıklama:** Silinmiş bir device notification'ı geri yüklemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Geri yüklenecek bildirimin ID'si

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Yanlışlıkla silinen bildirimi geri getirmek için kullanılır.

---

#### MarkNotificationAsRead
**Dosya Yolu:** `DeviceNotifications/Commands/MarkNotificationAsRead/MarkNotificationAsRead.cs`

**Açıklama:** Bir device notification'ı okundu olarak işaretlemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Okundu olarak işaretlenecek bildirimin ID'si

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Kullanıcı bir bildirimi okuduğunda kullanılır.

---

#### MarkAllSiteNotificationsAsRead
**Dosya Yolu:** `DeviceNotifications/Commands/MarkAllSiteNotificationsAsRead/MarkAllSiteNotificationsAsRead.cs`

**Açıklama:** Bir site'a ait tüm device notification'larını toplu olarak okundu olarak işaretlemek için kullanılan command'dir.

**Parametreler:**
- `SiteId` (Guid): Site ID'si

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Kullanıcı bir site'a ait tüm bildirimleri toplu olarak okundu yapmak istediğinde kullanılır.

---

### Queries

#### GetDeviceNotifications
**Dosya Yolu:** `DeviceNotifications/Queries/GetDeviceNotifications/GetDeviceNotifications.cs`

**Açıklama:** Bir site'a ait device notification'ları getiren query'dir. Filtreleme ve pagination desteği vardır.

**Parametreler:**
- `SiteId` (Guid): Site ID'si
- `PageNumber` (int): Sayfa numarası
- `PageSize` (int): Sayfa boyutu
- `IsToday` (bool?): Sadece bugünkü bildirimleri getirmek için
- `IsProcessed` (bool?): Okundu/okunmadı durumuna göre filtreleme
- `Category` (string?): Bildirim kategorisine göre filtreleme (INFO, WARNING, ERROR)
- `IncludeDeleted` (bool?): Silinen bildirimleri de dahil etmek için

**Return Type:** `PaginatedList<DeviceNotificationDto>`

**Özellikler:**
- Site erişim kontrolü yapar (sahip, paylaşım veya admin)
- Çoklu filtreleme seçenekleri sunar

**Kullanım Senaryosu:** Site bildirimlerini listelemek için kullanılır.

---

#### GetDeletedDeviceNotifications
**Dosya Yolu:** `DeviceNotifications/Queries/GetDeletedDeviceNotifications/GetDeletedDeviceNotifications.cs`

**Açıklama:** Silinmiş device notification'ları getiren query'dir. Çöp kutusu görünümü için kullanılır.

**Parametreler:**
- `SiteId` (Guid): Site ID'si
- `PageNumber` (int): Sayfa numarası
- `PageSize` (int): Sayfa boyutu

**Return Type:** `PaginatedList<DeviceNotificationDto>`

**Kullanım Senaryosu:** Çöp kutusunda silinmiş bildirimleri göstermek için kullanılır.

---

#### GetDeviceNotificationTypes
**Dosya Yolu:** `DeviceNotifications/Queries/GetDeviceNotificationTypes/GetDeviceNotificationTypes.cs`

**Açıklama:** Sistemdeki tüm device notification tiplerini getiren query'dir.

**Return Type:** `List<DeviceNotificationTypeDto>`

**Kullanım Senaryosu:** Bildirim tiplerini listelemek veya bildirim oluştururken seçenekleri göstermek için kullanılır.

---

## 📱 Devices

### Commands

#### CreateDevice
**Dosya Yolu:** `Devices/Commands/CreateDevice/CreateDevice.cs`

**Açıklama:** Yeni bir device oluşturmak için kullanılan command'dir. Device'ın temel bilgilerini ve component'lerini içerir.

**Parametreler:**
- `Manufacturer` (string): Üretici bilgisi
- `ModelNumber` (string?): Model numarası
- `SerialNumber` (string?): Seri numarası
- `TypeNumber` (string?): Tip numarası
- `QRCode` (string?): QR kod
- `Specifications` (JsonDocument): Device özellikleri (JSON formatında)
- `InstallationDate` (DateTime?): Kurulum tarihi
- `CalibrationDate` (DateTime?): Kalibrasyon tarihi
- `NextCheckoutDate` (DateTime?): Sonraki kontrol tarihi
- `WarrantyPeriod` (int?): Garanti süresi
- `WarrantyUnit` (WarrantyUnit?): Garanti birimi
- `WarrantyStartDate` (DateTime?): Garanti başlangıç tarihi
- `DeviceComponents` (List<CreateDeviceComponentCommand>): Device component'leri

**Return Type:** `Guid` (Oluşturulan device'ın ID'si)

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Yeni bir device kaydı oluştururken kullanılır. Device ve component'ler birlikte oluşturulur.

---

#### UpdateDevice
**Dosya Yolu:** `Devices/Commands/UpdateDevice/UpdateDevice.cs`

**Açıklama:** Mevcut bir device'ı güncellemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Güncellenecek device'ın ID'si
- Device bilgileri (CreateDevice ile aynı alanlar, component'ler hariç)

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Device bilgileri değiştirildiğinde kullanılır.

---

#### DeleteDevice
**Dosya Yolu:** `Devices/Commands/DeleteDevice/DeleteDevice.cs`

**Açıklama:** Bir device'ı silmek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Silinecek device'ın ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Artık kullanılmayan bir device'ı sistemden kaldırmak için kullanılır.

---

#### RegisterDevice
**Dosya Yolu:** `Devices/Commands/RegisterDevice/DeviceLogin.cs`

**Açıklama:** Device'ın sisteme giriş yapması için kullanılan command'dir. Device'ın SerialNumber veya QRCode bilgisi ile kimlik doğrulaması yapar ve JWT token döner.

**Parametreler:**
- `SerialNumber` (string): Device seri numarası
- `QRCode` (string?): QR kod (opsiyonel, SerialNumber ile birlikte veya yerine kullanılabilir)

**Return Type:** `DeviceLoginResult`

**İşlevi:**
- SerialNumber veya QRCode ile device'ı bulur
- Device'ın aktif olup olmadığını kontrol eder
- Device için JWT token oluşturur (DeviceJwtService kullanarak)
- Access token, refresh token ve expiration bilgilerini döner

**Kullanım Senaryosu:** Device'ların API'ye erişebilmesi için authentication sağlar. Device simulator veya gerçek device'lar tarafından çağrılır.

---

### Queries

#### GetDeviceById
**Dosya Yolu:** `Devices/Queries/GetDeviceById/GetDeviceById.cs`

**Açıklama:** ID'ye göre tek bir device getiren query'dir.

**Parametreler:**
- `Id` (Guid): Device ID'si

**Return Type:** `DeviceDto?`

**Kullanım Senaryosu:** Device detay sayfasında kullanılır.

---

#### GetDevices
**Dosya Yolu:** `Devices/Queries/GetDevices/GetDevices.cs`

**Açıklama:** Tüm device'ları getiren query'dir. Filtreleme ve pagination desteği vardır.

**Parametreler:**
- `Manufacturer` (string?): Üretici filtresi
- `IsDeleted` (bool?): Silinen device'ları da getirmek için
- `PageNumber` (int): Sayfa numarası
- `PageSize` (int): Sayfa boyutu

**Return Type:** `PaginatedList<DeviceDto>`

**Kullanım Senaryosu:** Device listesi sayfasında kullanılır.

---

#### GetDevicesWithComponents
**Dosya Yolu:** `Devices/Queries/GetDevicesWithComponents/GetDevicesWithComponents.cs`

**Açıklama:** Device'ları component'leriyle birlikte getiren query'dir.

**Parametreler:**
- `Manufacturer` (string?): Üretici filtresi
- `IsDeleted` (bool?): Silinen device'ları da getirmek için

**Return Type:** `List<DeviceWithComponentsDto>`

**Kullanım Senaryosu:** Device ve component'lerini birlikte görüntülemek için kullanılır.

---

#### CheckDeviceAccess
**Dosya Yolu:** `Devices/Queries/CheckDeviceAccess/CheckDeviceAccess.cs`

**Açıklama:** Kullanıcının bir device'a erişim yetkisi olup olmadığını kontrol eden query'dir.

**Parametreler:**
- `DeviceId` (Guid): Device ID'si

**Return Type:** `bool` (Erişim varsa true, yoksa false)

**İşlevi:**
- Admin kullanıcılar tüm device'lara erişebilir
- Normal kullanıcılar sadece kendi site'larına ait device'lara veya paylaşılan site'ların device'larına erişebilir

**Kullanım Senaryosu:** Device'a erişim yetkisi kontrolü yapılırken kullanılır.

---

#### GetDeviceWithData
**Dosya Yolu:** `Devices/Queries/GetDeviceWithData/GetDeviceWithDataQuery.cs`

**Açıklama:** Device'ı verileriyle birlikte getiren query'dir. Device bilgileri ve son verileri içerir.

**Parametreler:**
- `DeviceId` (Guid): Device ID'si

**Return Type:** `DeviceWithDataResponseDto`

**Kullanım Senaryosu:** Device detay sayfasında, device ve son verilerini birlikte göstermek için kullanılır.

---

#### GetComponentLiveData
**Dosya Yolu:** `Devices/Queries/GetComponentLiveData/GetComponentLiveDataQuery.cs`

**Açıklama:** Bir device component'ine ait canlı (live) verileri getiren query'dir.

**Parametreler:**
- `DeviceId` (Guid): Device ID'si
- `ComponentId` (Guid): Component ID'si

**Return Type:** `GetComponentLiveDataResponse`

**Kullanım Senaryosu:** Component'in canlı verilerini görmek için kullanılır.

---

## ✉️ Invitations

### Commands

#### CreateInvitation
**Dosya Yolu:** `Invitations/Commands/CreateInvitation/CreateInvitation.cs`

**Açıklama:** Bir site'ı başka bir kullanıcıyla paylaşmak için davetiye oluşturan command'dir. Email gönderimi yapar ve 6 haneli token oluşturur.

**Parametreler:**
- `Email` (string): Davet edilecek kullanıcının email adresi
- `SiteId` (Guid): Paylaşılacak site'ın ID'si

**Return Type:** `Guid` (Oluşturulan invitation'ın ID'si)

**İşlevi:**
- Site sahibi veya paylaşım yetkisi olan kullanıcı kontrolü yapar
- Email ile kullanıcıyı arar (kullanıcı sistemde yoksa bile davetiye oluşturulur)
- Pending durumda bir davetiye daha varsa hata verir
- 6 haneli benzersiz token oluşturur
- Invitation ve Notification kaydı oluşturur
- Email gönderir (InvitationEmailService kullanarak)

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Site sahibi, site'ını başka bir kullanıcıyla paylaşmak istediğinde kullanılır.

---

#### AcceptInvitation
**Dosya Yolu:** `Invitations/Commands/AcceptInvitation/AcceptInvitation.cs`

**Açıklama:** Bir davetiyeyi kabul etmek için kullanılan command'dir. Token ile davetiyeyi bulur ve kullanıcıyı site'a ekler.

**Parametreler:**
- `Token` (string): Davetiye token'ı (6 haneli sayı)

**Return Type:** `Unit`

**İşlevi:**
- Token ile pending durumdaki davetiyeyi bulur
- Kullanıcının email veya ID'si ile eşleşme kontrolü yapar
- Invitation'ı Accepted durumuna günceller
- UserSite kaydı oluşturur (Guest rolü ile)
- Notification'ı günceller

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Kullanıcı, kendisine gelen davetiyeyi kabul ettiğinde kullanılır.

---

#### DeclineInvitation
**Dosya Yolu:** `Invitations/Commands/DeclineInvitation/DeclineInvitation.cs`

**Açıklama:** Bir davetiyeyi reddetmek için kullanılan command'dir.

**Parametreler:**
- `Token` (string): Davetiye token'ı

**Return Type:** `Unit`

**İşlevi:**
- Token ile pending durumdaki davetiyeyi bulur
- Invitation'ı Declined durumuna günceller

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Kullanıcı, kendisine gelen davetiyeyi reddetmek istediğinde kullanılır.

---

#### RemoveUserFromSite
**Dosya Yolu:** `Invitations/Commands/RemoveUserFromSite/RemoveUserFromSite.cs`

**Açıklama:** Bir kullanıcıyı site'dan çıkarmak için kullanılan command'dir. Site sahibi tarafından kullanılır.

**Parametreler:**
- `InvitationId` (Guid): Invitation ID'si

**Return Type:** `Unit`

**İşlevi:**
- Invitation'ı bulur
- Site sahipliği kontrolü yapar
- UserSite kaydını siler (eğer varsa)
- Invitation'ı siler

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Site sahibi, bir kullanıcıyı site'dan çıkarmak istediğinde kullanılır.

---

#### MarkNotificationAsRead
**Dosya Yolu:** `Invitations/Commands/MarkNotificationAsRead/MarkNotificationAsRead.cs`

**Açıklama:** Davetiye ile ilgili bir notification'ı okundu olarak işaretlemek için kullanılan command'dir.

**Parametreler:**
- `NotificationId` (Guid): Notification ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Kullanıcı, davetiye bildirimini okuduğunda kullanılır.

---

### Queries

#### GetInvitationsBySite
**Dosya Yolu:** `Invitations/Queries/GetInvitationsBySite/GetInvitationsBySite.cs`

**Açıklama:** Bir site'a ait tüm davetiyeleri getiren query'dir. Kullanıcılı ve kullanıcısız davetiyeleri ayrı ayrı gruplar.

**Parametreler:**
- `SiteId` (Guid): Site ID'si

**Return Type:** `InvitationsVm` (WithUser ve WithoutUser listeleri içerir)

**İşlevi:**
- Site erişim kontrolü yapar
- Site'a ait tüm davetiyeleri getirir
- Davetiyeleri kullanıcılı ve kullanıcısız olarak gruplar
- Davet eden kullanıcının adını getirir

**Kullanım Senaryosu:** Site yönetim sayfasında, site'a ait davetiyeleri görüntülemek için kullanılır.

---

#### GetUserHomeSites
**Dosya Yolu:** `Invitations/Queries/GetUserHomeSites/GetUserHomeSites.cs`

**Açıklama:** Kullanıcının sahip olduğu ve paylaşıldığı site'ları getiren query'dir.

**Return Type:** `UserHomeSitesVm` (MyHomes ve SharedHomes listeleri içerir)

**İşlevi:**
- Kullanıcının sahip olduğu site'ları getirir (Owner rolü)
- Kullanıcıyla paylaşılan site'ları getirir (Guest rolü)

**Kullanım Senaryosu:** Kullanıcı ana sayfasında, ev listesini göstermek için kullanılır.

---

#### GetUserNotifications
**Dosya Yolu:** `Invitations/Queries/GetUserNotifications/GetUserNotifications.cs`

**Açıklama:** Kullanıcının tüm notification'larını getiren query'dir.

**Return Type:** `List<NotificationDto>`

**İşlevi:**
- Kullanıcıya ait tüm notification'ları getirir
- Oluşturulma tarihine göre sıralar (en yeni önce)

**Kullanım Senaryosu:** Bildirimler sayfasında kullanılır.

---

## 🌍 Locations

### Queries

#### GetCountries
**Dosya Yolu:** `Locations/Queries/GetCountries/GetCountries.cs`

**Açıklama:** Sistemdeki tüm aktif ülkeleri getiren query'dir.

**Return Type:** `List<CountryDto>`

**İşlevi:**
- Sadece aktif ülkeleri getirir
- İsme göre sıralar

**Kullanım Senaryosu:** Site oluştururken veya güncellerken ülke seçimi için kullanılır.

---

#### GetCitiesByCountry
**Dosya Yolu:** `Locations/Queries/GetCitiesByCountry/GetCitiesByCountry.cs`

**Açıklama:** Belirli bir ülkeye ait şehirleri getiren query'dir.

**Parametreler:**
- `CountryId` (Guid): Ülke ID'si

**Return Type:** `List<CityDto>`

**Kullanım Senaryosu:** Site oluştururken veya güncellerken şehir seçimi için kullanılır.

---

## 📱 Mobile

### Queries

#### GetMobileDashboard
**Dosya Yolu:** `Mobile/Queries/GetMobileDashboard/GetMobileDashboard.cs`

**Açıklama:** Mobil uygulama için dashboard verilerini getiren query'dir. Device, component'ler, sistem durumu ve alert'leri içerir.

**Return Type:** `MobileDashboardDto`

**İşlevi:**
- Kullanıcının ana site'ını bulur
- Site'ın setup ve device bilgilerini getirir
- Device component'lerini ve external device'ları getirir
- Sistem durumu bilgilerini oluşturur (şimdilik mock data, gelecekte DeviceSignal'dan gelecek)
- Active alert'leri getirir (gelecekte Alert entity'sinden gelecek)

**Özellikler:**
- Warranty hesaplaması yapar
- Device overview bilgileri oluşturur
- System status mock data ile oluşturulur

**Kullanım Senaryosu:** Mobil uygulamanın ana dashboard sayfasında kullanılır.

---

## ⚙️ Parameters

### Commands

#### CreateParameter
**Dosya Yolu:** `Parameters/Commands/CreateParameter/CreateParameter.cs`

**Açıklama:** Yeni bir parametre oluşturmak için kullanılan command'dir. Parametreler, device ayarlarını tanımlayan yapılandırma öğeleridir.

**Parametreler:**
- `ParameterGroupId` (Guid): Parametrenin ait olduğu grup ID'si
- `Name` (string): Parametre adı
- `Description` (string): Açıklama
- `ElementType` (string): Element tipi (input, select, checkbox vb.)
- `RoleType` (string): Rol tipi
- `Unit` (string): Birim (W, A, V vb.)
- `DefaultValue` (string): Varsayılan değer
- `SortOrder` (int): Sıralama düzeni

**Return Type:** `Guid` (Oluşturulan parametrenin ID'si)

**İşlevi:**
- ParameterGroupId'nin var olduğunu kontrol eder
- Yeni parametre kaydı oluşturur

**Kullanım Senaryosu:** Device ayarlarını yönetmek için parametre oluştururken kullanılır.

---

#### UpdateParameter
**Dosya Yolu:** `Parameters/Commands/UpdateParameter/UpdateParameter.cs`

**Açıklama:** Mevcut bir parametreyi güncellemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Güncellenecek parametrenin ID'si
- Parametre bilgileri (CreateParameter ile aynı alanlar)

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Parametre bilgileri değiştirildiğinde kullanılır.

---

#### DeleteParameter
**Dosya Yolu:** `Parameters/Commands/DeleteParameter/DeleteParameter.cs`

**Açıklama:** Bir parametreyi silmek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Silinecek parametrenin ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Artık kullanılmayan bir parametreyi sistemden kaldırmak için kullanılır.

---

### Queries

#### GetParameters
**Dosya Yolu:** `Parameters/Queries/GetParameters/GetParameters.cs`

**Açıklama:** Parametreleri getiren query'dir. Grup ID'sine göre filtreleme yapabilir.

**Parametreler:**
- `GroupId` (Guid?): Grup ID'si (opsiyonel, belirtilirse sadece o gruba ait parametreleri getirir)

**Return Type:** `ParametersVm` (Parametre listesi içerir)

**Kullanım Senaryosu:** Parametre listesi sayfasında veya parametre seçimi için kullanılır.

---

#### GetParameter
**Dosya Yolu:** `Parameters/Queries/GetParameter/GetParameter.cs`

**Açıklama:** ID'ye göre tek bir parametre getiren query'dir.

**Parametreler:**
- `Id` (Guid): Parametre ID'si

**Return Type:** `ParameterDto?`

**Kullanım Senaryosu:** Parametre detay sayfasında kullanılır.

---

## 📦 ParameterGroups

### Commands

#### CreateParameterGroup
**Dosya Yolu:** `ParameterGroups/Commands/CreateParameterGroup/CreateParameterGroup.cs`

**Açıklama:** Yeni bir parametre grubu oluşturmak için kullanılan command'dir. Parametreler gruplar halinde organize edilir.

**Parametreler:**
- `Name` (string): Grup adı
- `Description` (string?): Açıklama
- `SortOrder` (int): Sıralama düzeni

**Return Type:** `Guid` (Oluşturulan grubun ID'si)

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Parametreleri organize etmek için grup oluştururken kullanılır.

---

#### UpdateParameterGroup
**Dosya Yolu:** `ParameterGroups/Commands/UpdateParameterGroup/UpdateParameterGroup.cs`

**Açıklama:** Mevcut bir parametre grubunu güncellemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Güncellenecek grubun ID'si
- Grup bilgileri (CreateParameterGroup ile aynı alanlar)

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Parametre grubu bilgileri değiştirildiğinde kullanılır.

---

#### DeleteParameterGroup
**Dosya Yolu:** `ParameterGroups/Commands/DeleteParameterGroup/DeleteParameterGroup.cs`

**Açıklama:** Bir parametre grubunu silmek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Silinecek grubun ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Artık kullanılmayan bir parametre grubunu sistemden kaldırmak için kullanılır.

---

### Queries

#### GetParameterGroups
**Dosya Yolu:** `ParameterGroups/Queries/GetParameterGroups/GetParameterGroups.cs`

**Açıklama:** Tüm parametre gruplarını getiren query'dir.

**Return Type:** `ParameterGroupsVm` (Parametre grubu listesi içerir)

**Kullanım Senaryosu:** Parametre grubu listesi sayfasında kullanılır.

---

#### GetParameterGroup
**Dosya Yolu:** `ParameterGroups/Queries/GetParameterGroup/GetParameterGroup.cs`

**Açıklama:** ID'ye göre tek bir parametre grubunu getiren query'dir.

**Parametreler:**
- `Id` (Guid): Grup ID'si

**Return Type:** `ParameterGroupDto?`

**Kullanım Senaryosu:** Parametre grubu detay sayfasında kullanılır.

---

## 🎛️ ParameterValueSettingSchemes

### Commands

#### CreateParameterValueSettingScheme
**Dosya Yolu:** `ParameterValueSettingSchemes/Commands/CreateParameterValueSettingScheme/CreateParameterValueSettingScheme.cs`

**Açıklama:** Yeni bir parametre değer ayar şeması oluşturmak için kullanılan command'dir. Bu şemalar, belirli device model'leri için varsayılan parametre değerlerini tanımlar.

**Parametreler:**
- `ParameterId` (Guid): Parametre ID'si
- `ParameterGroupId` (Guid): Parametre grup ID'si
- `Value` (string): Parametre değeri
- `SortOrder` (int): Sıralama düzeni
- `ModelNumber` (string): Device model numarası

**Return Type:** `Guid` (Oluşturulan şemanın ID'si)

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Yeni bir device modeli için varsayılan ayar değerleri tanımlarken kullanılır.

---

#### UpdateParameterValueSettingScheme
**Dosya Yolu:** `ParameterValueSettingSchemes/Commands/UpdateParameterValueSettingScheme/UpdateParameterValueSettingScheme.cs`

**Açıklama:** Mevcut bir parametre değer ayar şemasını güncellemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Güncellenecek şemanın ID'si
- Şema bilgileri (CreateParameterValueSettingScheme ile aynı alanlar)

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Parametre değer ayar şeması değiştirildiğinde kullanılır.

---

#### DeleteParameterValueSettingScheme
**Dosya Yolu:** `ParameterValueSettingSchemes/Commands/DeleteParameterValueSettingScheme/DeleteParameterValueSettingScheme.cs`

**Açıklama:** Bir parametre değer ayar şemasını silmek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Silinecek şemanın ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Artık kullanılmayan bir şemayı sistemden kaldırmak için kullanılır.

---

### Queries

#### GetParameterValueSettingSchemes
**Dosya Yolu:** `ParameterValueSettingSchemes/Queries/GetParameterValueSettingSchemes/GetParameterValueSettingSchemes.cs`

**Açıklama:** Parametre değer ayar şemalarını getiren query'dir.

**Return Type:** `ParameterValueSettingSchemesVm` (Şema listesi içerir)

**Kullanım Senaryosu:** Parametre değer ayar şemalarını listelemek için kullanılır.

---

#### GetParameterValueSettingSchemesTree
**Dosya Yolu:** `ParameterValueSettingSchemes/Queries/GetParameterValueSettingSchemesTree/GetParameterValueSettingSchemesTree.cs`

**Açıklama:** Parametre değer ayar şemalarını ağaç yapısında getiren query'dir. Grup ve parametre ilişkilerini hiyerarşik olarak döner.

**Return Type:** `ParameterValueSettingSchemesTreeDto`

**Kullanım Senaryosu:** Şemaları ağaç yapısında görüntülemek için kullanılır.

---

## 👥 RoleManagement

### Commands

#### AssignRoleToUser
**Dosya Yolu:** `RoleManagement/Commands/AssignRoleToUser/AssignRoleToUser.cs`

**Açıklama:** Bir kullanıcıya rol atamak için kullanılan command'dir.

**Parametreler:**
- `UserId` (string): Kullanıcı ID'si
- `RoleName` (string): Atanacak rol adı (örneğin "Administrator", "User")

**Return Type:** `Result` (Başarı durumu)

**İşlevi:**
- IdentityService üzerinden kullanıcıya rol atar

**Kullanım Senaryosu:** Kullanıcı yetkilendirme işlemlerinde kullanılır.

---

#### RemoveRoleFromUser
**Dosya Yolu:** `RoleManagement/Commands/RemoveRoleFromUser/RemoveRoleFromUser.cs`

**Açıklama:** Bir kullanıcıdan rol kaldırmak için kullanılan command'dir.

**Parametreler:**
- `UserId` (string): Kullanıcı ID'si
- `RoleName` (string): Kaldırılacak rol adı

**Return Type:** `Result`

**Kullanım Senaryosu:** Kullanıcının yetkilerini azaltmak için kullanılır.

---

### Queries

#### GetRoles
**Dosya Yolu:** `RoleManagement/Queries/GetRoles/GetRoles.cs`

**Açıklama:** Sistemdeki tüm rolleri getiren query'dir.

**Return Type:** `RolesVm` (Rol listesi içerir)

**Kullanım Senaryosu:** Rol listesi sayfasında veya rol seçimi için kullanılır.

---

#### GetUserRoles
**Dosya Yolu:** `RoleManagement/Queries/GetUserRoles/GetUserRoles.cs`

**Açıklama:** Bir kullanıcının sahip olduğu rolleri getiren query'dir.

**Parametreler:**
- `UserId` (string): Kullanıcı ID'si

**Return Type:** `UserRolesVm`

**Kullanım Senaryosu:** Kullanıcı yetkilerini görüntülemek için kullanılır.

---

## 🔧 SetupDeviceParameterValues

### Commands

#### CreateSetupDeviceParameterValue
**Dosya Yolu:** `SetupDeviceParameterValues/Commands/CreateSetupDeviceParameterValue/CreateSetupDeviceParameterValue.cs`

**Açıklama:** Bir setup device'a ait parametre değeri oluşturmak için kullanılan command'dir. Bu değerler, device'ın gerçek çalışma ayarlarını temsil eder.

**Parametreler:**
- `SetupDeviceId` (Guid): Setup device ID'si
- `ParameterId` (Guid): Parametre ID'si
- `ParameterGroupId` (Guid): Parametre grup ID'si
- `Value` (string): Parametre değeri
- `SortOrder` (int): Sıralama düzeni
- `ModelNumber` (string): Device model numarası

**Return Type:** `Guid` (Oluşturulan değerin ID'si)

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Setup oluşturulurken veya device ayarları değiştirildiğinde kullanılır.

---

#### UpdateSetupDeviceParameterValue
**Dosya Yolu:** `SetupDeviceParameterValues/Commands/UpdateSetupDeviceParameterValue/UpdateSetupDeviceParameterValue.cs`

**Açıklama:** Mevcut bir setup device parametre değerini güncellemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Güncellenecek değerin ID'si
- Değer bilgileri (CreateSetupDeviceParameterValue ile aynı alanlar)

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Device ayarları değiştirildiğinde kullanılır.

---

#### DeleteSetupDeviceParameterValue
**Dosya Yolu:** `SetupDeviceParameterValues/Commands/DeleteSetupDeviceParameterValue/DeleteSetupDeviceParameterValue.cs`

**Açıklama:** Bir setup device parametre değerini silmek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Silinecek değerin ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Artık kullanılmayan bir parametre değerini kaldırmak için kullanılır.

---

### Queries

#### GetSetupDeviceParameterValues
**Dosya Yolu:** `SetupDeviceParameterValues/Queries/GetSetupDeviceParameterValues/GetSetupDeviceParameterValues.cs`

**Açıklama:** Bir setup device'a ait parametre değerlerini getiren query'dir.

**Parametreler:**
- `SetupDeviceId` (Guid): Setup device ID'si

**Return Type:** `SetupDeviceParameterValuesVm` (Parametre değeri listesi içerir)

**Kullanım Senaryosu:** Device ayarları sayfasında kullanılır.

---

#### GetSetupDeviceParameterValuesTree
**Dosya Yolu:** `SetupDeviceParameterValues/Queries/GetSetupDeviceParameterValuesTree/GetSetupDeviceParameterValuesTree.cs`

**Açıklama:** Setup device parametre değerlerini ağaç yapısında getiren query'dir. Grup ve parametre ilişkilerini hiyerarşik olarak döner.

**Parametreler:**
- `SetupDeviceId` (Guid): Setup device ID'si

**Return Type:** `SetupDeviceParameterValuesTreeDto`

**Kullanım Senaryosu:** Device ayarlarını ağaç yapısında görüntülemek için kullanılır.

---

## 📋 SetupDevices

### Commands

#### CreateSetupDevice
**Dosya Yolu:** `SetupDevices/Commands/CreateSetupDevice/CreateSetupDevice.cs`

**Açıklama:** Bir setup'a device bağlamak için kullanılan command'dir.

**Parametreler:**
- `SetupId` (Guid): Setup ID'si
- `DeviceId` (Guid): Bağlanacak device'ın ID'si

**Return Type:** `Guid` (Oluşturulan setup device'ın ID'si)

**Kullanım Senaryosu:** Setup oluşturulurken veya sonradan device eklenirken kullanılır.

---

## 🏗️ Setups

### Commands

#### CreateSetup
**Dosya Yolu:** `Setups/Commands/CreateSetup/CreateSetup.cs`

**Açıklama:** Yeni bir setup oluşturmak için kullanılan command'dir. Setup, bir site'a device bağlamak için kullanılan yapılandırma birimidir.

**Parametreler:**
- `SiteId` (Guid): Setup'ın bağlanacağı site'ın ID'si
- `Name` (string): Setup adı (varsayılan: "Default Setup")
- `DeviceQRCode` (string?): Device'ı bulmak için QR kod (opsiyonel)
- `DeviceSerialNumber` (string?): Device'ı bulmak için seri numara (opsiyonel)

**Return Type:** `Guid` (Oluşturulan setup'ın ID'si)

**İşlevi:**
- Site sahipliği veya paylaşım yetkisi kontrolü yapar
- Site'ın zaten bir setup'ı varsa hata verir (bir site'ın sadece bir setup'ı olabilir)
- QR Code veya Serial Number ile device'ı bulur
- Device'ın başka bir setup'ta kullanılmadığını kontrol eder
- Setup ve SetupDevice kaydı oluşturur
- Device'ın model numarasına göre varsayılan parametre değerlerini ParameterValueSettingSchemes'den kopyalar

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Site oluşturulduktan sonra, device bağlamak için setup oluştururken kullanılır.

---

#### UpdateSetup
**Dosya Yolu:** `Setups/Commands/UpdateSetup/UpdateSetup.cs`

**Açıklama:** Mevcut bir setup'ı güncellemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Güncellenecek setup'ın ID'si
- `Name` (string): Setup adı

**Return Type:** `Unit`

**Kullanım Senaryosu:** Setup bilgileri değiştirildiğinde kullanılır.

---

#### DeleteSetup
**Dosya Yolu:** `Setups/Commands/DeleteSetup/DeleteSetup.cs`

**Açıklama:** Bir setup'ı silmek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Silinecek setup'ın ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Setup'ı kaldırmak için kullanılır (device bağlantısı da kaldırılır).

---

### Queries

#### GetSetupDevicesByUser
**Dosya Yolu:** `Setups/Queries/GetSetupDevicesByUser/GetSetupDevicesByUser.cs`

**Açıklama:** Kullanıcının erişebildiği setup device bilgilerini getiren query'dir. Device, component'ler ve external device'ları içerir.

**Parametreler:**
- `SiteId` (Guid): Site ID'si

**Return Type:** `UserSetupDeviceDto?`

**İşlevi:**
- Site erişim kontrolü yapar (CheckSiteAccessQuery kullanarak)
- Setup, Device, Component ve External Device bilgilerini getirir

**Kullanım Senaryosu:** Site detay sayfasında, setup ve device bilgilerini göstermek için kullanılır.

---

#### GetSetupsByUser
**Dosya Yolu:** `Setups/Queries/GetSetupsByUser/GetSetupsByUser.cs`

**Açıklama:** Kullanıcının erişebildiği tüm setup'ları getiren query'dir.

**Return Type:** `List<SetupDto>`

**Kullanım Senaryosu:** Kullanıcının tüm setup'larını listelemek için kullanılır.

---

## 🏠 Sites

### Commands

#### CreateSite
**Dosya Yolu:** `Sites/Commands/CreateSite/CreateSite.cs`

**Açıklama:** Yeni bir site (ev/konum) oluşturmak için kullanılan command'dir.

**Parametreler:**
- `Name` (string): Site adı
- `Country` (string?): Ülke
- `City` (string?): Şehir
- `District` (string?): İlçe
- `AddressLine` (string?): Adres satırı
- `Latitude` (decimal?): Enlem
- `Longitude` (decimal?): Boylam
- `Timezone` (string): Zaman dilimi (varsayılan: "Europe/Istanbul")

**Return Type:** `Guid` (Oluşturulan site'ın ID'si)

**İşlevi:**
- Kullanıcıya ait yeni bir site oluşturur
- Koordinat bilgileri hava durumu servisi için kullanılabilir

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Kullanıcı yeni bir ev/konum eklemek istediğinde kullanılır.

---

#### UpdateSite
**Dosya Yolu:** `Sites/Commands/UpdateSite/UpdateSite.cs`

**Açıklama:** Mevcut bir site'ı güncellemek için kullanılan command'dir.

**Parametreler:**
- `Id` (Guid): Güncellenecek site'ın ID'si
- Site bilgileri (CreateSite ile aynı alanlar)

**Return Type:** `Unit`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Site bilgileri değiştirildiğinde kullanılır.

---

#### DeleteSite
**Dosya Yolu:** `Sites/Commands/DeleteSite/DeleteSite.cs`

**Açıklama:** Bir site'ı silmek için kullanılan command'dir. Soft delete yapar.

**Parametreler:**
- `Id` (Guid): Silinecek site'ın ID'si

**Return Type:** `Unit`

**Kullanım Senaryosu:** Artık kullanılmayan bir site'ı kaldırmak için kullanılır.

---

### Queries

#### GetSites
**Dosya Yolu:** `Sites/Queries/GetSites/GetSites.cs`

**Açıklama:** Site'ları getiren query'dir. Filtreleme ve pagination desteği vardır. Admin panelinde kullanılır.

**Parametreler:**
- `UserId` (string?): Kullanıcı ID'si filtresi
- `UserEmail` (string?): Kullanıcı email filtresi
- `UserName` (string?): Kullanıcı adı filtresi
- `PageNumber` (int): Sayfa numarası
- `PageSize` (int): Sayfa boyutu
- `Name` (string?): Site adı filtresi
- `Country` (string?): Ülke filtresi
- `City` (string?): Şehir filtresi
- `AddressLine` (string?): Adres filtresi
- `District` (string?): İlçe filtresi

**Return Type:** `GetSitesResponseDto` (TotalSite, TotalDevice ve paginated site listesi içerir)

**İşlevi:**
- Çoklu filtreleme seçenekleri sunar
- UserService ile kullanıcı arama yapar
- Site'ların device'ı olup olmadığını kontrol eder
- Toplam site ve device sayısını hesaplar

**Kullanım Senaryosu:** Admin panelinde tüm site'ları listelemek için kullanılır.

---

#### GetSitesByUser
**Dosya Yolu:** `Sites/Queries/GetSitesByUser/GetSitesByUser.cs`

**Açıklama:** Kullanıcının sahip olduğu ve paylaşıldığı site'ları güç bilgileriyle birlikte getiren query'dir. Dashboard için kullanılır.

**Return Type:** `SitesDashboardDto` (TotalHomes, TotalDevices, TotalPower ve site listesi içerir)

**İşlevi:**
- Kullanıcının sahip olduğu site'ları getirir
- Kullanıcıyla paylaşılan site'ları getirir
- Her site için güncel enerji verilerini hesaplar
- Toplam güç, ev ve device sayısını hesaplar

**Kullanım Senaryosu:** Kullanıcı dashboard'unda, tüm evleri ve güç bilgilerini göstermek için kullanılır.

---

#### GetSiteById
**Dosya Yolu:** `Sites/Queries/GetSiteById/GetSiteById.cs`

**Açıklama:** ID'ye göre tek bir site getiren query'dir.

**Parametreler:**
- `Id` (Guid): Site ID'si

**Return Type:** `SiteDto?`

**Kullanım Senaryosu:** Site detay sayfasında kullanılır.

---

#### CheckSiteAccess
**Dosya Yolu:** `Sites/Queries/CheckSiteAccess/CheckSiteAccess.cs`

**Açıklama:** Kullanıcının bir site'a erişim yetkisi olup olmadığını kontrol eden query'dir.

**Parametreler:**
- `SiteId` (Guid): Site ID'si

**Return Type:** `bool` (Erişim varsa true, yoksa false)

**İşlevi:**
- Admin kullanıcılar tüm site'lara erişebilir
- Normal kullanıcılar sadece kendi site'larına veya paylaşılan site'lara erişebilir

**Kullanım Senaryosu:** Site'a erişim yetkisi kontrolü yapılırken kullanılır.

---

## ✅ TodoItems

Bu feature, Clean Architecture template'inin bir parçasıdır ve örnek amaçlı kullanılır. Gerçek HEMS iş mantığı için gerekli değildir.

### Commands

- **CreateTodoItem**: Todo item oluşturur
- **UpdateTodoItem**: Todo item günceller
- **DeleteTodoItem**: Todo item siler
- **MarkAsCompleted**: Todo item'ı tamamlandı olarak işaretler

### Queries

- **GetTodos**: Tüm todo'ları getirir
- **GetTodoItemById**: ID'ye göre todo item getirir

---

## 📝 TodoLists

Bu feature, Clean Architecture template'inin bir parçasıdır ve örnek amaçlı kullanılır. Gerçek HEMS iş mantığı için gerekli değildir.

### Commands

- **CreateTodoList**: Todo listesi oluşturur
- **UpdateTodoList**: Todo listesi günceller
- **DeleteTodoList**: Todo listesi siler

### Queries

- **GetTodos**: Tüm todo listelerini getirir
- **GetTodoListById**: ID'ye göre todo listesi getirir

---

## 👤 Users

### Commands

#### UpdateUser
**Dosya Yolu:** `Users/Commands/UpdateUser/UpdateUser.cs`

**Açıklama:** Kullanıcı bilgilerini güncellemek için kullanılan command'dir.

**Parametreler:**
- `UserId` (string): Kullanıcı ID'si
- Kullanıcı bilgileri (ad, soyad, email vb.)

**Return Type:** `Result`

**Validasyon:** FluentValidation ile validasyon yapılır.

**Kullanım Senaryosu:** Kullanıcı profil sayfasında bilgileri güncellerken kullanılır.

---

## 🌤️ WeatherForecasts

### Queries

#### GetWeatherForecasts
**Dosya Yolu:** `WeatherForecasts/Queries/GetWeatherForecasts/GetWeatherForecastsQuery.cs`

**Açıklama:** Bir site için hava durumu tahminlerini getiren query'dir. Site koordinatlarına göre mevcut hava durumu bilgilerini döner.

**Parametreler:**
- `SiteId` (Guid): Site ID'si

**Return Type:** `IEnumerable<WeatherForecast>`

**İşlevi:**
- Site erişim kontrolü yapar
- Site koordinatlarını kontrol eder
- WeatherService üzerinden mevcut hava durumu bilgilerini getirir
- Sıcaklık, nem, basınç, rüzgar hızı, açıklama ve ikon bilgilerini döner

**Kullanım Senaryosu:** Dashboard'da veya site detay sayfasında hava durumu bilgisini göstermek için kullanılır. Ayrıca DeviceEnergyData oluşturulurken hava durumu bilgisi otomatik olarak kaydedilir.

---

## 📊 Genel Notlar

### CQRS Pattern
Bu proje CQRS (Command Query Responsibility Segregation) pattern'ini kullanır:
- **Commands**: Veri değişikliği yapan işlemler (Create, Update, Delete)
- **Queries**: Sadece veri okuma işlemleri

### Validation
Tüm command'lar FluentValidation ile validate edilir. Validator dosyaları ilgili command klasörü içinde bulunur.

### Authorization
Çoğu command ve query, kullanıcının yetkili olup olmadığını kontrol eder:
- Site sahipliği kontrolü
- Site paylaşım kontrolü
- Admin rolü kontrolü

### Error Handling
Sistemde özel exception'lar kullanılır:
- `NotFoundException`: Kayıt bulunamadığında
- `UnauthorizedAccessException`: Yetkisiz erişim durumunda
- `ValidationException`: Validasyon hatası durumunda

### Pagination
Çoğu liste query'si pagination desteği içerir. `PaginatedList<T>` sınıfı kullanılır.

### Soft Delete
Birçok entity için soft delete kullanılır (`IsDeleted` flag'i). Silinen kayıtlar veritabanında kalır ancak sorgularda filtrelenir.

---

## 🔄 İş Akışları

### Site ve Device Kurulum Akışı
1. **CreateSite**: Kullanıcı yeni bir site oluşturur
2. **CreateDevice**: Device kaydı oluşturulur (veya mevcut device kullanılır)
3. **CreateSetup**: Site'a setup oluşturulur ve device bağlanır
4. **SetupDeviceParameterValues**: Device için varsayılan parametre değerleri kopyalanır
5. Kullanıcı artık dashboard'u kullanabilir

### Invitation (Davetiye) Akışı
1. **CreateInvitation**: Site sahibi email ile davetiye oluşturur
2. Email gönderilir (6 haneli token ile)
3. **AcceptInvitation**: Kullanıcı token ile davetiyeyi kabul eder
4. UserSite kaydı oluşturulur (Guest rolü ile)
5. **GetUserHomeSites**: Kullanıcı paylaşılan site'ları görebilir

### Device Data Akışı
1. **RegisterDevice**: Device sisteme login olur ve JWT token alır
2. **CreateDeviceEnergyData**: Device düzenli olarak enerji verilerini gönderir
3. **CreateDeviceNotification**: Device alarm/uyarı gönderir
4. **GetDeviceData / Dashboard Queries**: Kullanıcı verileri görüntüler

---

*Bu dokümantasyon HEMS Backend projesinin Application katmanındaki tüm feature'ları ve işlevlerini detaylı şekilde açıklar. Her feature'ın amacı, parametreleri ve kullanım senaryoları belirtilmiştir.*

