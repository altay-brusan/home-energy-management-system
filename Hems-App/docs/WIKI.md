# N2N Energy Mobile · Teknik Wiki

Bu döküman, React Native tabanlı N2N Energy Mobile uygulamasının teknik mimarisini, bileşenlerini ve geliştirme süreçlerini profesyonel bir formatta özetler. Amacı yeni ekip üyelerinin projeye hızla adapte olmasını sağlamak ve mevcut geliştiricilere referans sunmaktır.

## 1. Genel Bakış
- **Amaç:** Enerji yönetimi ve cihaz izleme süreçlerini mobil ortamda yönetmek, kullanıcıları gerçek zamanlı olaylar konusunda bilgilendirmek ve idari operasyonları desteklemek.
- **Hedef Kitle:** Son kullanıcılar (konut sahipleri), saha ekipleri ve geniş yetkiye sahip yönetici kullanıcılar.
- **Çekirdek Modüller:** Gösterge paneli (dashboard), cihaz detay ve onboarding akışları, bildirim merkezi, profil yönetimi, yönetici paneli, enerji profili ayarları, arıza ve destek senaryoları.
- **Çapraz Platform:** React Native `0.81.1` ve React `19.1.0` ile iOS ve Android platformlarını aynı kod tabanından hedefler.

## 2. Teknoloji Yığını & Dizin Yapısı
- **Dil & Çatılar:** TypeScript, React Native, React Navigation, Zustand, SignalR, React Hook Form, Zod.
- **UI & Grafik:** Victory Native, d3-tabanlı çizimler, NativeWind uyumlu stiller, özel komponent kütüphanesi (`src/components`).
- **Durum & Veri:** Zustand store'ları, AsyncStorage persistansı, RESTful servis katmanı, SignalR gerçek zamanlı bağlantı.

Proje kök dizininde öne çıkan klasörler:

- `src/App.tsx`: Uygulamanın giriş noktası, sağlayıcı konfigürasyonları ve global UI bileşenleri.
- `src/app/`: Ekranlar, navigasyon (`routes`), layout bileşenleri.
- `src/api/`: Axios istemcisi, domain bazlı servisler ve tip tanımları.
- `src/store/`: Tüm global durumlar (auth, siteler, dashboard, temalar vb.) için Zustand store dosyaları.
- `src/hooks/`: Modüler iş mantıkları (SignalR, site seçimleri, roller, formlar, senkronizasyon).
- `src/components/`: UI kütüphanesi, form alanları, geri bildirim bileşenleri, QR tarayıcı, network wrapper.
- `src/lib/`: Uluslararasılaştırma (`i18n`), depolama yardımcıları, HTTP hata mapleyicileri ve doğrulama yardımcıları.
- `src/services/`: SignalR gibi framework dışı servis katmanları.
- `src/theme/`: Açık/koyu tema renk paletleri ve spacing tipografisi.

## 3. Navigasyon Mimarisı
- **Kök Navigasyon:** `src/app/routes/index.tsx` dosyasında `React Navigation` tabanlı `NavigationContainer` ve `createNativeStackNavigator` yapılandırması yer alır.
  - **Public Stack:** `Login`, `SignUp`, `ForgotPassword`, `ResetPassword` ekranları.
  - **Protected Stack:** Giriş sonrası `MainTabs`, `ProfileInfo`, cihaz onboarding, bildirim detayları, enerji profili vb. ekranlar.
- **Sekme Navigasyonu:** `TabNavigator`, kullanıcı rolüne göre `Dashboard`, `Device`, `Notifications`, `Profile` ve yalnızca adminlerde görünen `Admin` sekmesini yönetir. Seçilen site bilgisine göre tab erişimleri dinamik olarak güncellenir.
- **Yönlendirme Yardımcıları:** `navigationRef`, `navigate`, `replace` fonksiyonları modüller arası gezinmeyi kolaylaştırır.
- **Derin Bağlantı:** `src/App.tsx` içinde `Linking` üzerinden `n2nenergymobile://invite` şemasını dinleyen bir `useEffect` bulunur. Davet kodu tespit edildiğinde `useInvitationStore` üzerinden saklanır ve kullanıcı oturum açtığında açılır pencere tetiklenir.

## 4. Durum Yönetimi & Persistans
Uygulama genelindeki durumlar `Zustand` ile yönetilir ve kritik state'ler `AsyncStorage` üzerinde kalıcı hale getirilir.

| Store | Konum | Sorumluluklar |
| --- | --- | --- |
| `useAuthStore` | `src/store/authStore.ts` | Kullanıcı bilgisi, token durumu, rol kontrolleri, `AsyncStorage` persistansı. |
| `useSitesStore` | `src/store/sitesStore.ts` | Kullanıcı siteleri, seçili site/saha cihazı, `sitesService` entegrasyonu, seçili siteyi saklama. |
| `useDashboardStore` | `src/store/dashboardStore.ts` | Dashboard veri önbelleği, yükleme/flagr hataları, grafik verileri çekme. |
| `useInvitationStore` | `src/store/invitationStore.ts` | Davet kodu saklama, modal görünürlüğü, `AsyncStorage` üzerinden yeniden yükleme. |
| `useToastStore`, `useAlertStore`, `useErrorStore` | `src/store/` | Global toast, kritik uyarılar, hata panosu. |
| `useI18nStore`, `useThemeStore` | `src/store/` | Dil ve tema tercihlerini saklama. |

Ek olarak `src/store/modestorage.ts`, `onboardingStore.ts`, `faultsStore.ts` gibi domain odaklı store dosyaları da mevcuttur ve ilgili servisler üzerinden veri çeker.

Persist edilen başlıca anahtarlar:
- Auth erişim/yenileme token'ları ve kullanıcı objesi (`src/lib/storage/token.ts`).
- Seçili site ve siteye ait cihaz bilgileri (`useSitesStore`).
- Dil tercihi (`useI18nStore`).
- Bekleyen davet kodu (`useInvitationStore`).

## 5. Ağ Katmanı & API Servisleri
- **Axios İstemcisi:** `src/api/client/apiClient.ts` dosyası, `API_CONFIG` (base URL, timeout, retry) ile yapılandırılmış bir `AxiosInstance` üretir.
  - Her istekte access token başlığa eklenir.
  - Token süresi dolarken otomatik refresh akışı, eşzamanlı istekleri kuyrukta bekletir (`performTokenRefresh`).
  - `AbortController` listesi sayesinde logout veya sayfa değişiminde bekleyen istekler iptal edilir.
  - Global hatalarda `mapToAppError` ile kullanıcıya dost mesajlar üretilir, `toast` aracılığıyla bilgilendirme yapılır.
  - `registerGlobalRefreshCallback`/`updateGlobalRefreshCallback` ile token yenileme sonrası dashboard gibi ekranların otomatik tazelenmesi sağlanır.
- **Servis Katmanı:** `src/api/services` dizininde auth, site, cihaz, cihaz paylaşımı, dashboard, hava durumu, parameter grupları gibi domain bazlı servisler bulunur.
  - Her servis ilgili endpoint sabitlerini `src/api/client/config.ts` üzerinden kullanır.
  - `src/api/types` dizininde type-safe DTO ve komut tipleri (ör. `SiteDto`, `MobileDashboardDto`, `DeviceNotificationDto`) tanımlıdır.
- **Yeniden Kullanılabilir Yardımcılar:** `src/api/index.ts` ile servis ve tip dışa aktarımları yapılır; `src/api/README.md` proje içi kullanım örnekleri sunar.

## 6. Gerçek Zamanlı Bildirimler & Kritik Alarm Akışı
- **SignalR Servisi:** `src/services/signalRService.ts` `@microsoft/signalr` ile hub bağlantısı kurar, site bazlı gruplara katılır ve otomatik yeniden bağlanma stratejisi uygular.
- **Hook & Provider:** `src/hooks/useSignalR.ts` kritik olayları dinleyip `useAlertStore` üzerinden uyarı tetikler. `src/providers/SignalRProvider.tsx` ise bu hook'u bağlamaya çevirir ve global `SignalRContext` sağlar.
- **UI Tepkisi:**
  - `GlobalAlert` (`src/components/feedback/GlobalAlert/GlobalAlert.tsx`), alarm seviyesine göre stil değiştirir ve kullanıcıya öneriler sunar.
  - `GlobalError` (`src/components/feedback/GlobalError/GlobalError.tsx`) beklenmedik hataları gösterir.
  - `Toast` (`src/components/feedback/Toast/Toast.tsx`) durum mesajlarını kısa süreli bildirir.
- **Kullanım Senaryosu:** Cihaza ait kritik bir bildirim geldiğinde SignalR, `DeviceNotificationDto` nesnesini iletir; hook bu veriyi `criticalNotifications` listesine ekler ve arayüzde kullanıcıyı yönlendirir.

## 7. Uluslararasılaştırma & Tema Yönetimi
- **Dil Yönetimi:** `src/lib/i18n/index.ts` dosyası i18next kurulumu yapar, cihaz dilini otomatik algılar ve `en`/`tr` kaynaklarını yükler. `useI18nStore` (persist) ve `useI18n` hook'u bileşenlerde kullanım kolaylığı sağlar.
- **Tema:** `useThemeStore` açık/koyu modu yönetir, `getThemeColors` (`src/theme/constants.ts`) renk paletlerini döndürür. Çoğu bileşen `COLORS` sözlüğü üzerinden dinamik stil üretir.
- **Offline Deneyimi:** `src/app/screens/offline/OfflineScreen.tsx` çok dilli içerik ve animasyon desteğiyle bağlantı yokken kullanıcıyı bilgilendirir; `NetworkWrapper` (`src/components/NetworkWrapper/NetworkWrapper.tsx`) bağlantı durumuna göre offline ekranını gösterir.

## 8. Formlar, Doğrulama & UI Bileşenleri
- **Form Altyapısı:** `react-hook-form` + `@hookform/resolvers/zod` ikilisi kullanılır. Örnekler `src/app/screens/login/LoginScreen.tsx`, `src/app/screens/onboarding/HomeSetupScreen.tsx`, `src/app/screens/profile/ChangePassword/ChangePasswordScreen.tsx`.
- **Şema Tanımları:** `zod` ile form doğrulama şemaları oluşturulur (`src/app/screens/login/login.schema.ts`, `src/app/screens/onboarding/home.schema.ts`, `src/lib/validation/zod-helpers.ts`).
- **Form Bileşenleri:** `Input`, `IconInput`, `Button`, `Card`, `Badge`, `ThemeToggle` gibi tasarım bileşenleri `src/components` altında gruplanır.
- **QR Kod Tarama:** `src/components/QrScannerModal/QrScannerModal.tsx` Vision Camera entegrasyonu ile QR kodlarını okur, onboarding sırasında cihaz seri numarası vb. girişini hızlandırır.

## 9. Başlıca İşlevsel Modüller
- **Dashboard (`src/app/screens/dashboard/DashboardScreen.tsx`):** Enerji akışı grafikleri, batarya metrikleri, cihaz listeleri, hava durumu ve mod kartları içerir. `useDashboardRefresh` ile bileşen bazlı yenilenebilir yapı sağlanır.
- **Cihaz Detay & Yönetimi:** `src/app/screens/deviceDetail` ve `src/app/screens/devices` klasörleri cihaz bileşenleri, parametre yönetimleri ve yeni cihaz ekleme formlarını barındırır.
- **Bildirim Ekosistemi:** `src/app/screens/notification` ve `.../notificationDetail` klasörleri okuma/işaretleme, çöp kutusu (`src/app/screens/trashscreen/TrashScreen.tsx`) ve SignalR bildirimlerine ilişkin UI sunar.
- **Profil Alanı:** Şifre değişimi, şirket bilgileri, cihaz paylaşımı (`DeviceSharing`), arıza kaydı (`FaultsScreen`, `FaultDetailScreen`) ve destek rehberi (`TroubleShootingScreen`) alt modüllerini içerir.
- **Onboarding:** `src/app/screens/onboarding/HomeSetupScreen.tsx` coğrafi konum tespiti (`react-native-geolocation-service`) ve adres bilgisi toplarken; `DeviceSetupScreen.tsx` cihaz bağlantısı ve QR tarama akışı sağlar.
- **Admin Paneli (`src/app/screens/adminPage/AdminPage.tsx`):** Yönetici kullanıcılar için ülke/şehir bazlı site filtreleme, arama, istatistikler ve kullanıcı evlerine geçiş (tab navigasyonu ile dashboard'a yönlendirme) sunar.
- **Enerji Profili & Parametreler:** `src/app/screens/energyProfile` klasörü batarya seviyeleri, güneş önceliği gibi ayar ekranlarını ve ilgili servis çağrılarını kapsar.

## 10. Yerel Yetkinlikler & Donanım Entegrasyonu
- **Kamera:** `react-native-vision-camera` QR okuma için kullanılır, `useCodeScanner` ile yüksek performanslı tarama yapılır.
- **Konum:** `react-native-geolocation-service` ile ev kurulumunda veya bölge seçimlerinde konum önerisi yapılır.
- **Ağ Durumu:** `@react-native-community/netinfo` tabanlı `useNetInfo` hook'u bağlantı değişimlerini dinler; offline ekranı ve servis çağrılarını korunaklı hale getirir.
- **URL Polyfill:** SignalR ile uyum için `react-native-url-polyfill` `src/services/signalRService.ts` içinde adapte edilmiştir.

## 11. Davet & Derin Bağlantı Akışı
1. Kullanıcı davet linkine tıkladığında (`n2nenergymobile://invite?code=...`) `Linking` modülü `App.tsx` içinde `handleDeepLink` fonksiyonunu tetikler.
2. Kod `useInvitationStore.setPendingInvitationCode` ile saklanır ve `AsyncStorage`'a yazılır.
3. Kullanıcı oturum açtığında `RootNavigator` içindeki `useEffect` modalı açar (`InvitationModal` bileşeni).
4. Kabul işlemi `invitationsService.acceptInvitation` ile API'ye iletilir; ardından `useSitesStore.getUserSites` çağrısı ile site listesi tazelenir.
5. Reddedilen kodlar `clearPendingInvitation` ile temizlenir.

## 12. Geliştirme & Çalıştırma
- **Ön Koşullar:** Node.js `>=20`, iOS için Xcode + CocoaPods, Android için Java SDK & Android Studio. Gerekli mobil ortam kurulumu için React Native resmi dokümantasyonu takip edilmeli.
- **Kurulum:**
  ```bash
  npm install
  npm start          # Metro bundler
  npm run ios        # iOS simulator (CocoaPods kurulumunu unutmayın)
  npm run android    # Android emulator/device
  ```
- **Betikler:** `npm run lint` (ESLint), `npm test` (Jest), `npx pod-install` (postinstall).
- **Ortam Değişkenleri:** `API_CONFIG.BASE_URL` değeri `https://hems-api.pentaunity.com/api` olarak yapılandırılmıştır. Gerekirse `.env` veya `react-native-dotenv` ile override edilebilir.
- **Kod Standartları:** ESLint & Prettier ile uyumlu; store ve servis isimlendirmelerinde domain bazlı yaklaşım izlenir.

## 13. Test & Kalite
- **Jest:** Varsayılan test `__tests__/App.test.tsx` React bileşeninin render edip etmediğini doğrular.
- **Önerilen Genişletmeler:** Store fonksiyonları için unit testler, SignalR entegrasyonu için mock tabanlı testler, form doğrulama şemaları için edge-case senaryoları önerilir.
- **Gözden Geçirme:** Yeni modül eklerken `tsconfig` path alias'larına dikkat edilmeli; `useDashboardRefresh` veya `apiClient` gibi merkezi yardımcılar tekrar kullanılmalıdır.

## 14. Genişletme Rehberi
1. **Yeni API Uç Noktası:**
   - `src/api/client/config.ts` içine endpoint ekleyin.
   - `src/api/services` altında domain'e özel servis fonksiyonu yazın.
   - Gerekirse ilgili `types` dosyasına DTO ekleyin.
2. **Yeni Store:**
   - `src/store` altında domain adını taşıyan bir dosya oluşturun.
   - Hook ile kullanım kolaylığı sağlayın (`useXStore`).
   - Persist gerekiyorsa `persist` middleware'i ve `AsyncStorage` anahtarlarını tanımlayın.
3. **Yeni Ekran:**
   - `src/app/screens/<modül>` dizininde ekranı oluşturun.
   - Navigasyon erişimi gerekiyorsa `RootNavigator` veya `TabNavigator` içine ekleyin.
   - Tema ve dil desteğini `useThemeStore`, `useI18n` hook'ları ile entegre edin.

## 15. Sorun Giderme & Operasyonel Notlar
- **Token Süresi Dolması:** `apiClient` yenileme denemelerini otomatik yapar; başarısız olursa `onRefreshFailureCallback` tetiklenir ve kullanıcıdan tekrar giriş yapması beklenir.
- **Ağ Hataları:** `mapToAppError` fonksiyonu ağ kopukluklarını `NETWORK` kodu ile belirler, kullanıcıya tekrar deneme önerilir.
- **SignalR Bağlantısı:** Bağlantı koptuğunda servis otomatik yeniden bağlanmayı dener; `SignalRStatus` bileşeni (dashboard altı) bu durumu gözlemlemek için kullanılabilir.
- **Offline Senaryoları:** `NetworkWrapper` offline durumlarda kritik flow'ları durdurur. Manuel retry için `onRetry` callback'i sağlanabilir.
- **Kullanıcı Rolü:** Sekme erişimleri `useRoles` ile kontrol edilir. Admin yetkisi olmayan kullanıcılar Admin sekmesini göremez; bu logic `TabNavigator` içinde anahtar olarak `navigatorKey` ile cache edilir.

## 16. Ek Kaynaklar
- React Native resmi dokümanları: <https://reactnative.dev/docs/getting-started>
- React Navigation: <https://reactnavigation.org/docs/getting-started>
- Zustand: <https://docs.pmnd.rs/zustand/getting-started/introduction>
- Microsoft SignalR: <https://learn.microsoft.com/aspnet/core/signalr>
- React Hook Form & Zod: <https://react-hook-form.com/> · <https://zod.dev/>

---
Bu wiki dökümanı sürekli güncellenmelidir. Yeni modüller eklendiğinde veya mimari değişiklikler yapıldığında ilgili bölümleri lütfen revize edin; böylece ekip içi bilgi paylaşımı güncel kalır.
