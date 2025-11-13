# Devices (Cihazlar) Sayfası - Detaylı Dokümantasyon

## İçindekiler
1. [Genel Bakış](#genel-bakış)
2. [Mimari Yapı](#mimari-yapı)
3. [Ekranlar ve Bileşenler](#ekranlar-ve-bileşenler)
4. [Servisler ve API Entegrasyonları](#servisler-ve-api-entegrasyonları)
5. [State Yönetimi](#state-yönetimi)
6. [Navigation Flow](#navigation-flow)
7. [Veri Akışı](#veri-akışı)
8. [Hooks ve Custom İşlevler](#hooks-ve-custom-işlevler)
9. [Gerçek Zamanlı Veri (Live Data)](#gerçek-zamanlı-veri-live-data)
10. [Kullanıcı İşlemleri](#kullanıcı-işlemleri)

---

## Genel Bakış

Devices modülü, kullanıcının enerji yönetim sistemi cihazlarını görüntülemesini, yönetmesini ve cihaz detaylarına erişmesini sağlayan temel bir modüldür. Modül üç ana ekrandan oluşur:

- **DeviceListScreen**: Cihaz listesini görüntüleme (şu an mockup)
- **DeviceDetail**: Cihaz detayları, bileşenler ve gerçek zamanlı veriler
- **DeviceAddScreen**: Yeni cihaz ekleme/kurulum

### Teknoloji Stack
- **React Native**: UI framework
- **TypeScript**: Type safety
- **Zustand**: State management (`sitesStore`)
- **React Navigation**: Ekran navigasyonu
- **Axios**: HTTP client (API çağrıları)
- **React Hook Form + Zod**: Form yönetimi ve validasyon
- **i18next**: Çoklu dil desteği

---

## Mimari Yapı

### Dizin Yapısı
```
src/
├── app/
│   ├── screens/
│   │   ├── deviceList/
│   │   │   ├── DeviceListScreen.tsx         # Ana cihaz listesi
│   │   │   └── DeviceListScreen.styles.ts
│   │   ├── deviceDetail/
│   │   │   ├── DeviceDetail.tsx             # Cihaz detay ana ekranı
│   │   │   ├── DeviceDetail.styles.ts
│   │   │   ├── GatewayDetailScreen.tsx      # Gateway detayları
│   │   │   ├── PVPanelDetailScreen.tsx      # PV Panel detayları
│   │   │   └── ui/                          # Alt bileşenler
│   │   │       ├── DeviceHeader.tsx
│   │   │       ├── DeviceBadge.tsx
│   │   │       ├── GridCard.tsx
│   │   │       └── BatteryDetailBottomSheet.tsx
│   │   └── devices/
│   │       ├── DeviceAddScreen.tsx          # Cihaz ekleme
│   │       └── DeviceAddScreen.styles.ts
│   └── routes/
│       └── index.tsx                        # Navigation yapılandırması
├── api/
│   ├── services/
│   │   ├── devicesService.ts                # Cihaz CRUD işlemleri
│   │   ├── deviceDataService.ts             # Cihaz verisi işlemleri
│   │   ├── deviceComponentsService.ts       # Cihaz bileşen işlemleri
│   │   ├── mobileDashboardService.ts        # Dashboard & Live Data
│   │   ├── setupsService.ts                 # Setup/Kurulum işlemleri
│   │   ├── deviceNotificationsService.ts    # Cihaz bildirimleri
│   │   └── devicesharingService.ts          # Cihaz paylaşım işlemleri
│   └── types/
│       ├── devices.types.ts
│       ├── deviceData.types.ts
│       ├── deviceComponents.types.ts
│       └── setups.types.ts
├── hooks/
│   └── useDeviceLiveData.ts                 # Live data hook
└── store/
    └── sitesStore.ts                        # Site & Device state yönetimi
```

---

## Ekranlar ve Bileşenler

### 1. DeviceListScreen
**Dosya**: `src/app/screens/deviceList/DeviceListScreen.tsx`

#### Amaç
Kullanıcının sahip olduğu cihazların listelendiği ekran.

#### Özellikler
- Basit liste görünümü
- Cihaz kartları (name, serial, status, mode, power)
- Detay sayfasına yönlendirme
- **Not**: Şu anda mockup veri kullanıyor (gerçek API entegrasyonu bekliyor)

#### Veri Yapısı
```typescript
type Device = {
  id: string;
  name: string;
  serial: string;
  status: 'online' | 'offline';
  mode: 'On Grid' | 'Off Grid';
  powerKw: number;
};
```

#### Navigation
```typescript
navigation.navigate('DeviceDetail', { device: DEVICE })
```

#### State Yönetimi
- `useThemeStore`: Tema (dark/light mode)
- `useI18n`: Çoklu dil desteği

#### UI Bileşenleri
- Card yapısı: Cihaz bilgileri
- Status dot: Online/Offline göstergesi
- Power chip: Güç bilgisi
- Chevron right icon: Detaya git

---

### 2. DeviceDetail
**Dosya**: `src/app/screens/deviceDetail/DeviceDetail.tsx`

#### Amaç
Seçili cihazın detaylı bilgilerini, bileşenlerini ve gerçek zamanlı verilerini görüntüleme.

#### Özellikler
- Cihaz başlığı ve görseli
- Bileşen listesi (Inverter, BMS, Battery)
- Gerçek zamanlı veri gösterimi
- Component detay modal (BatteryDetailBottomSheet)
- Pull-to-refresh
- Navigasyon: Gateway ve PV Panel detay ekranlarına
- Admin kontrolü (admin değilse veya site seçilmemişse Admin'e yönlendirir)

#### Ana İşlevler

**1. Component Getirme**
```typescript
const fetchDeviceComponents = useCallback(async () => {
  // 1. Site listesini al
  const sitesData = await sitesService.getUserSites();
  const siteId = sitesData?.siteList?.[0]?.id;

  // 2. Setup device bilgisini al
  const setupRes = await mobileDashboardService.getSetupDevice(siteId);
  const device = setupRes?.device;

  // 3. Device ID'yi kaydet
  setDeviceId(device.id);
  setSetupId(setupRes?.setupId);

  // 4. Cihaz bileşenlerini ve verilerini al
  const withDataRes = await mobileDashboardService.getDeviceWithData(device.id);

  // 5. Bileşenleri state'e kaydet
  setComponents(comps);
}, []);
```

**2. Live Data Alma**
```typescript
const { data: batteryData, isFetching, fetchLiveData } = useDeviceLiveData(deviceId);

// Inverter için özel işlem
useEffect(() => {
  const loadInverterLiveData = async () => {
    const inverterComp = components.find((c) =>
      c.name.toLowerCase().includes("inverter")
    );
    if (inverterComp) {
      const data = await fetchLiveData(inverterComp);
      setInverterData(data);
    }
  };

  if (components.length > 0) {
    loadInverterLiveData();
  }
}, [components]);
```

**3. Component Modal Açma**
```typescript
const handleOpenBattery = async (component: any) => {
  setSelectedBattery(component);
  setBatterySheetVisible(true);
  await fetchLiveData(component); // Live data çek
};
```

**4. Alt Ekranlara Navigasyon**
```typescript
const navigateToDetailScreen = (componentId: string) => {
  switch (componentId) {
    case "pvpanel":
      navigation.navigate("PVPanelDetail");
      break;
    case "gateway":
      navigation.navigate("GatewayDetail");
      break;
  }
};
```

#### UI Bileşenleri
- **Header**: Geri buton, başlık, bildirim ikonu
- **Hero Card**: Cihaz görseli, component badge'leri
- **Stats Section**:
  - Daily Production (Günlük Üretim)
  - Battery Charge (Batarya Şarjı - SOC)
  - Connection Type (Bağlantı Tipi - On/Off Grid)
- **Device Grid**: Gateway ve PV Panel kartları
- **BatteryDetailBottomSheet**: Modal popup (component detayları)

#### State Yönetimi
```typescript
const [components, setComponents] = useState<any[]>([]);
const [deviceId, setDeviceId] = useState<string | null>(null);
const [setupId, setSetupId] = useState<string | null>(null);
const [loading, setLoading] = useState(true);
const [refreshing, setRefreshing] = useState(false);
const [batterySheetVisible, setBatterySheetVisible] = useState(false);
const [selectedBattery, setSelectedBattery] = useState<any>(null);
const [inverterData, setInverterData] = useState<any>(null);
```

#### Animasyonlar
```typescript
const headerOpacity = useRef(new Animated.Value(0)).current;
const headerY = useRef(new Animated.Value(-50)).current;

useEffect(() => {
  Animated.parallel([
    Animated.timing(headerOpacity, { toValue: 1, duration: 800 }),
    Animated.spring(headerY, { toValue: 0, tension: 50, friction: 8 }),
  ]).start();
}, []);
```

---

### 3. DeviceAddScreen
**Dosya**: `src/app/screens/devices/DeviceAddScreen.tsx`

#### Amaç
Kullanıcının mevcut site'ına yeni cihaz eklemesi için form ekranı.

#### Özellikler
- QR kod tarama desteği
- Form validasyonu (Zod schema)
- Site bilgisi gösterimi
- Setup oluşturma
- Site listesi yenileme
- Toast bildirimleri

#### Form Schema (Zod)
```typescript
const deviceAddSchema = z.object({
  setupName: z
    .string()
    .min(2, 'Setup name must be at least 2 characters')
    .max(64, 'Setup name is too long'),
  deviceSerialNumber: z
    .string()
    .min(6, 'Serial number must be at least 6 characters')
    .max(40, 'Serial number is too long'),
});

type DeviceAddForm = z.infer<typeof deviceAddSchema>;
```

#### Form İşlemleri
```typescript
const {
  control,
  handleSubmit,
  setValue,
  watch,
  formState: { errors },
} = useForm<DeviceAddForm>({
  resolver: zodResolver(deviceAddSchema),
  defaultValues: {
    setupName: currentSite?.name || '',
    deviceSerialNumber: '',
  },
});
```

#### QR Kod Tarama
```typescript
const handleQrScan = useCallback(
  (code: string) => {
    setValue('deviceSerialNumber', code);
    setIsQrScannerVisible(false);
  },
  [setValue],
);
```

#### Submit İşlemi
```typescript
const onSubmit = useCallback(
  async (values: DeviceAddForm) => {
    setIsSubmitting(true);
    try {
      // 1. Setup oluştur
      await setupsService.create({
        siteId: selectedSite?.id!,
        name: values.setupName,
        deviceQRCode: null,
        deviceSerialNumber: values.deviceSerialNumber,
      });

      // 2. Site listesini yenile (hasDevice güncellemesi için)
      await getUserSites();

      // 3. Seçili site'ın cihaz bilgisini yükle
      await loadSelectedSiteDevice();

      // 4. Başarı mesajı göster
      showToast({
        type: 'success',
        title: t('deviceAdd.success.title'),
        message: t('deviceAdd.success.message'),
      });

      // 5. Geri dön
      navigation.goBack();
    } catch (error: any) {
      showToast({
        type: 'error',
        title: t('deviceAdd.error.title'),
        message: error.message || t('deviceAdd.error.message'),
      });
    } finally {
      setIsSubmitting(false);
    }
  },
  [selectedSite?.id, getUserSites, loadSelectedSiteDevice, navigation, t],
);
```

#### State Yönetimi
- `useSitesStore`: Site seçimi, site listesi, cihaz bilgisi
- Local state: `isQrScannerVisible`, `isSubmitting`

#### UI Bileşenleri
- **Card**: Ana form container
- **Site Info**: Seçili site adı ve adresi
- **Input**: Setup name ve serial number
- **Button**: QR tarama, form gönderme, iptal
- **QrScannerModal**: QR kod okuyucu modal

---

### Alt Bileşenler

#### DeviceBadge
**Dosya**: `src/app/screens/deviceDetail/ui/DeviceBadge.tsx`

Component badge'leri (Inverter, BMS, Battery). Her bileşenin adı, değeri ve ikonu gösterilir.

```typescript
<DeviceBadge
  COLORS={COLORS}
  title="Inverter"
  subtitle=""
  value="5.16"
  icon="bolt"
  status="normal"
  onPress={() => handleOpenBattery(comp)}
/>
```

#### GridCard
**Dosya**: `src/app/screens/deviceDetail/ui/GridCard.tsx`

Gateway ve PV Panel için grid kartları.

```typescript
<GridCard
  COLORS={COLORS}
  title={t("deviceDetail.pvPanel.title")}
  subtitle="—"
  icon="sun-o"
  onPress={() => navigateToDetailScreen("pvpanel")}
/>
```

#### BatteryDetailBottomSheet
**Dosya**: `src/app/screens/deviceDetail/ui/BatteryDetailBottomSheet.tsx`

Component detaylarını gösteren modal. Batarya, Inverter veya BMS verileri gösterilir.

**Props:**
```typescript
interface BatteryDetailBottomSheetProps {
  visible: boolean;
  onClose: () => void;
  onRestart: () => void;
  loading: boolean;
  deviceName: string;
  batteryData: any;
}
```

---

## Servisler ve API Entegrasyonları

### 1. devicesService
**Dosya**: `src/api/services/devicesService.ts`

Cihaz CRUD işlemleri.

#### Fonksiyonlar
```typescript
export const devicesService = {
  // Tüm cihazları listele (pagination)
  async getAll(params: GetDevicesParams): Promise<PaginatedDeviceList>

  // ID'ye göre cihaz detayı
  async getById(id: string): Promise<DeviceDto>

  // Yeni cihaz oluştur
  async create(data: CreateDeviceCommand): Promise<string>

  // Cihaz güncelle
  async update(id: string, data: UpdateDeviceCommand): Promise<void>

  // Cihaz sil
  async delete(id: string): Promise<void>

  // Cihaz login (özel)
  async login(data: DeviceLoginCommand): Promise<DeviceLoginResult>
};
```

#### API Endpoints
- `GET /api/devices` - Tüm cihazlar
- `GET /api/devices/:id` - Cihaz detayı
- `POST /api/devices` - Cihaz oluştur
- `PUT /api/devices/:id` - Cihaz güncelle
- `DELETE /api/devices/:id` - Cihaz sil
- `POST /api/devices/login` - Cihaz login

---

### 2. deviceComponentsService
**Dosya**: `src/api/services/devicesService.ts`

Cihaz bileşenleri (Inverter, BMS, Battery vb.) işlemleri.

#### Fonksiyonlar
```typescript
export const deviceComponentsService = {
  // Cihaz ID'sine göre bileşenleri al
  async getByDeviceId(params: GetDeviceComponentParams): Promise<DeviceComponentDto[]>

  // Bileşen detayı
  async getById(id: string): Promise<DeviceComponentDto>

  // Yeni bileşen oluştur
  async create(data: CreateDeviceComponentCommand): Promise<string>

  // Bileşen güncelle
  async update(id: string, data: UpdateDeviceComponentCommand): Promise<void>

  // Bileşen sil
  async delete(id: string): Promise<void>
};
```

#### API Endpoints
- `GET /api/devicecomponents/device/:deviceId` - Cihaz bileşenleri
- `GET /api/devicecomponents/:id` - Bileşen detayı
- `POST /api/devicecomponents` - Bileşen oluştur
- `PUT /api/devicecomponents/:id` - Bileşen güncelle
- `DELETE /api/devicecomponents/:id` - Bileşen sil

---

### 3. mobileDashboardService
**Dosya**: `src/api/services/mobileDashboardService.ts`

Dashboard verileri ve gerçek zamanlı component verileri.

#### Fonksiyonlar
```typescript
export const mobileDashboardService = {
  // Setup device bilgisi (siteId ile)
  async getSetupDevice(siteId: string): Promise<any>

  // Cihaz ve bileşen verileri
  async getDeviceWithData(deviceId: string): Promise<DeviceWithDataResponseDto>

  // Component live data
  async getComponentLiveData(deviceId: string, componentId: string): Promise<GetComponentLiveDataResponse>

  // Hero metrics
  async getHero(deviceId: string): Promise<HeroResponseDto>

  // SOC (State of Charge) data
  async getSOC(deviceId: string): Promise<SOCResponseDto>

  // SOH (State of Health) data
  async getSOH(deviceId: string): Promise<SOHResponseDto>

  // Energy flow
  async getEnergyFlow(deviceId: string, startDate: string, endDate: string): Promise<EnergyFlowResponseDto>

  // Energy production
  async getEnergyProduction(deviceId: string, period: string): Promise<EnergyProductionResponseDto>

  // Energy consumption
  async getEnergyConsumption(deviceId: string, period: string): Promise<EnergyConsumptionResponseDto>

  // Power metrics
  async getPowerMetrics(deviceId: string, period: string): Promise<PowerMetricsResponseDto>

  // Battery data
  async getBattery(deviceId: string): Promise<BatteryResponseDto>

  // Charge/Discharge data
  async getChargeDischarge(deviceId: string): Promise<ChargeDischargeResponseDto>

  // Battery component
  async getBatteryComponent(deviceId: string): Promise<BatteryComponentDto>

  // Inverter component
  async getInverterComponent(deviceId: string): Promise<InverterComponentDto>
};
```

#### API Endpoints
- `GET /api/mobiledashboard/setup-device?siteId=XXX`
- `GET /api/mobiledashboard/device-with-data?deviceId=XXX`
- `GET /api/mobiledashboard/component-live-data?deviceId=XXX&componentId=YYY`
- `GET /api/mobiledashboard/hero?deviceId=XXX`
- `GET /api/mobiledashboard/soc?deviceId=XXX`
- `GET /api/mobiledashboard/soh?deviceId=XXX`
- `GET /api/mobiledashboard/energy-flow?deviceId=XXX&startDate=...&endDate=...`
- Ve diğerleri...

---

### 4. setupsService
**Dosya**: `src/api/services/setupsService.ts`

Setup (kurulum) işlemleri - cihaz ekleme için kritik.

#### Fonksiyonlar
```typescript
export const setupsService = {
  // Tüm setup'ları listele
  async getAll(params: GetSetupsParams): Promise<PaginatedSetupList>

  // ID'ye göre setup detayı
  async getById(id: string): Promise<SetupDto>

  // Site'a ait cihaz bilgisi
  async getSetupDevice(params: GetSetupDeviceParams): Promise<UserSetupDeviceDto>

  // Yeni setup oluştur (CIHAZ EKLEME)
  async create(data: CreateSetupCommand): Promise<string>

  // Setup güncelle
  async update(id: string, data: UpdateSetupCommand): Promise<void>

  // Setup sil (mobile)
  async deleteMobile(id: string): Promise<void>

  // Setup sil (admin)
  async deleteAdmin(id: string): Promise<void>
};
```

#### Create Setup Command
```typescript
interface CreateSetupCommand {
  siteId: string;
  name: string;
  deviceQRCode: string | null;
  deviceSerialNumber: string;
}
```

#### API Endpoints
- `GET /api/setups` - Tüm setup'lar
- `GET /api/setups/:id` - Setup detayı
- `GET /api/setups/setup-device?siteId=XXX` - Site device bilgisi
- `POST /api/setups` - Setup oluştur (cihaz ekle)
- `PUT /api/setups/:id` - Setup güncelle
- `DELETE /api/setups/mobile/:id` - Setup sil (mobile)
- `DELETE /api/setups/admin/:id` - Setup sil (admin)

---

### 5. deviceDataService
**Dosya**: `src/api/services/deviceDataService.ts`

Cihaz verilerinin kaydedilmesi ve sorgulanması.

#### Fonksiyonlar
```typescript
export const deviceDataService = {
  // Tüm cihaz verilerini listele
  async getAll(params: GetDeviceDataParams): Promise<PaginatedDeviceDataList>

  // Yeni veri oluştur
  async create(data: CreateDeviceDataCommand): Promise<number>

  // Enerji verisi oluştur
  async createEnergyData(data: CreateDeviceEnergyDataCommand): Promise<number>

  // Toplu veri oluştur
  async bulkCreate(data: BulkCreateDeviceDataCommand): Promise<BulkCreateDeviceDataResult>
};
```

---

### 6. deviceNotificationsService
**Dosya**: `src/api/services/deviceNotificationsService.ts`

Cihaz bildirimleri yönetimi.

---

### 7. devicesharingService
**Dosya**: `src/api/services/devicesharingService.ts`

Cihaz paylaşımı ve davet yönetimi.

---

## State Yönetimi

### sitesStore (Zustand)
**Dosya**: `src/store/sitesStore.ts`

Devices modülü için ana state store'u.

#### State Yapısı
```typescript
interface SitesState {
  sites: SiteDto[];                              // Kullanıcının tüm siteleri
  sitesDashboard: SitesDashboardDto | null;      // Dashboard verileri
  currentSite: SiteDto | null;                   // Mevcut site
  selectedSite: SiteDto | null;                  // Seçili site
  selectedSiteDevice: UserSetupDeviceDto | null; // Seçili site'ın cihazı
  paginatedSites: PaginatedSiteList | null;      // Paginated liste
  isLoading: boolean;                            // Loading durumu
  error: string | null;                          // Hata mesajı
}
```

#### Actions (Fonksiyonlar)

**1. getUserSites**
```typescript
getUserSites: async () => Promise<SitesDashboardDto>
```
Kullanıcının tüm sitelerini çeker ve AsyncStorage'dan seçili site'ı yükler.

**Akış:**
1. `sitesService.getUserSites()` çağrısı
2. `sites` ve `sitesDashboard` state'ine kaydet
3. `loadSelectedSiteFromStorage()` çağır (AsyncStorage'dan seçili site'ı yükle)

**2. getSiteDevice**
```typescript
getSiteDevice: async (siteId: string) => Promise<UserSetupDeviceDto | null>
```
Belirli bir site'ın cihaz bilgisini çeker.

**Akış:**
1. Site listesinden siteId'yi bul
2. `site.hasDevice` kontrolü
3. `setupsService.getSetupDevice()` ile cihaz bilgisini al

**3. loadSelectedSiteDevice**
```typescript
loadSelectedSiteDevice: async () => Promise<void>
```
Seçili site'ın cihaz bilgisini yükler ve state'e kaydeder.

**Akış:**
1. `selectedSite` kontrolü
2. `selectedSite.hasDevice` kontrolü
3. `setupsService.getSetupDevice()` çağrısı
4. `selectedSiteDevice` state'ine kaydet

**4. setSelectedSite**
```typescript
setSelectedSite: (site: SiteDto | null) => void
```
Kullanıcının seçtiği site'ı state'e ve AsyncStorage'a kaydeder.

**Akış:**
1. `selectedSite` state'ine kaydet
2. `selectedSiteDevice = null` (temizle)
3. AsyncStorage'a `@selected_home` key'i ile kaydet

**5. selectSiteById**
```typescript
selectSiteById: async (id: string) => Promise<void>
```
ID'ye göre site seçimi yapar.

**Akış:**
1. Site listesinden ID ile site bul
2. `setSelectedSite()` çağır
3. AsyncStorage'a kaydet

**6. loadSelectedSiteFromStorage**
```typescript
loadSelectedSiteFromStorage: async () => Promise<void>
```
Uygulama açıldığında AsyncStorage'dan son seçili site'ı yükler.

**Akış:**
1. AsyncStorage'dan `@selected_home` oku
2. Site listesinden ID ile site bul
3. Bulunursa: `selectedSite` state'ine kaydet
4. Bulunamazsa: AsyncStorage'dan sil, state temizle

**7. createSite, updateSite, deleteSite**
Site CRUD işlemleri. İşlem sonrası `getUserSites()` çağrılarak liste yenilenir.

**8. clearSites**
Logout durumunda tüm site verilerini temizler (AsyncStorage dahil).

#### AsyncStorage Keys
- `@selected_home`: Seçili site ID'si
- `@user_homes`: Site listesi (opsiyonel cache)

#### Hata Yönetimi
- Canceled request'ler sessizce handle edilir (token refresh sırasında)
- 401 hataları throw edilir (auth için)
- Diğer hatalar `error` state'ine kaydedilir

---

## Navigation Flow

### Tab Navigator
**Dosya**: `src/app/routes/index.tsx:77`

Bottom tab'larda "Device" sekmesi:

```typescript
<Tab.Screen
  name="Device"
  component={DeviceDetail}
  options={{
    tabBarLabel: t('tabs.device'),
    tabBarIcon: ({ color, size }) => (
      <Smartphone color={color} size={size} strokeWidth={2} />
    ),
  }}
/>
```

**Önemli:** Admin kullanıcılar ev seçmediklerinde Device tab'ı gösterilmez.

### Stack Navigator
**Dosya**: `src/app/routes/index.tsx:304`

Protected screens (authentication gerekli):

```typescript
{isAuthenticated && (
  <>
    {/* Cihaz ekleme */}
    <Stack.Screen name="DeviceAdd" component={DeviceAddScreen} />

    {/* PV Panel detayı */}
    <Stack.Screen
      name="PVPanelDetail"
      component={PVPanelDetailScreen}
      options={{ animation: 'slide_from_right' }}
    />

    {/* Gateway detayı */}
    <Stack.Screen
      name="GatewayDetail"
      component={GatewayDetailScreen}
      options={{ animation: 'slide_from_right' }}
    />

    {/* Device detay (standalone) */}
    <Stack.Screen
      name="DeviceDetail"
      component={DeviceDetail}
      options={{ animation: 'slide_from_right' }}
    />
  </>
)}
```

### Navigation Akışı

```
Dashboard
  └─> Device (Tab)
       ├─> DeviceDetail (Ana Ekran)
       │    ├─> PVPanelDetail
       │    ├─> GatewayDetail
       │    └─> BatteryDetailBottomSheet (Modal)
       │
       └─> DeviceAdd (Cihaz Ekleme)
            └─> QrScannerModal (Modal)
```

### Navigation Kullanımı

**1. Tab'dan Device'a**
```typescript
// Bottom tab'da "Device" sekmesine tıklama
navigation.navigate('Device');
```

**2. Device Ekleme**
```typescript
// Profile veya başka bir ekrandan
navigation.navigate('DeviceAdd', { siteId: 'xxx' });
```

**3. Alt Ekranlara**
```typescript
// DeviceDetail içinden
navigation.navigate('PVPanelDetail');
navigation.navigate('GatewayDetail');
```

**4. Geri Dönüş**
```typescript
navigation.goBack();
```

---

## Veri Akışı

### 1. Cihaz Listesi Yükleme (Gelecek)

```
User Action (Open DeviceList)
  │
  ├─> devicesService.getAll()
  │     └─> GET /api/devices
  │           └─> Response: PaginatedDeviceList
  │
  └─> setState(devices)
        └─> Render device cards
```

### 2. Cihaz Detay Yükleme

```
User Action (Open DeviceDetail)
  │
  ├─> sitesService.getUserSites()
  │     └─> GET /api/sites/user-sites
  │           └─> Response: SitesDashboardDto
  │
  ├─> mobileDashboardService.getSetupDevice(siteId)
  │     └─> GET /api/mobiledashboard/setup-device?siteId=xxx
  │           └─> Response: { setupId, device }
  │
  ├─> mobileDashboardService.getDeviceWithData(deviceId)
  │     └─> GET /api/mobiledashboard/device-with-data?deviceId=xxx
  │           └─> Response: DeviceWithDataResponseDto
  │                 └─> components: [Inverter, BMS, Battery]
  │
  ├─> setState(components, deviceId, setupId)
  │
  └─> useDeviceLiveData.fetchLiveData(inverterComp)
        └─> mobileDashboardService.getComponentLiveData(deviceId, componentId)
              └─> GET /api/mobiledashboard/component-live-data
                    └─> Response: { success, data }
                          └─> Parse & setState(inverterData)
```

### 3. Component Modal (Live Data)

```
User Action (Click on Component Badge)
  │
  ├─> handleOpenBattery(component)
  │     ├─> setSelectedBattery(component)
  │     ├─> setBatterySheetVisible(true)
  │     └─> fetchLiveData(component)
  │           │
  │           ├─> mobileDashboardService.getDeviceWithData(deviceId)
  │           │     └─> Find component by componentId
  │           │
  │           ├─> mobileDashboardService.getComponentLiveData(deviceId, componentId)
  │           │     └─> GET live data
  │           │
  │           └─> Parse data based on component type:
  │                 ├─> Inverter: Active Power, Phases, etc.
  │                 ├─> BMS: SOC, Voltage, Current, etc.
  │                 └─> Battery: SOC, SOH, Voltage, Temp, etc.
  │
  └─> Render BatteryDetailBottomSheet with parsed data
```

### 4. Cihaz Ekleme

```
User Action (Submit DeviceAddScreen)
  │
  ├─> Zod Validation (setupName, deviceSerialNumber)
  │     └─> If valid, continue
  │
  ├─> setupsService.create()
  │     └─> POST /api/setups
  │           └─> Body: { siteId, name, deviceSerialNumber }
  │                 └─> Response: setupId (string)
  │
  ├─> useSitesStore.getUserSites()
  │     └─> Refresh site list (hasDevice will be updated)
  │
  ├─> useSitesStore.loadSelectedSiteDevice()
  │     └─> Load device info for selected site
  │
  ├─> showToast (Success)
  │
  └─> navigation.goBack()
```

### 5. Pull-to-Refresh

```
User Action (Pull down on DeviceDetail)
  │
  ├─> setRefreshing(true)
  │
  ├─> fetchDeviceComponents() [full refresh]
  │     └─> Re-fetch all data (steps from "Cihaz Detay Yükleme")
  │
  └─> setRefreshing(false)
```

---

## Hooks ve Custom İşlevler

### useDeviceLiveData
**Dosya**: `src/hooks/useDeviceLiveData.ts`

Cihaz bileşenlerinin gerçek zamanlı verilerini çekmek için özel hook.

#### Signature
```typescript
function useDeviceLiveData(deviceId: string | null): {
  data: any;
  isFetching: boolean;
  fetchLiveData: (component: any) => Promise<any>;
}
```

#### Kullanım
```typescript
const { data: batteryData, isFetching, fetchLiveData } = useDeviceLiveData(deviceId);

// Component seçildiğinde veri çek
await fetchLiveData(selectedComponent);
```

#### İç Yapı

**1. State**
```typescript
const [isFetching, setIsFetching] = useState(false);
const [data, setData] = useState<any>(null);
```

**2. fetchLiveData Fonksiyonu**
```typescript
const fetchLiveData = async (component: any) => {
  if (!deviceId) return null;
  setIsFetching(true);

  try {
    // 1. Device ile component listesini al
    const devWithData = await mobileDashboardService.getDeviceWithData(deviceId);

    // 2. Target component'i bul
    const target = devWithData?.components?.find(
      (c: any) => c.componentId === component.id
    );

    // 3. Live data çek
    const liveData = await mobileDashboardService.getComponentLiveData(
      deviceId,
      target.componentId
    );

    // 4. Component tipine göre parse et
    const d = liveData.data || {};
    let parsed: any = {};

    if (component.name.toLowerCase().includes("inverter")) {
      parsed = {
        type: "inverter",
        "Active Power": d["Active Power"] ?? 0,
        "Reactive Power": d["Reactive Power"] ?? 0,
        "Power Factor": d["Power Factor"] ?? 0,
        "Insulation Resistance": d["Insulation Resistance"] ?? 0,
        "Last Inverter Startup Time": d["Last Inverter Startup Time"] ?? "-",
        "Last Inverter Shutdown Time": d["Last Inverter Shutdown Time"] ?? "-",
        "Device Serial Number": d["Device Serial Number"] ?? "-",
        "Firmware Version": d["Firmware Version"] ?? "-",
        Phases: d.Phases ?? [],
      };
    } else if (component.name.toLowerCase().includes("bms")) {
      parsed = {
        type: "bms",
        SOC: component.componentData ?? 0,
        VoltagePack: d["Voltage Pack"] ?? 0,
        CurrentPack: d["Current Pack"] ?? 0,
        MaxVoltageOfBatteryCluster: d["Max Voltage Of Battery Cluster"] ?? 0,
        MinVoltageOfBatteryCluster: d["Min Voltage Of Battery Cluster"] ?? 0,
        TotalChargeCapacity: d["Total Charge Capacity"] ?? 0,
        TotalDischargeCapacity: d["Total Discharge Capacity"] ?? 0,
        DateTime: d["DateTime"] ?? "-",
        DeviceSerialNumber: d["Device Serial Number"] ?? "-",
        FirmwareVersion: d["Firmware Version"] ?? "-",
      };
    } else {
      // Battery
      const cells = d.SOC || [];
      const avgVoltage =
        cells.reduce((a: number, c: any) => a + (c.CellVoltage || 0), 0) /
          (cells.length || 1) || 0;
      const avgTemp =
        cells.reduce((a: number, c: any) => a + (c.CellTemperature || 0), 0) /
          (cells.length || 1) || 0;

      parsed = {
        type: "battery",
        soc: component.componentData ?? 0,
        soh: d.SOH ?? "-",
        voltage: d.BatteryPackVoltage ?? 0,
        current: d.BatteryCurrent ?? 0,
        maxCellVoltage: d.MaxCellVoltage ?? 0,
        minCellVoltage: d.MinCellVoltage ?? 0,
        maxCellTemp: d.MaxCellTemperature ?? 0,
        minCellTemp: d.MinCellTemperature ?? 0,
        avgCellVoltage: avgVoltage,
        avgTemp: avgTemp,
        totalCharge: d.TotalChargeCapacity ?? "-",
        totalDischarge: d.TotalDischargeAmount ?? 0,
        model: d.DeviceModel ?? "-",
        sn: d.DeviceSerialNumber ?? "-",
        version: d.FirmwareVersion ?? "-",
        dateTime: d.DateTime ?? "-",
      };
    }

    setData(parsed);
    return parsed;
  } catch (err) {
    setData(null);
    return null;
  } finally {
    setIsFetching(false);
  }
};
```

#### Component Tipleri ve Parse Mantığı

**Inverter:**
- Active Power, Reactive Power, Power Factor
- Insulation Resistance
- Startup/Shutdown times
- Serial Number, Firmware Version
- Phase bilgileri (array)

**BMS (Battery Management System):**
- SOC (State of Charge)
- Voltage Pack, Current Pack
- Max/Min Battery Cluster Voltage
- Total Charge/Discharge Capacity
- DateTime, Serial Number, Firmware Version

**Battery:**
- SOC, SOH (State of Health)
- Battery Pack Voltage, Current
- Max/Min Cell Voltage
- Max/Min Cell Temperature
- Average Cell Voltage/Temperature (hesaplanmış)
- Total Charge/Discharge
- Device Model, Serial Number, Firmware Version

---

### useSelectedSiteUtils
**Dosya**: `src/hooks/useSelectedSite.ts`

Site seçimi için yardımcı hook (DeviceDetail'de kullanılıyor).

```typescript
const { selectedSite } = useSelectedSiteUtils();
```

---

### useRoles
**Dosya**: `src/hooks/useRoles.ts`

Kullanıcı rol kontrolü (Admin vs User).

```typescript
const { isAdmin, hasRole } = useRoles();
const isAdminUser = isAdmin || hasRole("Administrator");
```

DeviceDetail'de admin kontrolü için kullanılır. Admin kullanıcı ev seçmediyse Admin sayfasına yönlendirilir.

---

## Gerçek Zamanlı Veri (Live Data)

### Veri Kaynağı
Live data, backend'den REST API ile çekilir (SignalR değil, polling yok).

### Endpoint
```
GET /api/mobiledashboard/component-live-data
  ?deviceId=xxx
  &componentId=yyy
```

### Response Yapısı
```typescript
interface GetComponentLiveDataResponse {
  success: boolean;
  message?: string;
  data?: any; // Component tipine göre değişir
}
```

### Veri Tipleri

#### Inverter Data
```json
{
  "Active Power": 5.166,
  "Reactive Power": 0.23,
  "Power Factor": 0.95,
  "Insulation Resistance": 1200,
  "Last Inverter Startup Time": "2023-10-15T08:30:00Z",
  "Last Inverter Shutdown Time": "2023-10-14T20:00:00Z",
  "Device Serial Number": "INV12345678",
  "Firmware Version": "v2.3.1",
  "Phases": [
    { "voltage": 230, "current": 15.2, "power": 3498 },
    { "voltage": 228, "current": 14.8, "power": 3374 },
    { "voltage": 231, "current": 15.5, "power": 3580 }
  ]
}
```

#### BMS Data
```json
{
  "Voltage Pack": 52.4,
  "Current Pack": 12.5,
  "Max Voltage Of Battery Cluster": 53.2,
  "Min Voltage Of Battery Cluster": 51.8,
  "Total Charge Capacity": 150,
  "Total Discharge Capacity": 145,
  "DateTime": "2023-10-15T10:30:00Z",
  "Device Serial Number": "BMS98765432",
  "Firmware Version": "v1.8.2"
}
```

#### Battery Data
```json
{
  "SOH": 95,
  "BatteryPackVoltage": 52.1,
  "BatteryCurrent": 11.2,
  "MaxCellVoltage": 3.65,
  "MinCellVoltage": 3.58,
  "MaxCellTemperature": 28.5,
  "MinCellTemperature": 25.3,
  "TotalChargeCapacity": 200,
  "TotalDischargeAmount": 185,
  "DeviceModel": "LiFePO4-200Ah",
  "DeviceSerialNumber": "BAT11223344",
  "FirmwareVersion": "v3.1.0",
  "DateTime": "2023-10-15T10:30:00Z",
  "SOC": [
    { "CellVoltage": 3.62, "CellTemperature": 27.1 },
    { "CellVoltage": 3.60, "CellTemperature": 26.8 },
    ...
  ]
}
```

### Refresh Stratejisi

**Manuel Refresh:**
- Pull-to-refresh (DeviceDetail)
- Modal açıldığında (BatteryDetailBottomSheet)

**Otomatik Refresh:**
- Şu an yok (gelecekte polling veya SignalR eklenebilir)

---

## Kullanıcı İşlemleri

### 1. Cihaz Listesini Görüntüleme

**Akış:**
1. Kullanıcı "Devices" tab'ına tıklar
2. DeviceListScreen açılır
3. Mock veri gösterilir (gelecekte API'den çekilecek)
4. Cihaz kartlarında: name, serial, status, mode, power

**UI:**
- Card list
- Status indicator (online/offline)
- Power chip (kW)
- Chevron right (detaya git)

---

### 2. Cihaz Detaylarını Görüntüleme

**Akış:**
1. Kullanıcı cihaz kartına tıklar (veya "Device" tab'ına gider)
2. DeviceDetail açılır
3. Loading gösterilir
4. API çağrıları:
   - Site listesi
   - Setup device
   - Device with data
   - Inverter live data
5. Bileşenler ve istatistikler gösterilir

**UI:**
- Header (geri buton, başlık, bildirim)
- Hero card (cihaz görseli, bileşen badge'leri)
- Stats section (günlük üretim, batarya, bağlantı)
- Device grid (Gateway, PV Panel)

**İşlemler:**
- Pull-to-refresh: Tüm veriyi yenile
- Component badge tıklama: Modal aç, live data göster
- Grid card tıklama: Alt detay ekranına git

---

### 3. Component Detaylarını Görüntüleme

**Akış:**
1. Kullanıcı component badge'ine tıklar (örn: Inverter, BMS, Battery)
2. BatteryDetailBottomSheet açılır
3. Loading gösterilir
4. Live data çekilir (`fetchLiveData`)
5. Component tipine göre veriler parse edilir
6. Modal'da gösterilir

**UI:**
- Bottom sheet modal
- Header (component adı, kapatma butonu)
- Metrics (voltaj, akım, güç vb.)
- Device info (serial number, firmware)
- Restart button (admin)

**Component Tipleri:**
- **Inverter**: Active Power, Phases, Power Factor
- **BMS**: SOC, Voltage Pack, Current Pack
- **Battery**: SOC, SOH, Cell Voltage/Temp

---

### 4. Yeni Cihaz Ekleme

**Akış:**
1. Kullanıcı "Add Device" butonuna tıklar (Profile veya başka yerden)
2. DeviceAddScreen açılır
3. Form gösterilir:
   - Setup Name (default: site adı)
   - Device Serial Number
4. Kullanıcı manuel girebilir veya QR kod tarayabilir
5. Form submit edilir
6. Validasyon (Zod)
7. API çağrısı: `setupsService.create()`
8. Site listesi ve seçili site device'ı yenilenir
9. Başarı toast gösterilir
10. Geri dönülür

**UI:**
- Card container
- Site info (seçili site adı ve adresi)
- Input fields (Setup Name, Serial Number)
- QR scan button
- Submit button (disabled: loading veya serial number boş)
- Cancel button

**Validasyon:**
- Setup Name: 2-64 karakter
- Serial Number: 6-40 karakter

**QR Kod Tarama:**
1. "Scan QR" butonuna tıkla
2. QrScannerModal açılır
3. Kamera ile QR kod taranır
4. Serial Number field'ı otomatik doldurulur
5. Modal kapanır

---

### 5. Alt Detay Ekranlarına Gitme

**Gateway Detail:**
1. DeviceDetail'de "Gateway" card'ına tıkla
2. GatewayDetailScreen açılır

**PV Panel Detail:**
1. DeviceDetail'de "PV Panel" card'ına tıkla
2. PVPanelDetailScreen açılır

---

### 6. Cihazı Yeniden Başlatma (Restart)

**Akış:**
1. Component modal'ında "Restart" butonuna tıkla
2. Alert confirmation gösterilir
3. Kullanıcı onaylarsa restart komutu gönderilir (şu an mockup)
4. Başarı mesajı gösterilir

**Not:** Şu an gerçek API entegrasyonu yok, sadece UI.

---

## Notlar ve Önemli Bilgiler

### 1. DeviceListScreen Mock Veri Kullanıyor
`DeviceListScreen.tsx` şu an hardcoded bir `DEVICE` objesi kullanıyor. Gerçek API entegrasyonu için:
- `devicesService.getAll()` ile cihaz listesi çekilmeli
- State yönetimi eklenmeli (`useState` veya Zustand)
- Loading/Error handling eklenmeli

### 2. Admin Kullanıcı Kontrolü
Admin kullanıcılar ev seçmediklerinde:
- Device tab gösterilmez
- DeviceDetail'e gitmeye çalışırlarsa Admin sayfasına yönlendirilir

Kod:
```typescript
useFocusEffect(
  useCallback(() => {
    if (isAdminUser && !selectedSite) {
      navigation.navigate('Admin');
    }
  }, [isAdminUser, selectedSite, navigation])
);
```

### 3. Site Selection Mandatory
Devices modülü için **seçili bir site gereklidir**. Kullanıcı site seçmemişse:
- Dashboard veya Profile'dan site seçmesi istenir
- DeviceDetail çalışmaz (site bilgisi olmadan deviceId alınamaz)

### 4. AsyncStorage Kullanımı
Site seçimi `AsyncStorage` ile persist edilir:
- Key: `@selected_home`
- Value: Site ID (string)
- Uygulama açıldığında otomatik yüklenir (`loadSelectedSiteFromStorage`)

### 5. Token Refresh ve Canceled Request
`sitesStore`'da canceled request'ler sessizce handle edilir:
```typescript
if (error?.message?.includes('canceled') || error?.code === 'ERR_CANCELED') {
  set({ isLoading: false });
  return undefined as any;
}
```
Bu, token refresh sırasında iptal edilen request'lerin hata fırlatmamasını sağlar.

### 6. Lazy Loading
Seçili site'ın cihaz bilgisi lazy loading ile alınır. Site seçimi yapıldığında otomatik olarak cihaz bilgisi yüklenmez, sadece gerektiğinde çekilir (örn: DeviceDetail açıldığında).

### 7. Pull-to-Refresh
DeviceDetail'de pull-to-refresh aktif:
```typescript
<ScrollView
  refreshControl={
    <RefreshControl
      refreshing={refreshing}
      onRefresh={onRefresh}
      tintColor={COLORS.accent}
    />
  }
>
```
Tüm cihaz verilerini yeniler.

### 8. Component Parsing Mantığı
`useDeviceLiveData` hook'u component tipini `name` field'ından algılar:
- `name.toLowerCase().includes("inverter")` → Inverter parsing
- `name.toLowerCase().includes("bms")` → BMS parsing
- Diğer → Battery parsing

### 9. i18n (Çoklu Dil)
Tüm metinler `useI18n()` hook'u ile çekilir:
```typescript
const { t } = useI18n();
t('deviceDetail.title') // "Device Detail" veya "Cihaz Detayı"
```

Translation keys:
- `deviceList.*`
- `deviceDetail.*`
- `deviceAdd.*`
- `deviceNames.*` (component isimleri)

### 10. Tema Desteği (Dark/Light Mode)
Tüm ekranlar `useThemeStore` ile tema değişikliklerini destekler:
```typescript
const isDark = useThemeStore(s => s.mode) === 'dark';
const COLORS = getThemeColors(isDark);
```

---

## Gelecek Geliştirmeler

### Öneriler
1. **DeviceListScreen API Entegrasyonu**: Mock veri yerine `devicesService.getAll()` kullan
2. **Otomatik Refresh**: SignalR veya polling ile live data otomatik güncelleme
3. **Cihaz Silme**: Cihaz silme işlevi ekle
4. **Cihaz Düzenleme**: Cihaz adı ve ayarlarını düzenleme
5. **Cihaz Paylaşımı**: Mevcut davet sistemini devices modülüne entegre et
6. **Offline Mode**: Cached data ile offline çalışma desteği
7. **Chart & Graphs**: Component verilerini grafik ile göster
8. **Notification Integration**: Cihaz uyarıları için bildirim badge'i
9. **Component Restart API**: Gerçek restart endpoint'i entegre et
10. **Error Boundary**: Component-level error handling

---

## Sorun Giderme

### Problem: DeviceDetail boş gösteriliyor
**Çözüm:**
- Site seçimi yapılmış mı kontrol et (`selectedSite`)
- Site'ın `hasDevice: true` olduğunu doğrula
- API endpoint'lerinin çalıştığını kontrol et
- Network tab'da API response'larını incele

### Problem: Live data gelmiyor
**Çözüm:**
- `deviceId` doğru mu kontrol et
- `componentId` doğru mu kontrol et
- Backend'de live data endpoint'i çalışıyor mu kontrol et
- `useDeviceLiveData` hook'unun `fetchLiveData` fonksiyonu çağrıldı mı

### Problem: Cihaz eklenemedi
**Çözüm:**
- Form validasyonu geçti mi kontrol et
- Serial number 6+ karakter mi
- Setup name 2+ karakter mi
- API response hatası varsa toast'ta gösterilir

### Problem: Admin kullanıcı Device tab'ını göremiyorumÇözüm:**
- Admin kullanıcı önce bir site seçmelidir
- Site seçimi için Admin sayfasından veya Home sayfasından site seçilmeli

---

## API Endpoint Özeti

| Endpoint | Method | Açıklama |
|----------|--------|----------|
| `/api/devices` | GET | Tüm cihazlar (pagination) |
| `/api/devices/:id` | GET | Cihaz detayı |
| `/api/devices` | POST | Yeni cihaz oluştur |
| `/api/devices/:id` | PUT | Cihaz güncelle |
| `/api/devices/:id` | DELETE | Cihaz sil |
| `/api/devicecomponents/device/:deviceId` | GET | Cihaz bileşenleri |
| `/api/setups` | POST | Yeni setup (cihaz ekle) |
| `/api/setups/setup-device?siteId=xxx` | GET | Site device bilgisi |
| `/api/mobiledashboard/setup-device?siteId=xxx` | GET | Setup device (alternatif) |
| `/api/mobiledashboard/device-with-data?deviceId=xxx` | GET | Device + components + data |
| `/api/mobiledashboard/component-live-data?deviceId=xxx&componentId=yyy` | GET | Component live data |
| `/api/sites/user-sites` | GET | Kullanıcının siteleri |

---

## Sonuç

Devices modülü, HEMS uygulamasının temel özelliklerinden biridir. Kullanıcıların enerji yönetim cihazlarını görüntülemesini, yönetmesini ve gerçek zamanlı verilere erişmesini sağlar. Modül, Zustand state yönetimi, React Navigation, Axios API client ve özel hook'lar ile güçlü bir mimari üzerine inşa edilmiştir.

**Ana Özellikler:**
- ✅ Cihaz listesi (mockup)
- ✅ Cihaz detayları (gerçek API)
- ✅ Component live data (gerçek zamanlı)
- ✅ Cihaz ekleme (QR kod desteği)
- ✅ Pull-to-refresh
- ✅ Çoklu dil desteği
- ✅ Dark/Light mode
- ✅ Admin kontrolü
- ✅ AsyncStorage persistence

**Bağlı Modüller:**
- Dashboard (Dashboard verilerini kullanır)
- Profile (Cihaz paylaşımı)
- Admin (Admin site yönetimi)
- Notifications (Cihaz bildirimleri)

**Dosya Sayısı:** 20+ dosya (screens, services, types, hooks, store)

---

**Son Güncelleme:** 2025-01-13
**Versiyon:** 1.0.0
**Yazar:** Claude Code
