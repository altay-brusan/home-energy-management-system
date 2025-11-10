# SonarQube Entegrasyon Talimatları

## 📋 Yapılacaklar

### 1. Test Projelerine `coverlet.msbuild` Paketi Ekle

Her test projesi `.csproj` dosyasına şu satırı ekleyin:

**Dosyalar:**
- `tests/Application.UnitTests/Application.UnitTests.csproj`
- `tests/Application.FunctionalTests/Application.FunctionalTests.csproj`
- `tests/Domain.UnitTests/Domain.UnitTests.csproj`
- `tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj`

**Eklenecek Kod:**
```xml
<ItemGroup>
    <!-- Mevcut paketler... -->
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>

    <!-- YENİ: Bu satırı ekleyin -->
    <PackageReference Include="coverlet.msbuild" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

**Örnek:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>Hems_Backend.Application.UnitTests</RootNamespace>
        <AssemblyName>Hems_Backend.Application.UnitTests</AssemblyName>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="nunit" />
        <PackageReference Include="coverlet.collector">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <!-- YENİ EKLENEN -->
        <PackageReference Include="coverlet.msbuild" Version="6.0.0">
          <PrivateAssets>all</PrivateAssets>
          <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
        </PackageReference>
        <PackageReference Include="FluentAssertions" />
        <PackageReference Include="Moq" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Application\Application.csproj" />
    </ItemGroup>
</Project>
```

---

### 2. Jenkinsfile Güncelle

Proje root'undaki `Jenkinsfile`'ı güncellenmiş versiyonla değiştirin.

**Yeni Jenkinsfile:** `/root/Jenkinsfile-updated`

**Değişiklikler:**
- ✅ `.NET 9.0 SDK` container eklendi
- ✅ `Build & Test with Coverage` stage eklendi
- ✅ `SonarQube Analysis` stage eklendi
- ✅ `Quality Gate` stage eklendi
- ✅ Dockerfile path düzeltildi (`-f Dockerfile`)

---

### 3. GitHub'a Commit

```bash
git add Jenkinsfile
git add tests/Application.UnitTests/Application.UnitTests.csproj
git add tests/Application.FunctionalTests/Application.FunctionalTests.csproj
git add tests/Domain.UnitTests/Domain.UnitTests.csproj
git add tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj

git commit -m "feat: Add SonarQube integration with code coverage

- Add coverlet.msbuild to test projects for coverage reports
- Update Jenkinsfile with SonarQube analysis stages
- Add Quality Gate check before deployment
- Include .NET 9.0 SDK container for testing and analysis"

git push origin dev
```

---

### 4. Jenkins Build'i İzle

1. Push sonrası Jenkins otomatik tetiklenecek
2. Build loglarını izleyin:
   - ✅ Build & Test with Coverage
   - ✅ SonarQube Analysis
   - ✅ Quality Gate
3. SonarQube UI'da sonuçları kontrol edin:
   - URL: https://sonarqube-dev.oklidis.com.tr
   - Login: admin / Admin.oklidis.1
   - Project: HEMS Backend

---

## 🎯 Beklenen Sonuç

### Pipeline Akışı (Yeni)
```
1. Checkout
2. Build & Test with Coverage     [YENİ]
3. SonarQube Analysis              [YENİ]
4. Quality Gate Check              [YENİ]
5. Build Docker Image
6. Push Docker Image
7. Deploy to K3s
```

### SonarQube Kontrolleri
- ❌ **Quality Gate FAIL:** Deploy edilmez, build fail olur
- ✅ **Quality Gate PASS:** Deploy devam eder

---

## ⚠️ Önemli Notlar

**Quality Gate İlk Başta Fail Olabilir:**
- İlk analiz: Baseline oluşturur
- Code coverage %60 altıysa fail olabilir
- Code smell'ler varsa fail olabilir

**Çözüm:**
1. SonarQube UI'da Quality Gate kurallarını gözden geçirin
2. İlk aşamada threshold'ları düşürün (örn: coverage %40)
3. Kademeli olarak artırın (%60, %70)

**Quality Gate'i Geçici Devre Dışı Bırakma:**
Eğer ilk test için Quality Gate'i bypass etmek isterseniz, Jenkinsfile'daki `Quality Gate` stage'ini geçici olarak comment out edebilirsiniz:

```groovy
// stage('Quality Gate') {
//     steps {
//         ...
//     }
// }
```

---

## 📊 SonarQube Metrikleri

Analiz sonrası görebileceğiniz metrikler:
- **Code Coverage:** %XX
- **Bugs:** X adet
- **Vulnerabilities:** X adet
- **Code Smells:** X adet
- **Duplications:** %XX
- **Technical Debt:** X gün

---

## 🐛 Sorun Giderme

### Sorun 1: "SonarQube server 'SonarQube' could not be found"
**Çözüm:** Jenkins > Manage Jenkins > System > SonarQube servers kontrol edin.

### Sorun 2: "Quality Gate timed out"
**Çözüm:** SonarQube analizi uzun sürüyor, timeout'u 10 dakikaya çıkarın:
```groovy
timeout(time: 10, unit: 'MINUTES') {
```

### Sorun 3: "dotnet sonarscanner: command not found"
**Çözüm:** .NET SDK container'ı doğru eklenmiş mi kontrol edin.

### Sorun 4: "No coverage reports found"
**Çözüm:** coverlet.msbuild paketi tüm test projelerine eklenmiş mi kontrol edin.

---

## ✅ Tamamlandı

Bu adımlar tamamlandığında:
- ✅ Her commit'te otomatik SonarQube analizi
- ✅ Code coverage tracking
- ✅ Quality gate kontrolü
- ✅ Kalitesiz kod production'a geçmez

🎉 SonarQube entegrasyonu tamamlanmış olacak!
