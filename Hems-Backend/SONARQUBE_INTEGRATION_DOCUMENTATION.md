# HEMS Backend - SonarQube Integration Documentation

## Table of Contents
1. [Project Overview](#project-overview)
2. [Infrastructure Preparation](#infrastructure-preparation)
3. [SonarQube Integration Steps](#sonarqube-integration-steps)
4. [Test Infrastructure Setup](#test-infrastructure-setup)
5. [Quality Gate Configuration](#quality-gate-configuration)
6. [Complete Pipeline Flow](#complete-pipeline-flow)
7. [Troubleshooting Guide](#troubleshooting-guide)
8. [Success Metrics](#success-metrics)
9. [Future Notes](#future-notes)

---

## Project Overview

### Objective
Integrate SonarQube code quality analysis into the HEMS Backend Jenkins CI/CD pipeline with automatic deployment blocking on quality gate failures.

### Technologies
- **CI/CD**: Jenkins 2.528.1
- **Code Quality**: SonarQube
- **Container Orchestration**: Kubernetes (K3s cluster)
- **Application**: .NET 9.0 SDK
- **Database**: PostgreSQL 15
- **Testing**: NUnit, Coverlet (OpenCover format)
- **Container Runtime**: Docker 27.0-cli

### Architecture
- **Sidecar Pattern**: PostgreSQL database alongside dotnet container for test isolation
- **Quality Gate Enforcement**: Blocking deployment on code quality failures
- **Multi-Environment**: Branch-based deployment (dev/stage/main)
- **Rolling Updates**: Zero downtime deployments
- **Webhook Integration**: Fast feedback from SonarQube to Jenkins

---

## Infrastructure Preparation

### 1. Infrastructure Backup (Before Changes)

**Pre-Change State:**
- HEMS Backend Deployment Resources:
  - Memory: 2Gi (request) / 4Gi (limit)
  - CPU: 1 core (request) / 2 cores (limit)
- SonarQube: Operational but not integrated with Jenkins
- Jenkins: Functional with basic build/deploy pipeline

**Backup Actions:**
```bash
# Kubernetes configurations backed up
kubectl get deployment hems-backend-dev -n hems-dev -o yaml > backup/hems-backend-dev-deployment.yaml
kubectl get deployment hems-backend-stage -n hems-stage -o yaml > backup/hems-backend-stage-deployment.yaml
```

### 2. Resource Scaling (HEMS Backend)

**Problem**: .NET 9.0 build process consuming excessive memory, causing pod evictions.

**Solution**: Increased resource limits
```yaml
resources:
  requests:
    memory: "4Gi"  # Was: 2Gi
    cpu: "2"       # Was: 1
  limits:
    memory: "8Gi"  # Was: 4Gi
    cpu: "4"       # Was: 2
```

**Applied To:**
- Development namespace: `hems-dev`
- Stage namespace: `hems-stage`

**Result**: Pod evictions eliminated, builds stable.

### 3. Sentry Error Tracking Research

**Requirement**: Implement error tracking for production monitoring.

**Research Findings:**
- SonarQube provides code quality metrics, NOT runtime error tracking
- Sentry required for production error monitoring
- Sentry integration to be handled as separate task

**Decision**: Focus on SonarQube first, Sentry integration later.

---

## SonarQube Integration Steps

### Phase 1: Server-Side Configuration (Sunucu Tarafı)

#### 1.1 Jenkins Configuration

**Access**: http://jenkins.jenkins.svc.cluster.local:8080

**SonarQube Server Configuration:**
1. Navigate to: **Manage Jenkins** → **Configure System** → **SonarQube servers**
2. Configuration:
   - Name: `SonarQube`
   - Server URL: `http://sonarqube-sonarqube.sonarqube.svc.cluster.local:9000`
   - Server authentication token: Added from SonarQube (stored in Jenkins credentials)

#### 1.2 SonarQube Configuration

**Access**: http://sonarqube-sonarqube.sonarqube.svc.cluster.local:9000

**Steps:**
1. **Authentication Token Generation**:
   - Navigate to: **User** → **My Account** → **Security** → **Generate Tokens**
   - Token Name: `Jenkins Integration Token`
   - Token added to Jenkins credentials as `Secret text`

2. **Webhook Configuration** (Critical for Quality Gate):
   - Navigate to: **Administration** → **Configuration** → **Webhooks**
   - URL: `http://jenkins.jenkins.svc.cluster.local:8080/sonarqube-webhook/`
   - Secret: (optional, not configured)
   - **Result**: Quality Gate time reduced from 15 minutes to 10-30 seconds!

3. **Quality Gate Settings**:
   - Default Quality Gate: `Sonar way`
   - Conditions:
     - Coverage on New Code > 80%
     - Duplicated Lines on New Code < 3%
     - Maintainability Rating on New Code = A
     - Reliability Rating on New Code = A
     - Security Rating on New Code = A

### Phase 2: Developer-Side Changes (Geliştirici Tarafı)

#### 2.1 Jenkinsfile Updates

**Location**: `C:\Users\ngokt\GithubRepos\Hems-Backend\Jenkinsfile`

**Major Changes:**

1. **Agent Configuration - Added PostgreSQL Sidecar**:
```groovy
agent {
    kubernetes {
        yaml """
apiVersion: v1
kind: Pod
metadata:
  labels:
    jenkins: agent
spec:
  serviceAccountName: jenkins-agent
  containers:
  - name: dotnet
    image: mcr.microsoft.com/dotnet/sdk:9.0
    command: ['cat']
    tty: true
    env:
    - name: ConnectionStrings__Hems_BackendDb
      value: "Server=localhost;Port=5432;User ID=postgres;Password=test;Database=HEMS_Test;Pooling=true;"
  - name: docker
    image: docker:27.0-cli
    command: ['cat']
    tty: true
    volumeMounts:
    - name: docker-sock
      mountPath: /var/run/docker.sock
  - name: kubectl
    image: alpine/k8s:1.28.3
    command: ['cat']
    tty: true
  - name: postgres
    image: postgres:15
    env:
    - name: POSTGRES_USER
      value: "postgres"
    - name: POSTGRES_PASSWORD
      value: "test"
    - name: POSTGRES_DB
      value: "HEMS_Test"
    ports:
    - containerPort: 5432
  volumes:
  - name: docker-sock
    hostPath:
      path: /var/run/docker.sock
        """
    }
}
```

2. **Environment Variables - SonarQube Configuration**:
```groovy
environment {
    DOCKER_REGISTRY = 'registry.oklidis.com.tr:5000'
    DOCKER_IMAGE_NAME = 'hems-backend'
    GIT_COMMIT_SHORT = sh(returnStdout: true, script: 'git rev-parse --short HEAD').trim()
    BUILD_TAG = "${env.BRANCH_NAME}-${env.BUILD_NUMBER}-${GIT_COMMIT_SHORT}"

    // SonarQube Configuration
    SONAR_PROJECT_KEY = "hems-backend"
    SONAR_HOST_URL = "http://sonarqube-sonarqube.sonarqube.svc.cluster.local:9000"
}
```

3. **New Stage: Build & Test with Coverage**:
```groovy
stage('Build & Test with Coverage') {
    steps {
        echo "🔨 Building and testing with coverage..."
        container('dotnet') {
            sh """
                dotnet restore
                dotnet build -c Release --no-restore
                dotnet test -c Release --no-build --verbosity normal \
                    /p:CollectCoverage=true \
                    /p:CoverletOutputFormat=opencover \
                    /p:CoverletOutput=./TestResults/coverage/
            """
        }
    }
    post {
        failure {
            echo "❌ Tests failed! Blocking deployment."
            error("Tests failed. Deployment is blocked.")
        }
        success {
            echo "✅ All tests passed!"
        }
    }
}
```

4. **New Stage: SonarQube Analysis**:
```groovy
stage('SonarQube Analysis') {
    steps {
        echo "📊 Running SonarQube analysis..."
        container('dotnet') {
            withSonarQubeEnv('SonarQube') {
                sh """
                    dotnet tool install --global dotnet-sonarscanner || true
                    export PATH="\$PATH:\$HOME/.dotnet/tools"

                    dotnet sonarscanner begin \
                        /k:"${SONAR_PROJECT_KEY}" \
                        /d:sonar.host.url="${SONAR_HOST_URL}" \
                        /d:sonar.token="\${SONAR_AUTH_TOKEN}" \
                        /d:sonar.cs.opencover.reportsPaths="**/TestResults/coverage/coverage.opencover.xml"

                    dotnet build -c Release --no-restore

                    dotnet sonarscanner end /d:sonar.token="\${SONAR_AUTH_TOKEN}"
                """
            }
        }
    }
}
```

5. **New Stage: Quality Gate**:
```groovy
stage('Quality Gate') {
    steps {
        echo "🚦 Waiting for SonarQube Quality Gate..."
        timeout(time: 5, unit: 'MINUTES') {
            waitForQualityGate abortPipeline: true
        }
    }
    post {
        failure {
            echo "❌ Quality Gate failed! Code quality below threshold."
        }
        success {
            echo "✅ Quality Gate passed!"
        }
    }
}
```

6. **Updated Post Actions - Cleanup Error Handling**:
```groovy
post {
    always {
        echo "🧹 Cleaning up workspace..."
        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
            deleteDir()
        }
    }
    success {
        echo "✅ Pipeline completed successfully!"
    }
    failure {
        echo "❌ Pipeline failed!"
    }
}
```

7. **Branch Name Updates**:
   - Changed all references from `'development'` to `'dev'`
   - Updated stage conditions:
     - `when { branch 'dev' }`
     - `when { branch 'stage' }`

#### 2.2 .dockerignore Updates

**Location**: `C:\Users\ngokt\GithubRepos\Hems-Backend\.dockerignore`

**Added Lines** (to prevent SonarQube artifacts in Docker images):
```
# Test projects and coverage
**/tests/
**/TestResults/
**/*.trx
**/*.coverage
**/*.coveragexml
**/*.opencover.xml

# SonarQube
**/.sonarqube/
**/.scannerwork/
```

#### 2.3 Test Project Configuration

**Added Package to Test Projects**:

Files modified:
- `tests/Application.UnitTests/Application.UnitTests.csproj`
- `tests/Domain.UnitTests/Domain.UnitTests.csproj`

Added package reference:
```xml
<PackageReference Include="coverlet.msbuild" Version="6.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

---

## Test Infrastructure Setup

### Problem: Database-Dependent Tests Failing

**Initial Test Results**:
- Total tests: 71
- Failed tests: 71 (100% failure rate)
- Primary error: PostgreSQL connection refused (127.0.0.1:5433)

**Failed Test Categories**:
1. Application.FunctionalTests: 54 tests (migration errors)
2. Infrastructure.IntegrationTests: 17 tests (PostgreSQL connection required)
3. Application.UnitTests (DB-dependent): 7 tests (mixed issues)

### Solution: Test Segregation Strategy

**Manager's Guidance**: "DevOps responsibility is infrastructure setup, not fixing test code. Move database-dependent tests out of pipeline until developers fix them."

**Implementation**:

#### 1. Created TODO_DB_REQUIRED Directory Structure

```
TODO_DB_REQUIRED/
├── Application.FunctionalTests/          (54 tests - pending migration errors)
│   ├── Application.FunctionalTests.csproj
│   ├── BaseTestFixture.cs
│   ├── CustomWebApplicationFactory.cs
│   ├── ITestDatabase.cs
│   ├── PostgreSQLTestDatabase.cs
│   ├── PostgreSQLTestcontainersTestDatabase.cs
│   ├── TestDatabaseFactory.cs
│   ├── Testing.cs
│   ├── appsettings.json
│   ├── DeviceComponents/
│   │   └── CreateDeviceComponentTests.cs
│   ├── Devices/
│   │   └── CreateDeviceTests.cs
│   ├── Setups/
│   │   └── CreateSetupTests.cs
│   ├── Sites/
│   │   ├── CreateSiteTests.cs
│   │   ├── DeleteSiteTests.cs
│   │   ├── GetSitesTests.cs
│   │   └── UpdateSiteTests.cs
│   └── TodoItems/ & TodoLists/
│       └── (various command and query tests)
│
├── Infrastructure.IntegrationTests/      (17 tests - PostgreSQL connection required)
│   ├── Infrastructure.IntegrationTests.csproj
│   └── GlobalUsings.cs
│
└── Application.UnitTests.DbTests/        (4 test files - database-dependent)
    ├── Auth/
    │   └── GetUserStatusQueryTests.cs
    ├── DeviceData/
    │   └── CreateDeviceDataCommandValidatorTests.cs
    ├── Devices/
    │   └── DeviceLoginCommandTests.cs
    └── Setups/
        └── CreateSetupCommandTests.cs
```

**Test Files in TODO_DB_REQUIRED** (examples):

1. **CreateSetupCommandTests.cs** - Database-dependent:
```csharp
[SetUp]
public async Task Setup()
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Port=5433;User ID=postgres;Password=***REMOVED***;Database=HEMS;Pooling=true;";

    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(connectionString)
        .Options;

    _context = new ApplicationDbContext(options);
    _context.Database.EnsureCreated();

    // Test site creation
    var testSite = new Site { /* ... */ };
    _context.Sites.Add(testSite);
    await _context.SaveChangesAsync();
}
```

2. **DeviceLoginCommandTests.cs** - Database-dependent:
```csharp
[SetUp]
public async Task Setup()
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Port=5433;User ID=postgres;Password=***REMOVED***;Database=HEMS;Pooling=true;";

    _context = new ApplicationDbContext(options);
    _context.Database.EnsureCreated();

    // Insert test devices
    var testDevices = new List<Device> { /* ... */ };
    _context.Devices.AddRange(testDevices);
    await _context.SaveChangesAsync();
}
```

**Comparison with Proper Mock-Based Tests**:

**CreateDeviceCommandTests.cs** (Remaining in tests/ - WORKING):
```csharp
[SetUp]
public void Setup()
{
    _context = new Mock<IApplicationDbContext>();
    _dbSet = new Mock<DbSet<Device>>();
    _user = new Mock<IUser>();
    _timeProvider = TimeProvider.System;

    _context.Setup(x => x.Devices).Returns(_dbSet.Object);
    _context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    _user.Setup(x => x.Id).Returns("test-user-id");
}
```

**Key Difference**: Mock-based tests don't need actual database connection.

#### 2. Updated Solution File

**Location**: `C:\Users\ngokt\GithubRepos\Hems-Backend\Hems-Backend.sln`

**Changes**:
```diff
-Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Application.FunctionalTests", "tests\Application.FunctionalTests\Application.FunctionalTests.csproj", "{EA6127A5-94C9-4C31-AD11-E6811B92B520}"
-EndProject
-Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Infrastructure.IntegrationTests", "tests\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj", "{01FA6786-921D-4CE8-8C50-4FDA66C9477D}"
-EndProject

+# Moved to TODO_DB_REQUIRED folder - uncomment when test DB infrastructure is ready
+#Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Application.FunctionalTests", "TODO_DB_REQUIRED\Application.FunctionalTests\Application.FunctionalTests.csproj", "{EA6127A5-94C9-4C31-AD11-E6811B92B520}"
+#EndProject
+#Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Infrastructure.IntegrationTests", "TODO_DB_REQUIRED\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj", "{01FA6786-921D-4CE8-8C50-4FDA66C9477D}"
+#EndProject
```

**Result**: `dotnet test` now only runs tests from active projects.

#### 3. Current Test Status

**Working Tests** (41 tests in `tests/` directory):
- `tests/Domain.UnitTests/`: 5 tests ✅
- `tests/Application.UnitTests/`: 36 tests ✅

**Moved Tests** (75 tests in `TODO_DB_REQUIRED/`):
- `TODO_DB_REQUIRED/Application.FunctionalTests/`: 54 tests ⏸️
- `TODO_DB_REQUIRED/Infrastructure.IntegrationTests/`: 17 tests ⏸️
- `TODO_DB_REQUIRED/Application.UnitTests.DbTests/`: 4 test files ⏸️

**Test Infrastructure Available**:
- PostgreSQL 15 sidecar container in Jenkins pod
- Connection string configured for test database
- Isolated test environment per build

**Developer Action Required**: Fix database-dependent tests to work with test infrastructure.

---

## Quality Gate Configuration

### Initial Configuration (Non-Blocking)

**Problem**: Quality Gate timing out after 15 minutes → Pipeline ABORTED.

**Root Cause**: No webhook configured → Jenkins polling SonarQube every 10 seconds.

**Initial Jenkinsfile Configuration**:
```groovy
stage('Quality Gate') {
    steps {
        timeout(time: 15, unit: 'MINUTES') {
            waitForQualityGate abortPipeline: false  // Non-blocking during setup
        }
    }
}
```

**Behavior**: Even if Quality Gate failed, pipeline would continue to deployment.

### Webhook Configuration

**Discovery**: SonarQube analysis completed in 10 seconds, but Jenkins waited 15 minutes.

**Solution**: Configured webhook in SonarQube:
- URL: `http://jenkins.jenkins.svc.cluster.local:8080/sonarqube-webhook/`
- Method: POST
- Trigger: On analysis completion

**Result**:
- Quality Gate response time: **10-30 seconds** (was 15+ minutes)
- Jenkins receives immediate callback when analysis completes
- No more polling delays

### Final Configuration (Blocking)

After webhook configuration, changed to blocking mode:

```groovy
stage('Quality Gate') {
    steps {
        echo "🚦 Waiting for SonarQube Quality Gate..."
        timeout(time: 5, unit: 'MINUTES') {
            waitForQualityGate abortPipeline: true  // BLOCKING - deployment stops on failure
        }
    }
    post {
        failure {
            echo "❌ Quality Gate failed! Code quality below threshold."
        }
        success {
            echo "✅ Quality Gate passed!"
        }
    }
}
```

**Behavior**:
- Quality Gate failure → Pipeline FAILURE → No deployment
- Quality Gate success → Continue to Docker build & deploy stages
- Timeout reduced to 5 minutes (sufficient with webhook)

---

## Complete Pipeline Flow

### Stage-by-Stage Execution

```
┌─────────────────────────────────────────────────────────────┐
│  1. CHECKOUT                                                 │
│  ─────────────────────────────────────────────────────────  │
│  • Clone repository from Git                                │
│  • Branch: dev / stage / main                               │
│  Duration: ~5-10 seconds                                    │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  2. BUILD & TEST WITH COVERAGE                              │
│  ─────────────────────────────────────────────────────────  │
│  Container: dotnet (mcr.microsoft.com/dotnet/sdk:9.0)      │
│  • dotnet restore                                           │
│  • dotnet build -c Release --no-restore                     │
│  • dotnet test with Coverlet (OpenCover format)            │
│  Output: TestResults/coverage/coverage.opencover.xml       │
│  Duration: ~2-3 minutes                                     │
│                                                              │
│  ❌ FAILURE → Pipeline ABORTED (Tests failed)               │
│  ✅ SUCCESS → Continue to SonarQube Analysis                │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  3. SONARQUBE ANALYSIS                                      │
│  ─────────────────────────────────────────────────────────  │
│  Container: dotnet                                          │
│  • dotnet tool install dotnet-sonarscanner                  │
│  • dotnet sonarscanner begin (with coverage path)           │
│  • dotnet build -c Release                                  │
│  • dotnet sonarscanner end                                  │
│  • Upload results to SonarQube server                       │
│  Duration: ~30-60 seconds                                   │
│                                                              │
│  → SonarQube processes analysis asynchronously              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  4. QUALITY GATE                                            │
│  ─────────────────────────────────────────────────────────  │
│  • Wait for webhook callback from SonarQube                 │
│  • Timeout: 5 minutes (webhook makes it ~10-30 seconds)    │
│  • Evaluate Quality Gate status                            │
│  Duration: ~10-30 seconds (with webhook)                    │
│                                                              │
│  ❌ FAILURE → Pipeline ABORTED (Quality below threshold)    │
│  ✅ SUCCESS → Continue to Docker build                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  5. BUILD DOCKER IMAGE                                      │
│  ─────────────────────────────────────────────────────────  │
│  Container: docker (27.0-cli)                               │
│  Branch-specific:                                           │
│  • dev → registry.oklidis.com.tr:5000/hems-backend:dev-*   │
│  • stage → registry.oklidis.com.tr:5000/hems-backend:stage-*│
│  Tags: {branch}-{buildNumber}-{gitCommitShort}             │
│  Also tagged: {branch}-latest                               │
│  Duration: ~3-5 minutes                                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  6. PUSH DOCKER IMAGE                                       │
│  ─────────────────────────────────────────────────────────  │
│  Container: docker                                          │
│  • docker push {branch}-{buildNumber}-{gitCommitShort}     │
│  • docker push {branch}-latest                              │
│  Duration: ~1-2 minutes                                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  7. DEPLOY TO K3S                                           │
│  ─────────────────────────────────────────────────────────  │
│  Container: kubectl (alpine/k8s:1.28.3)                    │
│  Branch-specific:                                           │
│  • dev → namespace: hems-dev                                │
│  • stage → namespace: hems-stage                            │
│  Commands:                                                  │
│  • kubectl set image deployment/{name} ...                  │
│  • kubectl rollout status (timeout: 5m)                     │
│  Duration: ~30-60 seconds                                   │
│                                                              │
│  ✅ SUCCESS:                                                │
│  • Dev: http://hems-api-dev.oklidis.com.tr                 │
│  • Stage: http://hems-api-stage.oklidis.com.tr             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  8. POST ACTIONS                                            │
│  ─────────────────────────────────────────────────────────  │
│  • Cleanup workspace (catchError to prevent false failure) │
│  • Log final status                                         │
│  Duration: ~5-10 seconds                                    │
└─────────────────────────────────────────────────────────────┘
```

### Total Pipeline Duration (Successful Run)

| Stage | Duration |
|-------|----------|
| Checkout | ~5-10s |
| Build & Test with Coverage | ~2-3 min |
| SonarQube Analysis | ~30-60s |
| Quality Gate | ~10-30s (with webhook) |
| Build Docker Image | ~3-5 min |
| Push Docker Image | ~1-2 min |
| Deploy to K3s | ~30-60s |
| Post Actions | ~5-10s |
| **TOTAL** | **~8-12 minutes** |

### Deployment URLs

| Branch | Environment | URL |
|--------|-------------|-----|
| `dev` | Development | http://hems-api-dev.oklidis.com.tr |
| `stage` | Stage | http://hems-api-stage.oklidis.com.tr |
| `main` | Production | *(Not yet configured)* |

### Swagger Documentation

- Dev: http://hems-api-dev.oklidis.com.tr/api/index.html?url=/api/specification.json
- Stage: http://hems-api-stage.oklidis.com.tr/api/index.html?url=/api/specification.json

---

## Troubleshooting Guide

### Error 1: PostgreSQL Connection Failures

**Symptoms**:
```
Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5433
System.Net.Sockets.SocketException: Connection refused
```

**Cause**: Tests expecting PostgreSQL on port 5433, but not available in Jenkins pod.

**Attempted Solutions**:
1. ❌ Test filters (still had database-dependent unit tests)
2. ❌ Fixing tests (not DevOps responsibility per manager)
3. ✅ Added PostgreSQL sidecar + moved failing tests to TODO_DB_REQUIRED/

**Current Solution**: PostgreSQL sidecar available for future test fixes.

---

### Error 2: Pending Migration Errors

**Symptoms**:
```
System.InvalidOperationException: The model for context 'ApplicationDbContext' has pending changes.
Add a new migration before updating the database.
```

**Location**: `ApplicationDbContextInitialiser.cs:line 45`

**Cause**: Entity model changes without corresponding EF Core migration.

**Solution**: Moved `Application.FunctionalTests` to `TODO_DB_REQUIRED/` - developer will fix migrations.

---

### Error 3: Solution File Build Errors (MSB3202)

**Symptoms**:
```
error MSB3202: The project file "/home/jenkins/agent/workspace/HEMS-Backend_dev/tests/Application.FunctionalTests/Application.FunctionalTests.csproj" was not found.
```

**Cause**: `.sln` file referencing moved test projects.

**Solution**: Commented out moved projects in `Hems-Backend.sln`:
```diff
-Project("{9A19103F...}") = "Application.FunctionalTests", "tests\Application.FunctionalTests\..."
+# Moved to TODO_DB_REQUIRED folder - uncomment when test DB infrastructure is ready
+#Project("{9A19103F...}") = "Application.FunctionalTests", "TODO_DB_REQUIRED\Application.FunctionalTests\..."
```

---

### Error 4: Quality Gate Timeout (15 Minutes)

**Symptoms**:
- Jenkins waits 15 minutes
- Timeout reached → Pipeline ABORTED
- SonarQube analysis actually completed in 10 seconds

**Cause**: No webhook configured → Jenkins polling SonarQube every 10 seconds.

**Detection**: User checked SonarQube server and discovered webhook was missing.

**Solution**: Added webhook in SonarQube:
```
URL: http://jenkins.jenkins.svc.cluster.local:8080/sonarqube-webhook/
```

**Result**: Quality Gate time reduced to 10-30 seconds!

---

### Error 5: Cleanup Permission Errors

**Symptoms**:
```
java.nio.file.FileSystemException: /home/jenkins/agent/workspace/HEMS-Backend_dev/.sonarqube/out/5/output-cs/telemetry.pb: Operation not permitted
Finished: FAILURE
```

**Context**: Deploy successful, but build shows FAILURE due to cleanup error.

**Cause**: SonarQube scanner creates files with restricted permissions, Jenkins can't delete them.

**Solution**: Wrapped cleanup in `catchError`:
```groovy
post {
    always {
        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
            deleteDir()
        }
    }
}
```

**Result**: Cleanup errors no longer fail the build. Build shows SUCCESS when deployment succeeds.

---

### Error 6: Branch Name Mismatch

**Symptoms**: Dev branch builds not running dev-specific stages.

**Cause**: Jenkinsfile had `when { branch 'development' }` but branch was renamed to `dev`.

**Solution**: Updated all branch references from 'development' to 'dev' throughout Jenkinsfile:
```groovy
stage('Build Docker Image - Development') {
    when {
        branch 'dev'  // Was: 'development'
    }
    // ...
}
```

---

### Error 7: Resource Exhaustion (Pod Evictions)

**Symptoms**: Jenkins build pods getting evicted during .NET build process.

**Cause**: .NET 9.0 build consuming more memory than allocated (2Gi limit).

**Solution**: Increased resource limits:
```yaml
resources:
  requests:
    memory: "4Gi"  # Was: 2Gi
    cpu: "2"       # Was: 1
  limits:
    memory: "8Gi"  # Was: 4Gi
    cpu: "4"       # Was: 2
```

**Applied To**: hems-backend-dev and hems-backend-stage deployments.

---

## Success Metrics

### Build #13 Results (First Fully Successful Build)

**Pipeline Status**: ✅ SUCCESS

**Stages**:
1. ✅ Checkout - 5s
2. ✅ Build & Test with Coverage - 2m 15s
   - 41 tests passed
   - Code coverage collected
3. ✅ SonarQube Analysis - 45s
   - Analysis uploaded successfully
4. ✅ Quality Gate - 15s (webhook callback)
   - Quality Gate: PASSED
5. ✅ Build Docker Image - Development - 4m 10s
   - Image tagged: `dev-13-5c5fdd4`
6. ✅ Push Docker Image - Development - 1m 30s
7. ✅ Deploy to K3s - Development - 45s
   - Deployment: hems-backend-dev
   - Namespace: hems-dev
   - Rolling update completed

**Total Duration**: 10 minutes 5 seconds

**Deployment URL**: http://hems-api-dev.oklidis.com.tr ✅ OPERATIONAL

**SonarQube Report**:
- Project Key: hems-backend
- Quality Gate: Passed
- Coverage: (metrics available in SonarQube dashboard)
- URL: http://sonarqube-sonarqube.sonarqube.svc.cluster.local:9000/dashboard?id=hems-backend

### Quality Improvements

**Before SonarQube Integration**:
- No code quality enforcement
- No coverage tracking
- Manual code review only
- No automated quality gates

**After SonarQube Integration**:
- ✅ Automated code quality analysis on every commit
- ✅ Code coverage tracking with Coverlet (OpenCover format)
- ✅ Quality Gate enforcement (blocks deployment on failure)
- ✅ Real-time feedback via webhook (10-30s instead of 15+ minutes)
- ✅ 41 tests running successfully in CI/CD pipeline
- ✅ SonarQube metrics available for all code

### Infrastructure Stability

**Resource Allocation**:
- HEMS Backend: 4Gi/8Gi RAM, 2/4 CPU (sufficient for .NET 9.0)
- No pod evictions since resource increase
- Stable builds across all branches

**Test Infrastructure**:
- PostgreSQL sidecar container operational
- Isolated test database per build
- Ready for developers to fix database-dependent tests

---

## Future Notes

### Immediate Actions (DevOps Complete)

- ✅ SonarQube integrated into Jenkins pipeline
- ✅ Code coverage collection with Coverlet
- ✅ Quality Gate enforcement (blocking deployment on failure)
- ✅ Webhook configured for fast feedback
- ✅ Multi-environment deployment (dev/stage)
- ✅ PostgreSQL test infrastructure available
- ✅ Resource scaling completed
- ✅ Branch naming updated (development → dev)

### Pending Developer Actions

**Database-Dependent Test Fixes** (TODO_DB_REQUIRED/):
1. Fix Application.FunctionalTests (54 tests):
   - Resolve pending migration errors
   - Update connection strings to use test database
   - Ensure proper test isolation

2. Fix Infrastructure.IntegrationTests (17 tests):
   - Update PostgreSQL connection configuration
   - Test against Jenkins PostgreSQL sidecar

3. Fix Application.UnitTests.DbTests (4 files):
   - Refactor to use mocks instead of real database
   - OR update to use test database infrastructure

**Steps to Re-enable Tests**:
1. Fix test code (developer responsibility)
2. Move fixed tests back to `tests/` directory
3. Uncomment test projects in `Hems-Backend.sln`
4. Verify tests pass in Jenkins pipeline
5. Delete `TODO_DB_REQUIRED/` folder when all tests fixed

### Future Enhancements

**Sentry Integration** (Separate Task):
- Runtime error tracking for production
- Integration with .NET 9.0 application
- Alert configuration for critical errors
- Error grouping and analysis

**Production Deployment** (main branch):
- Configure production namespace
- Set up production Docker tags
- Add manual approval gate
- Configure production URL

**Quality Gate Customization**:
- Review default Quality Gate thresholds
- Adjust coverage requirements per project needs
- Add custom rules for HEMS-specific patterns

**Test Coverage Goals**:
- Current: 41 tests passing (Database-independent)
- Target: 116 tests passing (All tests fixed)
- Coverage target: >80% (SonarQube default)

---

## File Locations and Access

### Configuration Files

| File | Location | Purpose |
|------|----------|---------|
| Jenkinsfile | `C:\Users\ngokt\GithubRepos\Hems-Backend\Jenkinsfile` | CI/CD pipeline definition |
| .dockerignore | `C:\Users\ngokt\GithubRepos\Hems-Backend\.dockerignore` | Docker build exclusions |
| Solution file | `C:\Users\ngokt\GithubRepos\Hems-Backend\Hems-Backend.sln` | .NET solution configuration |

### Test Projects

| Project | Location | Status |
|---------|----------|--------|
| Application.UnitTests | `tests/Application.UnitTests/` | ✅ 36 tests passing |
| Domain.UnitTests | `tests/Domain.UnitTests/` | ✅ 5 tests passing |
| Application.FunctionalTests | `TODO_DB_REQUIRED/Application.FunctionalTests/` | ⏸️ 54 tests (needs fixes) |
| Infrastructure.IntegrationTests | `TODO_DB_REQUIRED/Infrastructure.IntegrationTests/` | ⏸️ 17 tests (needs fixes) |
| Application.UnitTests.DbTests | `TODO_DB_REQUIRED/Application.UnitTests.DbTests/` | ⏸️ 4 files (needs fixes) |

### Service URLs

| Service | URL | Access |
|---------|-----|--------|
| Jenkins | http://jenkins.jenkins.svc.cluster.local:8080 | Cluster internal |
| SonarQube | http://sonarqube-sonarqube.sonarqube.svc.cluster.local:9000 | Cluster internal |
| HEMS Backend (Dev) | http://hems-api-dev.oklidis.com.tr | Public |
| HEMS Backend (Stage) | http://hems-api-stage.oklidis.com.tr | Public |
| Swagger UI (Dev) | http://hems-api-dev.oklidis.com.tr/api/index.html?url=/api/specification.json | Public |
| Docker Registry | registry.oklidis.com.tr:5000 | Private |

---

## Conclusion

SonarQube integration has been successfully completed for the HEMS Backend project. The CI/CD pipeline now includes:

1. ✅ Automated code quality analysis on every commit
2. ✅ Code coverage tracking with detailed reports
3. ✅ Quality Gate enforcement blocking deployments on failures
4. ✅ Fast feedback via webhook integration (10-30 seconds)
5. ✅ Stable test infrastructure with PostgreSQL sidecar
6. ✅ Multi-environment deployments with branch-based strategy
7. ✅ Proper error handling and cleanup mechanisms

**Pipeline Health**: Fully operational with 41 tests passing consistently.

**Next Steps**: Developer team to fix database-dependent tests in `TODO_DB_REQUIRED/` folder.

---

**Documentation Version**: 1.0
**Last Updated**: 2025-11-11
**Build Version**: #13 (First successful SonarQube integration)
**Author**: DevOps Team
**Project**: HEMS Backend (.NET 9.0)
