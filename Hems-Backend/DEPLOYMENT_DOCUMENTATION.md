# HEMS Backend - Deployment Dokümantasyonu

Bu dokümantasyon, HEMS Backend projesinin CI/CD pipeline'ı, Docker containerization süreci ve Kubernetes deployment yapısını detaylı olarak açıklamaktadır.

---

## İçindekiler

1. [Genel Mimari](#1-genel-mimari)
2. [Git Branch Stratejisi](#2-git-branch-stratejisi)
3. [Jenkins CI/CD Pipeline](#3-jenkins-cicd-pipeline)
4. [Docker Build Süreci](#4-docker-build-süreci)
5. [Docker Registry](#5-docker-registry)
6. [Kubernetes Yapılandırması](#6-kubernetes-yapılandırması)
7. [PostgreSQL Kurulumu](#7-postgresql-kurulumu)
8. [Environment Yönetimi](#8-environment-yönetimi)
9. [DNS ve Cloudflare Yapılandırması](#9-dns-ve-cloudflare-yapılandırması)
10. [ServiceAccount Authentication](#10-serviceaccount-authentication)
11. [Yeni Environment Ekleme](#11-yeni-environment-ekleme)
12. [Troubleshooting](#12-troubleshooting)

---

## 1. Genel Mimari

### Sistem Bileşenleri

```
┌─────────────┐      ┌──────────────┐      ┌─────────────┐      ┌──────────────┐
│   GitHub    │─────▶│   Jenkins    │─────▶│   Docker    │─────▶│  Kubernetes  │
│  Repository │      │   Pipeline   │      │   Registry  │      │   (K3s)      │
└─────────────┘      └──────────────┘      └─────────────┘      └──────────────┘
                                                                         │
                                                                         ▼
                                                               ┌──────────────────┐
                                                               │   PostgreSQL     │
                                                               │   + Backend Pods │
                                                               └──────────────────┘
                                                                         │
                                                                         ▼
                                                               ┌──────────────────┐
                                                               │     Traefik      │
                                                               │     Ingress      │
                                                               └──────────────────┘
                                                                         │
                                                                         ▼
                                                               ┌──────────────────┐
                                                               │   Cloudflare     │
                                                               │   DNS + Proxy    │
                                                               └──────────────────┘
```

### Sunucu Bilgileri

- **Sunucu IP**: `REDACTED_HOST`
- **Kubernetes**: K3s (lightweight Kubernetes)
- **Ingress Controller**: Traefik
- **Docker Registry**: `registry.oklidis.com.tr:5000` (Private)
- **CI/CD**: Jenkins

### Environment'lar

| Environment | Branch      | Namespace   | URL                                       | Database        |
|-------------|-------------|-------------|-------------------------------------------|-----------------|
| Development | dev         | hems-dev    | https://hems-api-dev.oklidis.com.tr       | postgres-dev    |
| Stage       | stage       | hems-stage  | https://hems-api-stg.oklidis.com.tr       | postgres-stage  |

---

## 2. Git Branch Stratejisi

### Branch Yapısı

```
main (stable)
  │
  ├─── dev (auto-deploy to dev environment)
  │
  └─── stage (auto-deploy to stage environment)
```

### Branch Kuralları

1. **main**: Stable kod, direct push yapılmaz (merge via PR)
2. **dev**: Development environment için, manuel build tetiklenir
3. **stage**: Stage environment için, manuel build tetiklenir

### Workflow

```bash
# Feature geliştirme
git checkout dev
git pull origin dev
# Kod değişiklikleri yap
git add .
git commit -m "feat: yeni özellik"
git push origin dev
# Jenkins'te manuel "Şimdi Yapılandır" butonuna tıkla

# Stage'e aktarma
git checkout stage
git merge dev
git push origin stage
# Jenkins'te manuel "Şimdi Yapılandır" butonuna tıkla
```

---

## 3. Jenkins CI/CD Pipeline

### Pipeline Dosyası

**Konum**: `Jenkinsfile` (repository root)

### Pipeline Aşamaları

#### 1. Agent Yapılandırması

Jenkins, Kubernetes içinde geçici pod'lar kullanarak build yapar:

```yaml
agent:
  kubernetes:
    serviceAccountName: jenkins-agent  # K8s native auth
    containers:
      - docker:27.0-cli       # Docker build için
      - alpine/k8s:1.28.3     # kubectl deployment için
    volumes:
      - docker-sock           # Docker daemon access
```

**Önemli**: `serviceAccountName: jenkins-agent` ile kubeconfig dosyasına ihtiyaç duymadan Kubernetes'e erişim sağlanır.

#### 2. Checkout Stage

```groovy
stage('Checkout') {
    // Git repository'den kodu çeker
    checkout scm
}
```

#### 3. Build Docker Image (Branch'e Göre)

**Development Branch:**
```groovy
stage('Build Docker Image - Development') {
    when { branch 'dev' }
    steps {
        container('docker') {
            sh """
                docker build -t registry.oklidis.com.tr:5000/hems-backend:dev-${BUILD_TAG} -f src/Web/Dockerfile .
                docker tag ... :dev-latest
            """
        }
    }
}
```

**Stage Branch:**
```groovy
stage('Build Docker Image - Stage') {
    when { branch 'stage' }
    steps {
        container('docker') {
            sh """
                docker build -t registry.oklidis.com.tr:5000/hems-backend:stage-${BUILD_TAG} -f src/Web/Dockerfile .
                docker tag ... :stage-latest
            """
        }
    }
}
```

**Build Tag Formatı**: `{branch}-{buildNumber}-{gitCommitShort}`
- `BUILD_TAG = "${env.BRANCH_NAME}-${env.BUILD_NUMBER}-${GIT_COMMIT_SHORT}"`
- Dev Örnek: `dev-2-e117c93`
- Stage Örnek: `stage-9-8818c0c`

**Final Image Tag**: `{branch}-${BUILD_TAG}`
- Dev: `registry.oklidis.com.tr:5000/hems-backend:dev-dev-2-e117c93` (branch tekrarı normal)
- Stage: `registry.oklidis.com.tr:5000/hems-backend:stage-stage-9-8818c0c` (branch tekrarı normal)

#### 4. Push Docker Image

İmajlar private registry'ye push edilir:

```groovy
docker push registry.oklidis.com.tr:5000/hems-backend:dev-${BUILD_TAG}
docker push registry.oklidis.com.tr:5000/hems-backend:dev-latest
```

#### 5. Deploy to Kubernetes

**Development:**
```groovy
stage('Deploy to K3s - Development') {
    when { branch 'dev' }
    steps {
        container('kubectl') {
            sh """
                kubectl set image deployment/hems-backend-dev \
                    hems-backend=registry.oklidis.com.tr:5000/hems-backend:dev-${BUILD_TAG} \
                    -n hems-dev

                kubectl rollout status deployment/hems-backend-dev \
                    -n hems-dev \
                    --timeout=5m
            """
        }
    }
}
```

**Stage:**
```groovy
stage('Deploy to K3s - Stage') {
    when { branch 'stage' }
    steps {
        container('kubectl') {
            sh """
                kubectl set image deployment/hems-backend-stage \
                    hems-backend=registry.oklidis.com.tr:5000/hems-backend:stage-${BUILD_TAG} \
                    -n hems-stage

                kubectl rollout status deployment/hems-backend-stage \
                    -n hems-stage \
                    --timeout=5m
            """
        }
    }
}
```

### Jenkins Dashboard

Jenkins URL: `https://jenkins-dev.oklidis.com.tr`

### Manuel Build Tetikleme

Bu projede **manuel build** stratejisi kullanılıyor. Otomatik build kapatılmıştır.

#### Build Başlatma:

1. Jenkins'e git: `https://jenkins-dev.oklidis.com.tr/job/HEMS-Backend/`
2. İlgili branch'i seç (dev veya stage)
3. **"Şimdi Yapılandır"** (Build Now) butonuna tıkla
4. Build başlayacak ve sonuç görüntülenecek

#### Multi-Branch Scan:

Yeni branch eklendiğinde veya branch silind iğinde:
1. **HEMS-Backend** ana sayfasına git
2. **"Scan Multibranch Pipeline Now"** butonuna tıkla
3. Jenkins tüm branch'leri tarayacak ve yeni/silinmiş branch'leri bulacak

**NOT**: Scan işlemi sonrası otomatik build başlamaz, manuel "Şimdi Yapılandır" ile tetiklemeniz gerekir.

### Otomatik Build'i Devre Dışı Bırakma (Opsiyonel)

Eğer otomatik build aktifse ve kapatmak istiyorsanız:

#### 1. Periyodik Taramayı Kapat:

```
Jenkins → HEMS-Backend → Configure → Scan Multibranch Pipeline Triggers
→ "Periodically if not otherwise run" → UNCHECK
→ Save
```

#### 2. GitHub Webhook'u Kontrol Et:

GitHub → Repository → Settings → Webhooks → Jenkins webhook var mı?
- Varsa: Deactivate veya "Pushes" event'ini kapat
- Sadece "Pull requests" event'i açık kalabilir

#### 3. Jenkinsfile'da Trigger Yok:

Jenkinsfile'da `triggers` bloğu yok, bu doğru. Manuel trigger kullanıyoruz.

**Sonuç**: Her push sonrası Jenkins'te manuel olarak "Şimdi Yapılandır" ile build başlatılır.

---

## 4. Docker Build Süreci

### Dockerfile Yapısı

**Konum**: `src/Web/Dockerfile`

### Multi-Stage Build

#### Stage 1: Build

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["Hems-Backend.sln", "./"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore "src/Web/Web.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR /src/src/Web
RUN dotnet build "Web.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "Web.csproj" -c Release -o /app/publish /p:UseAppHost=false
```

#### Stage 2: Runtime

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Security: non-root user
RUN adduser --disabled-password --gecos "" --uid 1000 appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
# ASPNETCORE_ENVIRONMENT will be set by Kubernetes

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Hems_Backend.Web.dll"]
```

### Önemli Noktalar

1. **Multi-stage build**: Build araçları final image'da yok (image boyutu küçük)
2. **Non-root user**: Security best practice (appuser:1000)
3. **Health check**: Container health monitoring
4. **Port 8080**: Non-root user için güvenli port
5. **ASPNETCORE_ENVIRONMENT**: Kubernetes tarafından set edilir

### Image İçeriği

Docker image şunları içerir:
- ✅ Compiled DLL'ler
- ✅ `appsettings.json`
- ✅ `appsettings.development.json`
- ✅ `appsettings.stage.json`
- ❌ Source code (yok)
- ❌ SDK tools (yok)

---

## 5. Docker Registry

### Registry Bilgileri

- **URL**: `registry.oklidis.com.tr:5000`
- **Type**: Private Docker Registry
- **Access**: Kubernetes nodes'lardan erişilebilir

### Image Tagging Stratejisi

Her build için 2 tag oluşturulur:

1. **Versioned tag**: `{branch}-{branch}-{buildNumber}-{gitCommit}`
   - Dev Örnek: `dev-dev-2-e117c93`
   - Stage Örnek: `stage-stage-9-8818c0c`
   - Rollback için kullanılır

2. **Latest tag**: `{branch}-latest`
   - Dev Örnek: `dev-latest`
   - Stage Örnek: `stage-latest`
   - Her yeni build'de güncellenir

### Registry'ye Manuel Erişim

```bash
# Sunucuda
ssh root@REDACTED_HOST

# Image listesi
curl http://registry.oklidis.com.tr:5000/v2/_catalog

# Specific image tags
curl http://registry.oklidis.com.tr:5000/v2/hems-backend/tags/list
```

---

## 6. Kubernetes Yapılandırması

### Namespace Yapısı

```bash
# Namespace'leri görüntüle
kubectl get namespaces

# hems-dev     - Development environment
# hems-stage   - Stage environment
```

### Development Environment (hems-dev)

#### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hems-backend-dev
  namespace: hems-dev
spec:
  replicas: 2
  selector:
    matchLabels:
      app: hems-backend
  template:
    metadata:
      labels:
        app: hems-backend
    spec:
      containers:
      - name: hems-backend
        image: registry.oklidis.com.tr:5000/hems-backend:dev-latest
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "development"  # ← Küçük harf!
        resources:
          requests:
            memory: "256Mi"
            cpu: "200m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

**Önemli Environment Variable**: `ASPNETCORE_ENVIRONMENT=development`
- ASP.NET Core bu değere göre `appsettings.development.json` dosyasını yükler
- Küçük harf olması önemli (case-sensitive)!

#### Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: hems-backend-dev
  namespace: hems-dev
spec:
  type: ClusterIP
  selector:
    app: hems-backend
  ports:
  - port: 80
    targetPort: 8080
    protocol: TCP
```

**ClusterIP**: Cluster içinden erişim (external IP yok)

#### Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: hems-backend-dev
  namespace: hems-dev
  annotations:
    kubernetes.io/ingress.class: traefik
spec:
  rules:
  - host: hems-api-dev.oklidis.com.tr
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: hems-backend-dev
            port:
              number: 80
```

**Traefik**: K3s'in default ingress controller'ı

### Stage Environment (hems-stage)

Yapısı development ile aynı, sadece şunlar farklı:

| Özellik          | Development                  | Stage                        |
|------------------|------------------------------|------------------------------|
| Namespace        | hems-dev                     | hems-stage                   |
| Deployment       | hems-backend-dev             | hems-backend-stage           |
| Service          | hems-backend-dev             | hems-backend-stage           |
| Ingress Host     | hems-api-dev.oklidis.com.tr  | hems-api-stg.oklidis.com.tr  |
| Environment Var  | ASPNETCORE_ENVIRONMENT=development | ASPNETCORE_ENVIRONMENT=stage |
| Image Tag        | dev-dev-*-xxx                | stage-stage-*-xxx            |

### Kubernetes Komutları

```bash
# SSH ile sunucuya bağlan
ssh root@REDACTED_HOST

# Pod'ları kontrol et
kubectl get pods -n hems-dev
kubectl get pods -n hems-stage

# Pod detayları
kubectl describe pod <pod-name> -n hems-dev

# Pod logları
kubectl logs -n hems-dev -l app=hems-backend --tail=100
kubectl logs -n hems-dev -l app=hems-backend --tail=100 -f  #Follow mode

# Deployment durumu
kubectl get deployments -n hems-dev

# Service'leri görüntüle
kubectl get svc -n hems-dev

# Ingress'leri görüntüle
kubectl get ingress -n hems-dev
kubectl describe ingress hems-backend-dev -n hems-dev

# Environment variable'ları güncelle
kubectl set env deployment/hems-backend-dev ASPNETCORE_ENVIRONMENT=development -n hems-dev

# Manuel restart (rolling restart)
kubectl rollout restart deployment/hems-backend-dev -n hems-dev

# Rollout durumu izle
kubectl rollout status deployment/hems-backend-dev -n hems-dev

# Image güncelle (manuel deployment)
kubectl set image deployment/hems-backend-dev \
    hems-backend=registry.oklidis.com.tr:5000/hems-backend:dev-dev-2-e117c93 \
    -n hems-dev

# Rollback (önceki version'a dön)
kubectl rollout undo deployment/hems-backend-dev -n hems-dev

# Rollback history
kubectl rollout history deployment/hems-backend-dev -n hems-dev

# Pod'a shell ile gir (troubleshooting)
kubectl exec -it <pod-name> -n hems-dev -- /bin/sh
```

---

## 7. PostgreSQL Kurulumu

Her environment'ın kendi PostgreSQL instance'ı var.

### Development PostgreSQL

#### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres-dev
  namespace: hems-dev
spec:
  replicas: 1
  selector:
    matchLabels:
      app: postgres-dev
  template:
    metadata:
      labels:
        app: postgres-dev
    spec:
      containers:
      - name: postgres
        image: postgres:15
        env:
        - name: POSTGRES_USER
          value: "postgres"
        - name: POSTGRES_PASSWORD
          value: "***REMOVED***"
        - name: POSTGRES_DB
          value: "HEMS"
        ports:
        - containerPort: 5432
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-dev-pvc
```

#### Service (ClusterIP)

```yaml
apiVersion: v1
kind: Service
metadata:
  name: postgres-dev
  namespace: hems-dev
spec:
  type: ClusterIP
  selector:
    app: postgres-dev
  ports:
  - port: 5432
    targetPort: 5432
```

**ClusterIP**: Sadece cluster içinden erişilebilir
- Backend pod'ları `Server=postgres-dev;Port=5432` ile bağlanır
- Kubernetes DNS otomatik resolve eder

#### Service (NodePort) - External Access

```yaml
apiVersion: v1
kind: Service
metadata:
  name: postgres-dev-nodeport
  namespace: hems-dev
spec:
  type: NodePort
  selector:
    app: postgres-dev
  ports:
  - port: 5432
    targetPort: 5432
    nodePort: 30434  # External port
```

**NodePort**: Sunucu IP'si üzerinden dışarıdan erişim
- `REDACTED_HOST:30434` ile TablePlus, pgAdmin gibi tool'lardan bağlanabilirsin

### Stage PostgreSQL

Development ile aynı yapı, farklılıklar:

| Özellik      | Development      | Stage             |
|--------------|------------------|-------------------|
| Namespace    | hems-dev         | hems-stage        |
| Service      | postgres-dev     | postgres-stage    |
| Password     | ***REMOVED***        | ***REMOVED***_stage   |
| NodePort     | 30434            | 30435             |

### PostgreSQL Bağlantı Bilgileri

#### Development (Internal - Backend'den)
```
Server: postgres-dev
Port: 5432
User: postgres
Password: ***REMOVED***
Database: HEMS
```

#### Development (External - TablePlus/pgAdmin)
```
Host: REDACTED_HOST
Port: 30434
User: postgres
Password: ***REMOVED***
Database: HEMS
```

#### Stage (Internal - Backend'den)
```
Server: postgres-stage
Port: 5432
User: postgres
Password: ***REMOVED***_stage
Database: HEMS
```

#### Stage (External - TablePlus/pgAdmin)
```
Host: REDACTED_HOST
Port: 30435
User: postgres
Password: ***REMOVED***_stage
Database: HEMS
```

### PostgreSQL Komutları

```bash
# Pod'ları görüntüle
kubectl get pods -n hems-dev | grep postgres

# PostgreSQL pod'una bağlan
kubectl exec -it postgres-dev-xxxxx -n hems-dev -- psql -U postgres -d HEMS

# Database backup
kubectl exec postgres-dev-xxxxx -n hems-dev -- pg_dump -U postgres HEMS > backup.sql

# Database restore
cat backup.sql | kubectl exec -i postgres-dev-xxxxx -n hems-dev -- psql -U postgres HEMS

# Logs
kubectl logs postgres-dev-xxxxx -n hems-dev
```

---

## 8. Environment Yönetimi

### ASP.NET Core Configuration Hierarchy

ASP.NET Core şu sırayla config dosyalarını yükler:

1. `appsettings.json` (Base configuration)
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json` (Environment-specific override)

**Örnek: Development Environment**

```
ASPNETCORE_ENVIRONMENT=development
  ↓
appsettings.json + appsettings.development.json
```

`appsettings.development.json`:
```json
{
  "ConnectionStrings": {
    "Hems_BackendDb": "Server=postgres-dev;Port=5432;User ID=postgres;Password=***REMOVED***;Database=HEMS;Pooling=true;"
  }
}
```

**Örnek: Stage Environment**

```
ASPNETCORE_ENVIRONMENT=stage
  ↓
appsettings.json + appsettings.stage.json
```

`appsettings.stage.json`:
```json
{
  "ConnectionStrings": {
    "Hems_BackendDb": "Server=postgres-stage;Port=5432;User ID=postgres;Password=***REMOVED***_stage;Database=HEMS;Pooling=true;"
  },
  "SmtpSettings": {
    "InvitationUrl": "https://hems-stg.oklidis.com.tr"
  }
}
```

### Environment Variable Ayarlama

**ASPNETCORE_ENVIRONMENT Kubernetes'te ayarlanır, Dockerfile'da değil!**

```bash
# Development
kubectl set env deployment/hems-backend-dev ASPNETCORE_ENVIRONMENT=development -n hems-dev

# Stage
kubectl set env deployment/hems-backend-stage ASPNETCORE_ENVIRONMENT=stage -n hems-stage
```

### Configuration Override Sırası

1. **Dockerfile**: Environment variable yok (kaldırıldı)
2. **Kubernetes Deployment**: `ASPNETCORE_ENVIRONMENT` set edilir
3. **Container başladığında**: ASP.NET Core bu değeri okur
4. **Config loading**: İlgili `appsettings.{environment}.json` yüklenir
5. **Database connection**: Environment'a özel PostgreSQL service'e bağlanır

### 12-Factor App Prensibi

Aynı Docker image'ı farklı ortamlarda kullanıyoruz:

```
registry.oklidis.com.tr:5000/hems-backend:dev-dev-2-e117c93
   ↓
   ├─→ hems-dev namespace:   ASPNETCORE_ENVIRONMENT=development → postgres-dev
   └─→ hems-stage namespace: ASPNETCORE_ENVIRONMENT=stage → postgres-stage
```

**NOT**: Image tag'inde branch tekrarı (`dev-dev`, `stage-stage`) normaldir. BUILD_TAG'de zaten branch name var.

**Avantaj**: Tek image, çoklu ortam (testing kolaylaşır, deploy hızlanır)

---

## 9. DNS ve Cloudflare Yapılandırması

### Cloudflare DNS Records

Cloudflare dashboard'da (oklidis.com.tr domain):

| Type | Name          | Content         | Proxy Status | TTL  |
|------|---------------|-----------------|--------------|------|
| A    | hems-api-dev  | REDACTED_HOST  | Proxied 🟠   | Auto |
| A    | hems-api-stg  | REDACTED_HOST  | Proxied 🟠   | Auto |

**Proxied (🟠 Orange Cloud)**:
- Cloudflare proxy aktif
- SSL/TLS otomatik
- DDoS protection
- CDN caching

### SSL/TLS

Cloudflare otomatik SSL sertifikası sağlar:
- `http://hems-api-dev.oklidis.com.tr` → `https://hems-api-dev.oklidis.com.tr` redirect
- Browser'da yeşil kilit simgesi

### DNS Propagation

Yeni DNS kaydı ekledikten sonra:
- Cloudflare: 2-3 dakika
- Global DNS: 15-30 dakika

Test:
```bash
# DNS resolve kontrolü
nslookup hems-api-dev.oklidis.com.tr
dig hems-api-dev.oklidis.com.tr

# Curl test
curl -I https://hems-api-dev.oklidis.com.tr/health
```

### Traffic Flow

```
User Browser
  ↓ HTTPS
Cloudflare (SSL termination + Proxy)
  ↓ HTTP
REDACTED_HOST:80 (Traefik Ingress)
  ↓
Kubernetes Service (ClusterIP)
  ↓
Backend Pod (8080)
```

---

## 10. ServiceAccount Authentication

### Neden ServiceAccount?

**Önceki Problem**: Jenkins kubeconfig dosyası kullanıyordu
- ❌ PEM certificate parsing errors
- ❌ Credential management karmaşık
- ❌ Security risk (kubeconfig file leaks)

**Yeni Çözüm**: Kubernetes ServiceAccount
- ✅ Native Kubernetes authentication
- ✅ Token-based (otomatik yönetilen)
- ✅ RBAC ile fine-grained permissions
- ✅ No credential files needed

### ServiceAccount Yapılandırması

#### 1. ServiceAccount Oluşturma

```bash
# ServiceAccount oluştur
kubectl create serviceaccount jenkins-agent -n default

# Veya YAML ile:
apiVersion: v1
kind: ServiceAccount
metadata:
  name: jenkins-agent
  namespace: default
```

#### 2. ClusterRole Oluşturma

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: jenkins-agent-role
rules:
- apiGroups: ["", "apps", "networking.k8s.io"]
  resources: ["deployments", "pods", "services", "ingresses"]
  verbs: ["get", "list", "watch", "create", "update", "patch", "delete"]
```

#### 3. ClusterRoleBinding Oluşturma

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: jenkins-agent-binding
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: jenkins-agent-role
subjects:
- kind: ServiceAccount
  name: jenkins-agent
  namespace: default
```

```bash
# Apply RBAC
kubectl apply -f jenkins-rbac.yaml
```

#### 4. Jenkinsfile'da Kullanım

```yaml
agent:
  kubernetes:
    yaml """
spec:
  serviceAccountName: jenkins-agent  # ← Bu satır!
  containers:
    - name: kubectl
      image: alpine/k8s:1.28.3
"""
```

**Önemli**: `serviceAccountName` set edilince:
- Jenkins pod'u bu ServiceAccount ile çalışır
- kubectl komutları otomatik authenticate olur
- Kubeconfig dosyasına veya `--kubeconfig` parametresine gerek yok

### ServiceAccount Kontrolü

```bash
# ServiceAccount var mı?
kubectl get serviceaccount jenkins-agent

# ServiceAccount detayları
kubectl describe serviceaccount jenkins-agent

# ServiceAccount'un token'ı
kubectl get secret $(kubectl get serviceaccount jenkins-agent -o jsonpath='{.secrets[0].name}') -o jsonpath='{.data.token}' | base64 -d
```

---

## 11. Yeni Environment Ekleme

Örnek: **Production** environment ekleme

### Adım 1: Git Branch Oluştur

```bash
git checkout main
git checkout -b production
git push origin production
```

### Adım 2: Kubernetes Namespace Oluştur

```bash
ssh root@REDACTED_HOST

kubectl create namespace hems-prod
```

### Adım 3: PostgreSQL Deployment

#### Deployment YAML

`postgres-prod.yaml`:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres-prod
  namespace: hems-prod
spec:
  replicas: 1
  selector:
    matchLabels:
      app: postgres-prod
  template:
    metadata:
      labels:
        app: postgres-prod
    spec:
      containers:
      - name: postgres
        image: postgres:15
        env:
        - name: POSTGRES_USER
          value: "postgres"
        - name: POSTGRES_PASSWORD
          value: "SECURE_PASSWORD_HERE"  # Güçlü şifre kullan!
        - name: POSTGRES_DB
          value: "HEMS"
        ports:
        - containerPort: 5432
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-prod-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-prod
  namespace: hems-prod
spec:
  type: ClusterIP
  selector:
    app: postgres-prod
  ports:
  - port: 5432
    targetPort: 5432
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-prod-nodeport
  namespace: hems-prod
spec:
  type: NodePort
  selector:
    app: postgres-prod
  ports:
  - port: 5432
    targetPort: 5432
    nodePort: 30436  # Farklı port!
```

```bash
kubectl apply -f postgres-prod.yaml
```

### Adım 4: Backend appsettings Dosyası

`src/Web/appsettings.production.json`:
```json
{
  "ConnectionStrings": {
    "Hems_BackendDb": "Server=postgres-prod;Port=5432;User ID=postgres;Password=SECURE_PASSWORD_HERE;Database=HEMS;Pooling=true;"
  },
  "SmtpSettings": {
    "InvitationUrl": "https://hems.oklidis.com.tr"
  }
}
```

```bash
git add src/Web/appsettings.production.json
git commit -m "Add production config"
git push origin production
```

### Adım 5: Backend Deployment

`hems-backend-prod.yaml`:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hems-backend-prod
  namespace: hems-prod
spec:
  replicas: 3  # Production'da daha fazla replica
  selector:
    matchLabels:
      app: hems-backend
  template:
    metadata:
      labels:
        app: hems-backend
    spec:
      containers:
      - name: hems-backend
        image: registry.oklidis.com.tr:5000/hems-backend:production-latest
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "production"  # ← Küçük harf!
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
---
apiVersion: v1
kind: Service
metadata:
  name: hems-backend-prod
  namespace: hems-prod
spec:
  type: ClusterIP
  selector:
    app: hems-backend
  ports:
  - port: 80
    targetPort: 8080
---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: hems-backend-prod
  namespace: hems-prod
  annotations:
    kubernetes.io/ingress.class: traefik
spec:
  rules:
  - host: hems-api.oklidis.com.tr
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: hems-backend-prod
            port:
              number: 80
```

```bash
kubectl apply -f hems-backend-prod.yaml
```

### Adım 6: Environment Variable Ayarla

```bash
kubectl set env deployment/hems-backend-prod ASPNETCORE_ENVIRONMENT=production -n hems-prod
```

### Adım 7: Cloudflare DNS

Cloudflare dashboard'da:
- Type: `A`
- Name: `hems-api` (veya `hems-api-prod`)
- Content: `REDACTED_HOST`
- Proxy: Enabled 🟠
- TTL: Auto

### Adım 8: Jenkinsfile Güncelle

`Jenkinsfile`'a production stage'leri ekle:

```groovy
stage('Build Docker Image - Production') {
    when {
        branch 'production'
    }
    steps {
        echo "🐳 Building Docker image for PRODUCTION environment..."
        container('docker') {
            sh """
                docker build -t ${DOCKER_REGISTRY}/${DOCKER_IMAGE_NAME}:production-${BUILD_TAG} -f src/Web/Dockerfile .
                docker tag ${DOCKER_REGISTRY}/${DOCKER_IMAGE_NAME}:production-${BUILD_TAG} ${DOCKER_REGISTRY}/${DOCKER_IMAGE_NAME}:production-latest
            """
        }
    }
}

stage('Push Docker Image - Production') {
    when {
        branch 'production'
    }
    steps {
        echo "📦 Pushing Docker image to registry (PRODUCTION)..."
        container('docker') {
            sh """
                docker push ${DOCKER_REGISTRY}/${DOCKER_IMAGE_NAME}:production-${BUILD_TAG}
                docker push ${DOCKER_REGISTRY}/${DOCKER_IMAGE_NAME}:production-latest
            """
        }
    }
}

stage('Deploy to K3s - Production') {
    when {
        branch 'production'
    }
    steps {
        echo "🚀 Deploying to K3s PRODUCTION namespace..."
        container('kubectl') {
            sh """
                kubectl set image deployment/hems-backend-prod \
                    hems-backend=${DOCKER_REGISTRY}/${DOCKER_IMAGE_NAME}:production-${BUILD_TAG} \
                    -n hems-prod

                kubectl rollout status deployment/hems-backend-prod \
                    -n hems-prod \
                    --timeout=5m
            """
        }
    }
    post {
        success {
            echo "✅ Successfully deployed to PRODUCTION environment!"
            echo "🌐 URL: https://hems-api.oklidis.com.tr"
        }
        failure {
            echo "❌ Deployment to PRODUCTION failed!"
        }
    }
}
```

```bash
git add Jenkinsfile
git commit -m "Add production pipeline stages"
git push origin production
```

### Adım 9: Test

```bash
# Pod'ları kontrol et
kubectl get pods -n hems-prod

# Ingress kontrol et
kubectl get ingress -n hems-prod

# Health check
curl https://hems-api.oklidis.com.tr/health

# Swagger UI
https://hems-api.oklidis.com.tr/api
```

---

## 12. Troubleshooting

### Problem: Pod'lar CrashLoopBackOff

**Belirti**:
```bash
kubectl get pods -n hems-dev
NAME                               READY   STATUS             RESTARTS   AGE
hems-backend-dev-xxx               0/1     CrashLoopBackOff   5          3m
```

**Çözüm**:
```bash
# Log'ları kontrol et
kubectl logs hems-backend-dev-xxx -n hems-dev

# Yaygın sebepler:
# 1. Database bağlanamıyor → Connection string yanlış
# 2. appsettings dosyası bulunamıyor → ASPNETCORE_ENVIRONMENT yanlış
# 3. Port conflict → ASPNETCORE_URLS kontrol et
```

### Problem: ImagePullBackOff

**Belirti**:
```bash
kubectl get pods -n hems-dev
NAME                               READY   STATUS             RESTARTS   AGE
hems-backend-dev-xxx               0/1     ImagePullBackOff   0          1m
```

**Çözüm**:
```bash
# Image var mı kontrol et
curl http://registry.oklidis.com.tr:5000/v2/hems-backend/tags/list

# Deployment'ta image name doğru mu?
kubectl describe pod hems-backend-dev-xxx -n hems-dev

# Jenkins build başarılı mı kontrol et
```

### Problem: Database Connection Failed

**Belirti**: Log'larda "Name or service not known" veya "connection refused"

**Çözüm**:
```bash
# 1. PostgreSQL pod çalışıyor mu?
kubectl get pods -n hems-dev | grep postgres

# 2. Service var mı?
kubectl get svc -n hems-dev | grep postgres

# 3. DNS resolve oluyor mu?
kubectl exec -it hems-backend-dev-xxx -n hems-dev -- nslookup postgres-dev

# 4. Connection string doğru mu?
kubectl exec -it hems-backend-dev-xxx -n hems-dev -- cat /app/appsettings.development.json

# 5. ASPNETCORE_ENVIRONMENT doğru mu?
kubectl describe pod hems-backend-dev-xxx -n hems-dev | grep ASPNETCORE_ENVIRONMENT
```

### Problem: Ingress Çalışmıyor (502 Bad Gateway)

**Belirti**: Browser'da 502 hatası

**Çözüm**:
```bash
# 1. Pod'lar çalışıyor mu?
kubectl get pods -n hems-dev

# 2. Service pod'ları buluyor mu?
kubectl get endpoints -n hems-dev

# 3. Ingress doğru service'i işaret ediyor mu?
kubectl describe ingress hems-backend-dev -n hems-dev

# 4. Traefik logları
kubectl logs -n kube-system -l app.kubernetes.io/name=traefik

# 5. Health check
kubectl exec -it hems-backend-dev-xxx -n hems-dev -- curl localhost:8080/health
```

### Problem: Jenkins Build Başarısız

**Belirti**: Jenkins pipeline red ❌

**Çözüm**:
```bash
# 1. Jenkins logs kontrol et (Jenkins UI'da)

# 2. ServiceAccount permissions kontrol et
kubectl describe serviceaccount jenkins-agent

# 3. Docker daemon erişilebilir mi?
# Jenkins pod içinde:
docker ps

# 4. kubectl çalışıyor mu?
# Jenkins pod içinde:
kubectl get pods

# 5. Git credentials doğru mu?
# Jenkins UI → Credentials kontrol et
```

### Problem: Environment Variable Güncellemesi Geçerli Olmadı

**Belirti**: `kubectl set env` yaptın ama eski config kullanılıyor

**Çözüm**:
```bash
# 1. Pod'ların yeniden başlatıldığından emin ol
kubectl get pods -n hems-dev -w  # Watch mode

# 2. Yeni pod'ları kontrol et
kubectl describe pod hems-backend-dev-NEW-POD -n hems-dev | grep ASPNETCORE_ENVIRONMENT

# 3. Manuel restart
kubectl rollout restart deployment/hems-backend-dev -n hems-dev

# 4. Config doğru yüklendi mi kontrol et
kubectl logs hems-backend-dev-NEW-POD -n hems-dev | grep "appsettings"
```

### Problem: DNS Resolve Edilmiyor

**Belirti**: `curl: (6) Could not resolve host`

**Çözüm**:
```bash
# 1. Cloudflare'de DNS kaydı var mı kontrol et
# Cloudflare dashboard → DNS → Records

# 2. DNS propagation bekle (2-3 dakika)

# 3. Local DNS cache temizle
# Windows: ipconfig /flushdns
# Linux: sudo systemd-resolve --flush-caches
# Mac: sudo dscacheutil -flushcache

# 4. nslookup ile test et
nslookup hems-api-dev.oklidis.com.tr

# 5. Host header ile direkt IP'ye istek at (K8s ingress test)
curl -v http://REDACTED_HOST/health -H "Host: hems-api-dev.oklidis.com.tr"
```

### Faydalı Debug Komutları

```bash
# Tüm resources'ları görüntüle
kubectl get all -n hems-dev

# Events (son olaylar)
kubectl get events -n hems-dev --sort-by='.lastTimestamp'

# Node resources
kubectl top nodes

# Pod resources
kubectl top pods -n hems-dev

# Describe ile detaylı bilgi
kubectl describe pod <pod-name> -n hems-dev
kubectl describe deployment hems-backend-dev -n hems-dev
kubectl describe service hems-backend-dev -n hems-dev
kubectl describe ingress hems-backend-dev -n hems-dev

# YAML export (backup/inspect)
kubectl get deployment hems-backend-dev -n hems-dev -o yaml > deployment-backup.yaml

# Port forward (local testing)
kubectl port-forward pod/<pod-name> 8080:8080 -n hems-dev
# Artık localhost:8080'de erişebilirsin

# Pod'a shell ile gir
kubectl exec -it <pod-name> -n hems-dev -- /bin/sh

# Multiple pod logs (tüm replica'lar)
kubectl logs -n hems-dev -l app=hems-backend --all-containers=true --tail=50

# Real-time log streaming
kubectl logs -n hems-dev -l app=hems-backend -f
```

---

## Özet: Deployment Akışı

### Normal Deployment (Git Push → Production)

```
1. Developer kodu yazar
   ↓
2. git commit && git push origin dev
   ↓
3. Jenkins'te manuel "Şimdi Yapılandır" ile build tetiklenir
   ↓
4. Jenkins:
   a. Kodu checkout eder
   b. Docker image build eder
   c. Registry'ye push eder
   d. kubectl ile K8s deployment günceller
   ↓
5. Kubernetes:
   a. Yeni image'ı pull eder
   b. Eski pod'ları terminate eder
   c. Yeni pod'ları başlatır (rolling update)
   d. Health check ile pod'ları validate eder
   ↓
6. Traefik Ingress:
   a. Yeni pod'ları backend pool'a ekler
   b. Traffic'i yeni pod'lara yönlendirir
   ↓
7. Cloudflare:
   a. DNS üzerinden traffic yönlendirir
   b. SSL termination yapar
   ↓
8. User browser'da https://hems-api-dev.oklidis.com.tr/api erişir
```

### Rollback Akışı

```bash
# 1. Önceki deployment'ları listele
kubectl rollout history deployment/hems-backend-dev -n hems-dev

# 2. Rollback yap
kubectl rollout undo deployment/hems-backend-dev -n hems-dev

# 3. Veya specific revision'a dön
kubectl rollout undo deployment/hems-backend-dev --to-revision=5 -n hems-dev

# 4. Rollout durumunu izle
kubectl rollout status deployment/hems-backend-dev -n hems-dev
```

---

## Ek Kaynaklar

### Dokümantasyon

- **Kubernetes**: https://kubernetes.io/docs/
- **Docker**: https://docs.docker.com/
- **ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/
- **Jenkins**: https://www.jenkins.io/doc/

### Proje Dökümanları

- `CLAUDE.md` - Proje genel bilgileri
- `SYSTEM_OVERVIEW.md` - Sistem mimarisi
- `APPLICATION_FEATURES_DOCUMENTATION.md` - API özellikleri
- `Jenkinsfile` - CI/CD pipeline tanımı
- `src/Web/Dockerfile` - Container build tanımı

---

**Son Güncelleme**: 2025-11-10
**Versiyon**: 1.1
**Hazırlayan**: Claude Code

**Değişiklikler (v1.1)**:
- Branch adı `development` → `dev` olarak değiştirildi
- Image tag formatı güncellendi: `dev-dev-*-xxx`, `stage-stage-*-xxx`
- Manuel build tetikleme stratejisine geçildi
- Jenkins otomatik build kapatma talimatları eklendi
