# Netzr - Next.js Web Application Backend API Documentation

Bu dokümantasyon, React Native mobil uygulamasından Next.js web uygulamasına geçiş için hazırlanmıştır. Backend API'leri, tipleri ve entegrasyon detaylarını içerir.

---

## 1. API Configuration

### Base URL
```typescript
const API_CONFIG = {
  BASE_URL: 'https://hems-api-stg.oklidis.com.tr/api',
  TIMEOUT: 30000,
  RETRY_ATTEMPTS: 3,
  RETRY_DELAY: 1000,
  TOKEN_REFRESH_THRESHOLD: 300000, // 5 dakika
};
```

### SignalR Hub URL
```typescript
const SIGNALR_HUB_URL = "https://hems-api-stg.oklidis.com.tr/deviceNotificationHub";
```

---

## 2. Authentication System

### 2.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Users/login` | POST | Kullanıcı girişi |
| `/Users/register` | POST | Kullanıcı kaydı |
| `/Users/refresh` | POST | Token yenileme |
| `/Auth/user-status` | GET | Kullanıcı durumu |
| `/Auth/mobile/get-user-status` | GET | Mobil kullanıcı durumu |
| `/Users/forgotPassword` | POST | Şifre sıfırlama isteği |
| `/Users/resetPassword` | POST | Şifre sıfırlama |
| `/Users/confirmEmail` | GET | Email onaylama |
| `/Users/resendConfirmationEmail` | POST | Onay emaili tekrar gönder |
| `/Users/userInfo2` | GET | Kullanıcı bilgileri |
| `/Users/manage/2fa` | POST | 2FA yönetimi |
| `/Users/manage/info` | GET/POST | Kullanıcı bilgi yönetimi |
| `/Users/update-profile` | PUT | Profil güncelleme |

### 2.2 Request/Response Types

```typescript
// Login
interface LoginRequest {
  email: string;
  password: string;
  twoFactorCode?: string | null;
  twoFactorRecoveryCode?: string | null;
}

interface AccessTokenResponse {
  tokenType: string;         // "Bearer"
  accessToken: string;
  expiresIn: number;         // saniye cinsinden
  refreshToken: string;
}

// Register
interface RegisterRequest {
  email: string;
  firstName: string;
  surname: string;
  password: string;
}

// Password Reset
interface ForgotPasswordRequest {
  email: string;
}

interface ResetPasswordRequest {
  email: string;
  resetCode: string;
  newPassword: string;
}

// Token Refresh
interface RefreshRequest {
  refreshToken: string;
}

// User Status
enum UserStatusType {
  Complete = 1,
  NeedsSite = 2,
  NeedsSetup = 3,
  Error = 4,
}

interface UserStatusDto {
  status: UserStatusType;
  redirectTo: string;
  message: string;
  data?: {
    siteId?: string | null;
    siteName?: string | null;
    setupId?: string | null;
    setupName?: string | null;
    deviceId?: string | null;
    deviceName?: string | null;
  } | null;
}

// User Info
interface UserInfoDTO {
  email: string;
  name?: string | null;
  surname?: string | null;
  phoneNumber?: string | null;
  roles: string[];  // ["Admin", "User", etc.]
}

// 2FA
interface TwoFactorRequest {
  enable?: boolean | null;
  twoFactorCode?: string | null;
  resetSharedKey: boolean;
  resetRecoveryCodes: boolean;
  forgetMachine: boolean;
}

interface TwoFactorResponse {
  sharedKey: string;
  recoveryCodesLeft: number;
  recoveryCodes?: string[] | null;
  isTwoFactorEnabled: boolean;
  isMachineRemembered: boolean;
}

// Profile Update
interface UpdateUserCommand {
  firstName?: string | null;
  surname?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
}
```

### 2.3 Token Management (Next.js Implementation)

```typescript
// Token storage için next-auth veya cookies kullanılabilir
// API Client örneği:

import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'https://hems-api-stg.oklidis.com.tr/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - token ekleme
apiClient.interceptors.request.use(async (config) => {
  const token = await getAccessToken(); // cookies veya next-auth'dan
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor - token yenileme
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      const refreshToken = await getRefreshToken();
      if (refreshToken) {
        try {
          const { data } = await axios.post(
            'https://hems-api-stg.oklidis.com.tr/api/Users/refresh',
            { refreshToken }
          );
          await saveTokens(data);
          error.config.headers.Authorization = `Bearer ${data.accessToken}`;
          return apiClient(error.config);
        } catch (refreshError) {
          // Logout user
          await clearTokens();
          window.location.href = '/login';
        }
      }
    }
    return Promise.reject(error);
  }
);
```

---

## 3. Sites (Evler/Lokasyonlar)

### 3.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Sites` | GET | Tüm siteleri listele (Admin) |
| `/Sites/:id` | GET | Site detayı |
| `/Sites/mobile/get-sites` | GET | Kullanıcı sitelerini getir |
| `/Sites/mobile/create-site` | POST | Site oluştur |
| `/Sites/mobile/update-site/:id` | PUT | Site güncelle |
| `/Sites/mobile/delete-site/:id` | DELETE | Site sil (Mobile) |
| `/Sites/:id` | DELETE | Site sil (Admin) |

### 3.2 Types

```typescript
interface SiteDto {
  id: string;
  userId: string;
  name: string;
  country?: string | null;
  city?: string | null;
  district?: string | null;
  addressLine?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  timezone: string;
  isDeleted: boolean;
  hasDevice: boolean;
  created: string;
  createdBy?: string | null;
  lastModified?: string | null;
  lastModifiedBy?: string | null;
  power?: string | null;
}

interface CreateSiteCommand {
  name: string;
  country?: string | null;
  city?: string | null;
  district?: string | null;
  addressLine?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  timezone: string;
}

interface UpdateSiteCommand {
  id: string;
  name: string;
  country?: string | null;
  city?: string | null;
  district?: string | null;
  addressLine?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  timezone: string;
}

interface GetSitesParams {
  UserId?: string | null;
  UserEmail?: string | null;
  userName?: string | null;
  pageNumber: number;
  pageSize: number;
  name?: string | null;
  country?: string | null;
  city?: string | null;
  addressLine?: string | null;
  district?: string | null;
}

interface SitesDashboardDto {
  totalHomes: number;
  totalDevices: number;
  totalPower: string;
  siteList: SiteWithPowerDto[];
}

interface SiteWithPowerDto extends SiteDto {
  power: string;
}
```

---

## 4. Devices (Cihazlar)

### 4.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Devices` | GET | Tüm cihazları listele |
| `/Devices/:id` | GET | Cihaz detayı |
| `/Devices` | POST | Cihaz oluştur |
| `/Devices/:id` | PUT | Cihaz güncelle |
| `/Devices/:id` | DELETE | Cihaz sil |
| `/Devices/login` | POST | Cihaz girişi |

### 4.2 Types

```typescript
enum WarrantyUnit {
  Day = 1,
  Month = 2,
  Year = 3,
}

interface DeviceDto {
  id: string;
  manufacturer: string;
  modelNumber?: string | null;
  serialNumber?: string | null;
  typeNumber?: string | null;
  qrCode?: string | null;
  specifications: any;
  isDeleted: boolean;
  installationDate?: string | null;
  calibrationDate?: string | null;
  nextCheckoutDate?: string | null;
  warrantyPeriod?: number | null;
  warrantyUnit?: WarrantyUnit | null;
  warrantyStartDate?: string | null;
  deviceComponents: DeviceComponentDto[];
  setupDevice?: any | null;
  created: string;
  createdBy?: string | null;
  lastModified?: string | null;
  lastModifiedBy?: string | null;
}

interface CreateDeviceCommand {
  manufacturer: string;
  modelNumber?: string | null;
  serialNumber?: string | null;
  typeNumber?: string | null;
  qrCode?: string | null;
  specifications: any;
  installationDate?: string | null;
  calibrationDate?: string | null;
  nextCheckoutDate?: string | null;
  warrantyPeriod?: number | null;
  warrantyUnit?: WarrantyUnit | null;
  warrantyStartDate?: string | null;
  deviceComponents: CreateDeviceComponentCommand[];
}

interface DeviceLoginCommand {
  serialNumber: string;
  qrCode?: string | null;
}

interface DeviceLoginResult {
  isSuccess: boolean;
  deviceId?: string | null;
  deviceName?: string | null;
  accessToken?: string | null;
  refreshToken?: string | null;
  expiresAt?: string | null;
  errorMessage?: string | null;
}
```

---

## 5. Device Components (Cihaz Bileşenleri)

### 5.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/DeviceComponents/device/:deviceId` | GET | Cihaza göre bileşenler |
| `/DeviceComponents/:id` | GET | Bileşen detayı |
| `/DeviceComponents` | POST | Bileşen oluştur |
| `/DeviceComponents/:id` | PUT | Bileşen güncelle |
| `/DeviceComponents/:id` | DELETE | Bileşen sil |
| `/DeviceComponents/DeviceSummary` | GET | Cihaz özeti |
| `/DeviceComponents/Details/:componentId` | GET | Bileşen detayları |

### 5.2 Types

```typescript
enum DeviceType {
  MPPS = 1,
  Invertor = 2,
  BMS = 3,
  Battery = 4,
}

interface DeviceComponentDto {
  id: string;
  deviceId: string;
  deviceType: DeviceType;
  name: string;
  manufacturer?: string;
  modelNumber?: string;
  serialNumber?: string;
  specifications: any;
  warrantyPeriod?: number;
  warrantyUnit?: WarrantyUnit;
  warrantyStartDate?: string;
  isDeleted: boolean;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
}

// Device Summary Types
type ComponentType = 'inverter' | 'battery';

interface InverterSummaryDto {
  id: string;
  type: 'inverter';
  name: string;
  model: string;
  manufacturer: string;
  serialNumber: string;
  activePowerKw: number | null;
}

interface BatteryPackDto {
  id: string;
  type: 'battery';
  name: string;
  model: string;
  manufacturer: string;
  serialNumber: string;
  stateOfCharge: number | null;
}

interface BatterySummaryDto {
  id: string;
  type: 'battery';
  name: string;
  model: string;
  manufacturer: string;
  serialNumber: string;
  stateOfCharge: number | null;
  packs: BatteryPackDto[];
}

interface DeviceSummaryResponseDto {
  components: (InverterSummaryDto | BatterySummaryDto)[];
}

interface DeviceDetailItemDto {
  key: string;
  value: any;
  unit: string | null;
  labelKey: string;
}

interface DeviceDetailsResponseDto {
  details: DeviceDetailItemDto[];
}
```

---

## 6. Dashboard

### 6.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Dashboard/mobile/get-dashboard` | GET | Ana dashboard |
| `/Dashboard/mobile/device/:deviceId/component/:componentId/data` | GET | Bileşen verileri |
| `/Dashboard/hero` | GET | Hero metrikleri |
| `/Dashboard/Soc` | GET | State of Charge |
| `/Dashboard/Soh` | GET | State of Health |
| `/Dashboard/TodayEnergySummary` | GET | Günlük enerji özeti |
| `/Dashboard/PowerAndEnergyMetrics` | GET | Güç ve enerji metrikleri |
| `/Dashboard/device/GetWithData` | GET | Cihaz ve verileri |
| `/Dashboard/device/component/getComponentLiveData` | GET | Canlı bileşen verileri |
| `/Dashboard/graphs/Components/Battery` | GET | Batarya grafik verileri |
| `/Dashboard/graphs/Components/Inverter` | GET | İnvertör grafik verileri |
| `/Dashboard/graphs/EnergyFlow` | GET | Enerji akış grafiği |
| `/Dashboard/graphs/EnergyProduction` | GET | Enerji üretim grafiği |
| `/Dashboard/graphs/EnergyConsumption` | GET | Enerji tüketim grafiği |
| `/Dashboard/graphs/PowerMetrics` | GET | Güç metrikleri grafiği |
| `/Dashboard/graphs/Battery` | GET | Batarya grafiği |
| `/Dashboard/graphs/ChargeDischarge` | GET | Şarj/deşarj grafiği |
| `/Dashboard/GridPerformance` | GET | Şebeke performansı |
| `/Dashboard/GridPerformanceSummary` | GET | Şebeke performans özeti |
| `/Dashboard/EnergyRevenue` | GET | Enerji geliri |
| `/WeatherForecasts/:siteId` | GET | Hava durumu tahmini |

### 6.2 Types

```typescript
enum AlertType {
  Info = 1,
  Warning = 2,
  Error = 3,
  Maintenance = 4,
}

enum AlertPriority {
  Low = 1,
  Medium = 2,
  High = 3,
  Critical = 4,
}

interface AlertDto {
  id: string;
  title: string;
  message: string;
  type: AlertType;
  priority: AlertPriority;
  createdAt: string;
  isRead: boolean;
}

interface SystemStatusDto {
  overallStatus: string;
  batteryLevel: number;
  powerGeneration: number;
  powerConsumption: number;
  gridImport: number;
  gridExport: number;
  operatingMode: string;
  batteryTemperatures: number[];
  ambientTemperature: number;
  fanState: string;
  lastUpdate: string;
}

interface MobileDashboardDto {
  siteId: string;
  siteName: string;
  setupId: string;
  setupName: string;
  device: DeviceOverviewDto;
  externalDevices: ExternalDeviceOverviewDto[];
  activeAlerts: AlertDto[];
  systemStatus: SystemStatusDto;
}

interface DeviceOverviewDto {
  id: string;
  name: string;
  manufacturer: string;
  modelNumber: string;
  serialNumber: string;
  qrCode: string;
  isOnline: boolean;
  lastSeen?: string | null;
  specifications: any;
  components: DeviceComponentOverviewDto[];
  warranty: WarrantyInfoDto;
}

interface WeatherForecastDto {
  date: string;
  temperature: number;
  feelsLike: number;
  humidity: number;
  pressure: number;
  windSpeed: number;
  description: string;
  icon: string;
}

// SOC Response
interface SocResponseDto {
  timestampUtc: string;
  version: number;
  deviceId: string;
  inverterBatChannelSoc: {
    channelId: number;
    socPercent: number;
  }[];
  bmsSocDetails: any[];
}

// Energy Revenue
interface EnergyRevenueDataPointDto {
  timestamp: string;
  buy: number;
  sell: number;
  net: number;
  price: number;
}

interface EnergyRevenueResponseDto {
  period: 'day' | 'week' | 'month' | 'year';
  buyPricePerKWh: number;
  sellPricePerKWh: number;
  data: EnergyRevenueDataPointDto[];
  total: {
    buyPrice: number;
    sellPrice: number;
    netPrice: number;
  };
}

// Power Metrics Query Parameters
// Day: ?deviceId=xxx&period=day&date=2025-12-03
// Week: ?deviceId=xxx&period=week&year=2025&week=49
// Month: ?deviceId=xxx&period=month&year=2025&month=12
// Year: ?deviceId=xxx&period=year&year=2025
```

---

## 7. Invitations (Davetler)

### 7.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Invitations/create` | POST | Davet oluştur |
| `/Invitations/by-site/:siteId` | GET | Site davetleri |
| `/Invitations/accept` | POST | Daveti kabul et |
| `/Invitations/decline` | POST | Daveti reddet |
| `/Invitations/:invitationId` | DELETE | Daveti sil |
| `/Invitations/remove-user` | POST | Kullanıcıyı kaldır |
| `/Invitations/user-home-sites` | GET | Kullanıcı evleri |
| `/Invitations/notifications` | GET | Bildirimler |
| `/Invitations/notifications/:notificationId/mark-read` | POST | Okundu işaretle |

### 7.2 Types

```typescript
enum InvitationStatus {
  Pending = 1,
  Accepted = 2,
  Declined = 3,
  Cancelled = 4,
}

enum UserSiteRole {
  Owner = 1,
  Guest = 2,
}

interface CreateInvitationCommand {
  email: string;
  siteId: string;
}

interface InvitationDto {
  id: string;
  email: string;
  invitedUserId?: string | null;
  invitedByUserId: string;
  invitedByUserName: string;
  siteId: string;
  siteName: string;
  token: string;
  status: InvitationStatus;
  acceptanceTime?: string | null;
  created: string;
}

interface InvitationsVm {
  withUser: InvitationDto[];      // Kayıtlı kullanıcılara giden
  withoutUser: InvitationDto[];   // Kayıtsız email adreslerine giden
}

interface AcceptInvitationCommand {
  token: string;
}

interface DeclineInvitationCommand {
  token: string;
}

interface UserHomeSitesVm {
  myHomes: UserSiteDto[];      // Kullanıcının kendi evleri
  sharedHomes: UserSiteDto[];  // Paylaşılan evler
}

interface UserSiteDto {
  id: string;
  userId: string;
  siteId: string;
  siteName: string;
  role: UserSiteRole;
}

interface NotificationDto {
  id: string;
  message: string;
  isRead: boolean;
  invitationId: string;
  created: string;
}
```

---

## 8. Device Notifications (Cihaz Bildirimleri)

### 8.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/DeviceNotifications` | GET | Tüm bildirimler |
| `/DeviceNotifications` | POST | Bildirim oluştur |
| `/DeviceNotifications/deleted` | GET | Silinen bildirimler |
| `/DeviceNotifications/types` | GET | Bildirim tipleri |
| `/DeviceNotifications/:id/mark-read` | POST | Okundu işaretle |
| `/DeviceNotifications/mark-all-read/:siteId` | POST | Tümünü okundu işaretle |
| `/DeviceNotifications/:id` | DELETE | Sil |
| `/DeviceNotifications/:id/restore` | POST | Geri yükle |
| `/DeviceNotifications/:id/permanent` | DELETE | Kalıcı sil |

---

## 9. Device Settings (Cihaz Ayarları)

### 9.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/DeviceSettings/ChangeExportStrategy` | POST | Export stratejisi değiştir |
| `/DeviceSettings/device/:deviceId/tree` | GET | Ayar ağacı |
| `/DeviceSettings/device/:deviceId/parameters` | PUT | Parametreleri güncelle |
| `/DeviceSettings/modes` | GET | Modları listele |
| `/DeviceSettings/device/:deviceId/mode` | GET | Cihaz modu |
| `/DeviceSettings/device/:deviceId/mode` | PUT | Cihaz modu güncelle |
| `/DeviceSettings/device/:deviceId/price` | GET | Cihaz fiyatı |
| `/DeviceSettings/device/:deviceId/price` | PUT | Cihaz fiyatı güncelle |

---

## 10. Locations (Lokasyonlar)

### 10.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Locations` | GET | Ülkeleri listele |
| `/Locations/cities/:countryId` | GET | Şehirleri listele |

---

## 11. Setups (Kurulumlar)

### 11.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Setups` | GET | Tüm kurulumlar |
| `/Setups/:id` | GET | Kurulum detayı |
| `/Setups/mobile/get-setup-device` | GET | Kurulum cihazı |
| `/Setups/mobile/create-setup` | POST | Kurulum oluştur |
| `/Setups/mobile/update-setup/:id` | PUT | Kurulum güncelle |
| `/Setups/mobile/delete-setup/:id` | DELETE | Kurulum sil |

---

## 12. Roles (Roller - Admin)

### 12.1 Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/RoleManagement` | GET | Rolleri listele |
| `/RoleManagement` | POST | Rol oluştur |
| `/RoleManagement/:roleName` | DELETE | Rol sil |
| `/RoleManagement/assign` | POST | Rol ata |
| `/RoleManagement/user/:userId` | GET | Kullanıcı rolleri |

---

## 13. SignalR Real-time Connection

### 13.1 Hub URL
```
wss://hems-api-stg.oklidis.com.tr/deviceNotificationHub
```

### 13.2 Connection Setup (Next.js)

```typescript
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('https://hems-api-stg.oklidis.com.tr/deviceNotificationHub', {
    accessTokenFactory: async () => {
      const token = await getAccessToken();
      return token || '';
    },
    // Web için WebSockets tercih edilebilir
    transport: HttpTransportType.WebSockets,
  })
  .withServerTimeout(120000)
  .withKeepAliveInterval(15000)
  .configureLogging(LogLevel.Warning)
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: (retryContext) => {
      return Math.min(2000 * Math.pow(2, retryContext.previousRetryCount), 30000);
    }
  })
  .build();

// Connection başlat
await connection.start();

// Site grubuna katıl
await connection.invoke('JoinSiteGroup', siteId);

// Site grubundan ayrıl
await connection.invoke('LeaveSiteGroup');
```

### 13.3 SignalR Events

```typescript
// Device Notifications
interface DeviceNotificationDto {
  id: string;
  deviceId: string;
  siteId: string;
  setupDeviceId: string;
  deviceNotificationTypeId: string;
  timestamp: string;
  additionalData: string;
  isProcessed: boolean;
  created: string;
  deviceNotificationType: {
    id: string;
    code: string;
    message: string;
    status: string;
    description: string;
    register: number;
    value: number;
    category: string;  // 'CRITICAL' | 'WARNING' | 'INFO' | 'ALARM'
    version: string;
    isActive: boolean;
  };
  deviceManufacturer: string;
  siteName: string;
}

// Power Flow (Real-time)
interface PowerFlowDto {
  deviceId: string;
  solarPowerKw: number;      // Solar güç (kW)
  gridPowerKw: number;       // Şebeke gücü (kW)
  loadPowerKw: number;       // Yük gücü (kW)
  batteryPowerKw: number;    // Batarya gücü (kW)
  generatorPowerKw: number;  // Jeneratör gücü (kW)
}

// Device Status
type DeviceStatusType = 'OK' | 'PENDING' | 'WAITING' | 'DISCONNECTED' | 'NOT_TRACKED';

interface DeviceStatusChangedDto {
  deviceId: string;
  status: DeviceStatusType;
  missedHeartbeats: number;
  timestamp: string;
  isInitialStatus?: boolean;
}

// Event Listeners
connection.on('ReceiveDeviceNotification', (notification: DeviceNotificationDto) => {
  console.log('New notification:', notification);
});

connection.on('ReceiveDeviceNotificationUpdate', (notification: DeviceNotificationDto) => {
  console.log('Notification updated:', notification);
});

connection.on('DeviceStatusChanged', (status: DeviceStatusChangedDto) => {
  console.log('Device status changed:', status);
});
```

---

## 14. Common Types

```typescript
// Pagination
interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

interface PaginationParams {
  pageNumber: number;
  pageSize: number;
}

// API Error
interface ApiError {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  message?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  url?: string;
}

// Priority Level
enum PriorityLevel {
  None = 0,
  Low = 1,
  Medium = 2,
  High = 3,
}

// Warranty Info
interface WarrantyInfoDto {
  warrantyPeriod?: number | null;
  warrantyUnit?: WarrantyUnit | null;
  warrantyStartDate?: string | null;
  warrantyEndDate?: string | null;
  daysRemaining: number;
  isExpired: boolean;
}
```

---

## 15. Next.js State Management Recommendation

### Zustand Store Pattern (Recommended)

```typescript
import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

// Auth Store
interface AuthState {
  user: UserInfoDTO | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  setUser: (user: UserInfoDTO) => void;
  clearAuth: () => void;
  hasRole: (role: string) => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      isAuthenticated: false,
      isLoading: false,

      setUser: (user) => set({ user, isAuthenticated: true }),
      clearAuth: () => set({ user: null, isAuthenticated: false }),
      hasRole: (role) => get().user?.roles?.includes(role) || false,
    }),
    {
      name: 'auth-storage',
      storage: createJSONStorage(() => localStorage),
    }
  )
);

// Sites Store
interface SitesState {
  sites: SiteDto[];
  selectedSite: SiteDto | null;
  isLoading: boolean;
  error: string | null;

  getUserSites: () => Promise<SitesDashboardDto>;
  setSelectedSite: (site: SiteDto | null) => void;
  clearSites: () => void;
}

export const useSitesStore = create<SitesState>((set, get) => ({
  sites: [],
  selectedSite: null,
  isLoading: false,
  error: null,

  getUserSites: async () => {
    set({ isLoading: true, error: null });
    try {
      const data = await sitesService.getUserSites();
      set({ sites: data.siteList, isLoading: false });
      return data;
    } catch (error: any) {
      set({ error: error.message, isLoading: false });
      throw error;
    }
  },

  setSelectedSite: (site) => {
    set({ selectedSite: site });
    if (site) {
      localStorage.setItem('selected_site', site.id);
    } else {
      localStorage.removeItem('selected_site');
    }
  },

  clearSites: () => set({ sites: [], selectedSite: null, error: null }),
}));
```

---

## 16. Service Pattern

```typescript
// services/authService.ts
import apiClient from '@/lib/apiClient';

export const authService = {
  async login(data: LoginRequest): Promise<AccessTokenResponse> {
    const response = await apiClient.post<AccessTokenResponse>('/Users/login', data);
    return response.data;
  },

  async register(data: RegisterRequest): Promise<void> {
    await apiClient.post('/Users/register', data);
  },

  async getUserInfo(): Promise<UserInfoDTO> {
    const response = await apiClient.get<UserInfoDTO>('/Users/userInfo2');
    return response.data;
  },

  async getUserStatus(): Promise<UserStatusDto> {
    const response = await apiClient.get<UserStatusDto>('/Auth/user-status');
    return response.data;
  },

  async refreshToken(refreshToken: string): Promise<AccessTokenResponse> {
    const response = await apiClient.post<AccessTokenResponse>('/Users/refresh', { refreshToken });
    return response.data;
  },

  async forgotPassword(email: string): Promise<void> {
    await apiClient.post('/Users/forgotPassword', { email });
  },

  async resetPassword(data: ResetPasswordRequest): Promise<void> {
    await apiClient.post('/Users/resetPassword', data);
  },
};

// services/sitesService.ts
export const sitesService = {
  async getUserSites(): Promise<SitesDashboardDto> {
    const response = await apiClient.get<SitesDashboardDto>('/Sites/mobile/get-sites');
    return response.data;
  },

  async getById(id: string): Promise<SiteDto> {
    const response = await apiClient.get<SiteDto>(`/Sites/${id}`);
    return response.data;
  },

  async create(data: CreateSiteCommand): Promise<string> {
    const response = await apiClient.post<string>('/Sites/mobile/create-site', data);
    return response.data;
  },

  async update(id: string, data: UpdateSiteCommand): Promise<void> {
    await apiClient.put(`/Sites/mobile/update-site/${id}`, data);
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/Sites/mobile/delete-site/${id}`);
  },
};

// services/dashboardService.ts
export const dashboardService = {
  async getDashboard(): Promise<MobileDashboardDto> {
    const response = await apiClient.get<MobileDashboardDto>('/Dashboard/mobile/get-dashboard');
    return response.data;
  },

  async getDeviceSoc(deviceId: string): Promise<number | null> {
    const response = await apiClient.get<SocResponseDto>('/Dashboard/Soc', {
      params: { deviceId }
    });
    if (response.data.inverterBatChannelSoc?.length > 0) {
      return response.data.inverterBatChannelSoc[0].socPercent;
    }
    return null;
  },

  async getPowerAndEnergyMetrics(
    deviceId: string,
    period: 'day' | 'week' | 'month' | 'year',
    dateOrYear: string,
    weekOrMonth?: number
  ): Promise<any> {
    let url = `/Dashboard/PowerAndEnergyMetrics?deviceId=${deviceId}&period=${period}`;

    if (period === 'day') {
      url += `&date=${dateOrYear}`;
    } else if (period === 'week') {
      url += `&year=${dateOrYear}&week=${weekOrMonth}`;
    } else if (period === 'month') {
      url += `&year=${dateOrYear}&month=${weekOrMonth}`;
    } else {
      url += `&year=${dateOrYear}`;
    }

    const response = await apiClient.get(url);
    return response.data;
  },

  async getWeatherForecast(siteId: string): Promise<WeatherForecastDto[]> {
    const response = await apiClient.get<WeatherForecastDto[]>(`/WeatherForecasts/${siteId}`);
    return response.data;
  },

  async getEnergyRevenue(
    deviceId: string,
    period: 'day' | 'week' | 'month' | 'year',
    dateOrYear: string,
    weekOrMonth?: number
  ): Promise<EnergyRevenueResponseDto> {
    let url = `/Dashboard/EnergyRevenue?deviceId=${deviceId}&period=${period}`;

    if (period === 'day') {
      url += `&date=${dateOrYear}`;
    } else if (period === 'week') {
      url += `&year=${dateOrYear}&week=${weekOrMonth}`;
    } else if (period === 'month') {
      url += `&year=${dateOrYear}&month=${weekOrMonth}`;
    } else {
      url += `&year=${dateOrYear}`;
    }

    const response = await apiClient.get<EnergyRevenueResponseDto>(url);
    return response.data;
  },
};
```

---

## 17. Suggested Next.js Project Structure

```
src/
├── app/                          # Next.js App Router
│   ├── (auth)/                   # Auth layout group
│   │   ├── login/
│   │   ├── register/
│   │   ├── forgot-password/
│   │   └── reset-password/
│   ├── (dashboard)/              # Dashboard layout group
│   │   ├── dashboard/
│   │   ├── sites/
│   │   ├── devices/
│   │   ├── settings/
│   │   └── profile/
│   ├── layout.tsx
│   └── page.tsx
├── components/
│   ├── ui/                       # UI components (shadcn/ui recommended)
│   ├── charts/                   # Chart components
│   ├── dashboard/
│   ├── devices/
│   └── shared/
├── hooks/
│   ├── useAuth.ts
│   ├── useSites.ts
│   ├── useDashboard.ts
│   ├── useSignalR.ts
│   └── usePowerFlow.ts
├── lib/
│   ├── apiClient.ts              # Axios instance
│   ├── signalR.ts                # SignalR connection
│   └── utils.ts
├── services/
│   ├── authService.ts
│   ├── sitesService.ts
│   ├── dashboardService.ts
│   ├── devicesService.ts
│   └── invitationsService.ts
├── store/
│   ├── authStore.ts
│   ├── sitesStore.ts
│   ├── dashboardStore.ts
│   └── signalRStore.ts
├── types/
│   ├── auth.types.ts
│   ├── sites.types.ts
│   ├── devices.types.ts
│   ├── dashboard.types.ts
│   └── signalR.types.ts
└── middleware.ts                 # Auth middleware
```

---

## 18. Deep Linking Support

Mobil uygulamada kullanılan deep link pattern:
```
n2nenergymobile://invite?code=XXX
```

Web için:
```
https://yourdomain.com/invite?code=XXX
```

---

## 19. User Roles

Sistemde kullanılan roller:
- `Admin`: Tam yönetici erişimi
- `User`: Standart kullanıcı
- `Guest`: Paylaşılan ev misafiri

Role checking:
```typescript
const { hasRole, hasAnyRole, hasAllRoles } = useAuthStore();

// Tek rol kontrolü
if (hasRole('Admin')) { ... }

// Herhangi biri kontrolü
if (hasAnyRole(['Admin', 'User'])) { ... }

// Tümü kontrolü
if (hasAllRoles(['Admin', 'User'])) { ... }
```

---

## 20. Error Handling Pattern

```typescript
interface ApiError {
  status: number;
  title: string;
  detail?: string;
  message?: string;
  errors?: Record<string, string[]>;
}

// Backend hata mesajları
const errorTranslations: Record<string, string> = {
  'lockedout': 'Hesabınız kilitlendi',
  'failed': 'İşlem başarısız',
  'not found': 'Bulunamadı',
  'invalidcredentials': 'Geçersiz kimlik bilgileri',
  'usernotfound': 'Kullanıcı bulunamadı',
  'emailalreadyexists': 'Bu email zaten kayıtlı',
  'passwordtooweak': 'Şifre çok zayıf',
  'tokenexpired': 'Oturum süresi doldu',
  'networkerror': 'Ağ hatası',
  'servererror': 'Sunucu hatası',
  'validationerror': 'Doğrulama hatası',
  'unauthorized': 'Yetkisiz erişim',
  'forbidden': 'Erişim engellendi',
  'timeout': 'İstek zaman aşımına uğradı',
  'ratelimit': 'Çok fazla istek',
};
```

---

Bu dokümantasyon, Next.js web uygulamasının geliştirilmesi için gerekli tüm backend API bilgilerini içermektedir. Herhangi bir sorunuz olursa, mevcut React Native kodlarına referans verilebilir.
