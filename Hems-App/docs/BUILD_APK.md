# Android APK Build Guide

Bu döküman, N2N Energy Mobile (HEMS) uygulamasının Android APK çıktısını nasıl alacağınızı açıklar.

## Gereksinimler

- **Node.js**: >= 20
- **React Native CLI**: 0.81.1
- **Android Studio**: En son sürüm
- **JDK**: 17 veya üzeri
- **Android SDK**: API Level 34 (veya üzeri)
- **Gradle**: 8.14.3 (proje ile birlikte gelir)

## Build Tipleri

### 1. Debug APK (Geliştirme için)

Debug APK, geliştirme ve test için kullanılır. İmzalanmamış ve optimizasyonsuz olduğu için boyutu daha büyüktür.

```bash
cd android
./gradlew assembleDebug
```

**Çıktı konumu:**
```
android/app/build/outputs/apk/debug/app-debug.apk
```

### 2. Release APK (Dağıtım için)

Release APK, production ortamı için optimize edilmiş ve minified versiyondur.

```bash
cd android
./gradlew assembleRelease
```

**Çıktı konumu:**
```
android/app/build/outputs/apk/release/app-release.apk
```

**Boyut:** ~71 MB

## Adım Adım Build İşlemi

### 1. Temiz Build (İlk Kez veya Sorun Varsa)

```bash
# Metro bundler'ı temizle
npm start -- --reset-cache

# Android build cache'ini temizle
cd android
./gradlew clean

# APK oluştur
./gradlew assembleRelease
cd ..
```

### 2. Hızlı Build (Günlük Kullanım)

```bash
cd android
./gradlew assembleRelease
```

### 3. Build Loglarını İnceleme

Build sırasında hata alırsanız, verbose mode'da çalıştırın:

```bash
cd android
./gradlew assembleRelease --stacktrace --info
```

## APK İmzalama (Signing)

Play Store'a yüklemek veya production'da dağıtmak için APK'nın imzalanması gerekir.

### 1. Keystore Oluşturma

```bash
keytool -genkeypair -v -storetype PKCS12 -keystore my-release-key.keystore -alias my-key-alias -keyalg RSA -keysize 2048 -validity 10000
```

### 2. Signing Konfigürasyonu

`android/gradle.properties` dosyasına ekleyin:

```properties
MYAPP_RELEASE_STORE_FILE=my-release-key.keystore
MYAPP_RELEASE_KEY_ALIAS=my-key-alias
MYAPP_RELEASE_STORE_PASSWORD=****
MYAPP_RELEASE_KEY_PASSWORD=****
```

### 3. Build Config Güncellemesi

`android/app/build.gradle` dosyasında:

```gradle
android {
    ...
    signingConfigs {
        release {
            if (project.hasProperty('MYAPP_RELEASE_STORE_FILE')) {
                storeFile file(MYAPP_RELEASE_STORE_FILE)
                storePassword MYAPP_RELEASE_STORE_PASSWORD
                keyAlias MYAPP_RELEASE_KEY_ALIAS
                keyPassword MYAPP_RELEASE_KEY_PASSWORD
            }
        }
    }
    buildTypes {
        release {
            signingConfig signingConfigs.release
            minifyEnabled true
            proguardFiles getDefaultProguardFile('proguard-android.txt'), 'proguard-rules.pro'
        }
    }
}
```

### 4. İmzalı APK Oluşturma

```bash
cd android
./gradlew assembleRelease
```

## Android App Bundle (AAB) Oluşturma

Play Store için AAB formatı önerilir (daha küçük boyut):

```bash
cd android
./gradlew bundleRelease
```

**Çıktı konumu:**
```
android/app/build/outputs/bundle/release/app-release.aab
```

## Sık Karşılaşılan Sorunlar

### 1. "Execution failed for task ':app:mergeReleaseResources'"

**Çözüm:**
```bash
cd android
./gradlew clean
./gradlew assembleRelease
```

### 2. "SDK location not found"

**Çözüm:** `android/local.properties` dosyasına Android SDK yolunu ekleyin:
```properties
sdk.dir=C\:\\Users\\YourUsername\\AppData\\Local\\Android\\Sdk
```

### 3. Metro bundler hatası

**Çözüm:**
```bash
npm start -- --reset-cache
# Yeni terminal'de:
cd android
./gradlew assembleRelease
```

### 4. Out of memory hatası

**Çözüm:** `android/gradle.properties` dosyasına ekleyin:
```properties
org.gradle.jvmargs=-Xmx4096m -XX:MaxMetaspaceSize=512m
```

## APK Test Etme

### Emulator'da Test

```bash
# Emulator'u başlat
emulator -avd YOUR_AVD_NAME

# APK'yı yükle
adb install android/app/build/outputs/apk/release/app-release.apk
```

### Fiziksel Cihazda Test

1. Cihazı USB ile bağlayın
2. USB debugging'i aktif edin
3. APK'yı yükleyin:

```bash
adb install android/app/build/outputs/apk/release/app-release.apk
```

## Build Optimizasyonu

### 1. ProGuard/R8 Minification

Release build'de otomatik olarak aktiftir. Ek kurallar için `android/app/proguard-rules.pro` dosyasını düzenleyin.

### 2. Enable Hermes

Hermes engine varsayılan olarak aktiftir. Devre dışı bırakmak için `android/gradle.properties`:

```properties
hermesEnabled=false
```

### 3. ABI Split

Farklı CPU mimarileri için ayrı APK'lar oluşturmak için `android/app/build.gradle`:

```gradle
android {
    splits {
        abi {
            enable true
            reset()
            include "armeabi-v7a", "arm64-v8a", "x86", "x86_64"
            universalApk true
        }
    }
}
```

## CI/CD Entegrasyonu

### GitHub Actions Örneği

```yaml
name: Build Android APK

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Set up Node.js
        uses: actions/setup-node@v2
        with:
          node-version: '20'
      - name: Install dependencies
        run: npm install
      - name: Build APK
        run: |
          cd android
          ./gradlew assembleRelease
      - name: Upload APK
        uses: actions/upload-artifact@v2
        with:
          name: app-release
          path: android/app/build/outputs/apk/release/app-release.apk
```

## Versiyonlama

Version bilgisi için `android/app/build.gradle`:

```gradle
android {
    defaultConfig {
        versionCode 1
        versionName "1.0.0"
    }
}
```

Her release için:
- `versionCode`: Integer olarak artırın (1, 2, 3, ...)
- `versionName`: Semantic versioning (1.0.0, 1.0.1, 1.1.0, ...)

## Dosya Boyutu Analizi

APK içeriğini analiz etmek için:

```bash
# APK Analyzer ile
studio.sh android/app/build/outputs/apk/release/app-release.apk

# veya command line ile
apkanalyzer apk file-size android/app/build/outputs/apk/release/app-release.apk
```

## Kaynaklar

- [React Native Android Build Docs](https://reactnative.dev/docs/signed-apk-android)
- [Android App Bundle](https://developer.android.com/guide/app-bundle)
- [Gradle Build Tool](https://docs.gradle.org/)
