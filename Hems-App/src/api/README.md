# API Documentation

## 📁 Klasör Yapısı

```
src/api/
├── client/
│   ├── apiClient.ts          # Axios instance + interceptors
│   └── config.ts              # API URL, endpoints, timeout
├── types/
│   ├── common.types.ts        # Ortak tipler (PaginatedList, Error, Enums)
│   ├── auth.types.ts          # Auth API tipleri
│   ├── sites.types.ts         # Sites API tipleri
│   ├── setups.types.ts        # Setups API tipleri
│   ├── devices.types.ts       # Devices API tipleri
│   └── dashboard.types.ts     # Dashboard API tipleri
├── services/
│   ├── authService.ts         # Auth endpoints
│   ├── sitesService.ts        # Sites endpoints
│   ├── setupsService.ts       # Setups endpoints
│   ├── devicesService.ts      # Devices endpoints
│   └── dashboardService.ts    # Dashboard endpoints
└── index.ts                   # Tüm export'lar
```

## 🚀 Kullanım Örnekleri

### **1. Login (Auth Store)**

```typescript
import { useAuthStore } from '@/store/authStore';

const LoginScreen = () => {
  const { login, isLoading, error } = useAuthStore();

  const handleLogin = async () => {
    try {
      await login({
        email: 'user@example.com',
        password: 'password123',
      });
      // Başarılı! Token otomatik kaydedildi
      navigation.navigate('Dashboard');
    } catch (error) {
      // Hata toast ile gösterilecek
      toast.error(error.detail || 'Login failed');
    }
  };

  return <Button onPress={handleLogin} loading={isLoading} />;
};
```

### **2. Get User Sites (Sites Store)**

```typescript
import { useSitesStore } from '@/store/sitesStore';
import { useEffect } from 'react';

const SitesScreen = () => {
  const { sites, getUserSites, isLoading } = useSitesStore();

  useEffect(() => {
    getUserSites();
  }, []);

  return <FlatList data={sites} />;
};
```

### **3. Get Dashboard (Dashboard Store)**

```typescript
import { useDashboardStore } from '@/store/dashboardStore';

const DashboardScreen = () => {
  const { dashboard, getDashboard, isLoading } = useDashboardStore();

  useEffect(() => {
    getDashboard();
  }, []);

  return (
    <View>
      <Text>{dashboard?.siteName}</Text>
      <Text>{dashboard?.systemStatus.batteryLevel}%</Text>
    </View>
  );
};
```

### **4. Direct Service Call (Servisler)**

```typescript
import { authService, sitesService } from '@/api';

// Auth service
const userStatus = await authService.getUserStatusMobile();

// Sites service
const userSites = await sitesService.getUserSites();

// Dashboard service
const dashboard = await dashboardService.getDashboard();
```

## 🔐 Authentication Flow

### **Token Management:**

1. **Login:**
   - User girişi → `authStore.login()`
   - Token otomatik AsyncStorage'a kaydedilir
   - User bilgisi store'a kaydedilir

2. **Auto Token Refresh:**
   - Her API isteğinde token kontrolü
   - Token expire olmadan 5 dakika önce auto refresh
   - Refresh token ile yeni access token alınır

3. **Logout:**
   - `authStore.logout()`
   - Tüm tokenlar temizlenir
   - Store sıfırlanır

### **Token Interceptor:**

```typescript
// Her request otomatik token ekler
headers: {
  Authorization: `Bearer ${accessToken}`
}

// 401 hatası → Auto token refresh
if (status === 401 && !retry) {
  const newToken = await refreshToken();
  retry request with new token;
}
```

## 📝 Type Safety

Tüm API call'ları type-safe:

```typescript
// ✅ Type-safe
const response: AccessTokenResponse = await authService.login({
  email: string,      // Type checked
  password: string,   // Type checked
});

// ✅ Auto-complete
const sites: SiteDto[] = await sitesService.getUserSites();
sites[0].name      // Auto-complete çalışır
sites[0].timezone  // Auto-complete çalışır
```

## 🛠 Error Handling

### **Global Error Interceptor:**

```typescript
// API Client otomatik error handle eder
try {
  await authService.login(data);
} catch (error: ApiError) {
  // error.status   → HTTP status code
  // error.title    → Error başlığı
  // error.detail   → Hata detayı
  // error.errors   → Validation hataları
  toast.error(error.detail);
}
```

### **Validation Errors:**

```typescript
catch (error: ApiError) {
  if (error.errors) {
    // Field-specific errors
    Object.entries(error.errors).forEach(([field, messages]) => {
      console.log(`${field}: ${messages.join(', ')}`);
    });
  }
}
```

## 🔄 Store Patterns

### **Loading States:**

```typescript
const { isLoading } = useAuthStore();
<Button loading={isLoading} />
```

### **Error States:**

```typescript
const { error, clearError } = useSitesStore();
useEffect(() => {
  if (error) {
    toast.error(error);
    clearError();
  }
}, [error]);
```

### **Data Caching:**

```typescript
// Zustand otomatik cache eder
const { sites } = useSitesStore();
// İlk call → API'den gelir
// Sonraki render → Store'dan gelir
```

## 📊 Available Stores

| Store | Dosya | Amaç |
|-------|-------|------|
| `useAuthStore` | `authStore.ts` | Login, Register, User yönetimi |
| `useSitesStore` | `sitesStore.ts` | Site CRUD operations |
| `useDashboardStore` | `dashboardStore.ts` | Dashboard data |

## 🎯 Next Steps

1. **Toast Library Ekle (Optional):**
   ```bash
   npm install react-native-toast-message
   ```

2. **API Base URL (.env):**
   ```env
   API_BASE_URL=https://hems-api.pentaunity.com/api
   ```

3. **Login Screen Entegrasyonu:**
   - `useAuthStore` kullan
   - `login()` fonksiyonunu çağır
   - Error handling ekle

4. **Dashboard Screen Entegrasyonu:**
   - `useDashboardStore` kullan
   - `getDashboard()` çağır
   - Real data göster

