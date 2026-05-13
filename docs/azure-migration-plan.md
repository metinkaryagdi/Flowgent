# Azure Migration Plan — .NET Mikroservis Kanban Projesi

> **Durum:** Taslak — sonra detaylı incelenecek.
> **Tarih:** 2026-05-13
> **Kapsam:** BFF, Identity, Project, Issue, Notification, AI, Sprint servisleri + YARP + RabbitMQ + Redis + Outbox Pattern.
> **Hedef:** Yerel Docker ortamından Azure bulut ekosistemine aşamalı geçiş.

---

## Özet ve Karar Notu (Thesis Bağlamı)

**Tam migration thesis kapsamında mantıksız, kısmi/showcase migration mantıklı.**

- 20 mikroservis + AKS + SQL/PostgreSQL Flexible + Service Bus + Redis kombinasyonu **ayda ~80-120 USD**.
- Azure for Students 100 USD kredi **3-4 haftada biter**.
- Mevcut Docker Compose setup'ı zaten çalışıyor (210/210 test geçiyor).
- Migration sırasında çıkacak network/secret/migration bug'ları **2-3 hafta kaybettirir**.

**Önerilen hibrit yaklaşım:**
1. Migration planını (bu doküman) thesis ekine koy — production thinking kanıtı.
2. Tek servis + frontend'i showcase olarak Azure'a deploy et (Container Apps veya tek node AKS, ~15-20 USD/ay).
3. Ana sistem lokal/Docker kalsın.

---

## Phase 1 — Hazırlık ve Altyapı

### 1.1 Azure for Students Kısıtları ve Resource Group Stratejisi

Azure for Students hesabı **100 USD kredi + 12 ay ücretsiz katman** sağlar. Kritik kısıtlar:
- Sponsorship kapsamında **AKS node SKU**'ları sınırlı (B-serisi, D2s_v3'e kadar pratik)
- Premium tier servisler (Service Bus Premium, SQL Hyperscale) kredi tüketimini hızlandırır
- Bölge seçimi: **West Europe** veya **North Europe** — latency + quota durumu en iyisi

**Resource Group hiyerarşisi** (environment + lifecycle bazlı ayrım):

```
rg-kanban-shared-weu       → ACR, Key Vault, Log Analytics, App Insights
rg-kanban-data-weu         → Azure SQL/PostgreSQL servers, Cache for Redis
rg-kanban-platform-weu     → AKS cluster, VNet, NSG, Public IP
rg-kanban-messaging-weu    → Service Bus namespace (veya RabbitMQ VMSS)
rg-kanban-dev-weu          → Dev-only kaynaklar (silmesi kolay olsun)
```

**Naming convention:** `{type}-{project}-{component}-{env}-{region}` örn: `sql-kanban-issue-prod-weu`.

**Tag stratejisi** (cost tracking için zorunlu):
```
Environment=dev|prod
Service=issue|project|identity|...
CostCenter=thesis
Owner=metin
```

Subscription seviyesinde **Azure Policy** ile tag enforcement aç.

### 1.2 Azure Container Registry (ACR) Kurulumu

**SKU seçimi:** Basic (10 GB) — 20 mikroservis × ~200 MB layer cache için yeterli. Premium gerekmiyor.

**Kurulum adımları:**

```powershell
# 1. ACR oluştur
az acr create `
  --resource-group rg-kanban-shared-weu `
  --name acrkanbanprod `
  --sku Basic `
  --admin-enabled false

# 2. AKS → ACR pull yetkisi (managed identity ile)
az aks update `
  --resource-group rg-kanban-platform-weu `
  --name aks-kanban-prod `
  --attach-acr acrkanbanprod

# 3. ACR Tasks ile multi-stage build (CI dışı yedek)
az acr task create `
  --registry acrkanbanprod `
  --name build-issue-service `
  --image issue-service:{{.Run.ID}} `
  --context https://github.com/metinkaryagdi/BitirmeProject.git `
  --file src/services/Issue/Dockerfile `
  --git-access-token <PAT>
```

**Image tagging stratejisi:**
- `{service}:{git-sha}` — immutable, production deploy
- `{service}:latest` — sadece dev branch
- `{service}:{semver}` — release tag'leri

**Retention policy:**
```powershell
az acr config retention update `
  --registry acrkanbanprod `
  --status enabled `
  --days 30 `
  --type UntaggedManifests
```

---

## Phase 2 — Veri ve Durum Yönetimi

### 2.1 Database Migration Stratejisi (Database-per-Service)

| Servis | DB Engine | Azure Service | Tier (Dev) | Tier (Prod) | Gerekçe |
|---|---|---|---|---|---|
| Identity | MSSQL | Azure SQL DB | Basic 5 DTU | S1 20 DTU | ASP.NET Identity şemasıyla doğal uyum |
| Project | PostgreSQL | Azure DB for PostgreSQL Flexible | B1ms | GP D2s_v3 | EF Core + JSONB kullanımı varsa |
| Issue | PostgreSQL | Flexible Server | B1ms | GP D2s_v3 | En yoğun trafikli servis |
| Sprint | PostgreSQL | Flexible Server | B1ms | B2s | Görece düşük yazma |
| Notification | PostgreSQL | Flexible Server | B1ms | B1ms | Outbox + ephemeral data |
| AI | PostgreSQL (+ vector) | Flexible Server + `pgvector` | B2s | GP D2s_v3 | Embedding storage için |

**Migration adımları (servis başına):**

1. **Şema senkronizasyonu:** EF Core migrations `dotnet ef database update --connection "{azure-conn-string}"` ile uygula. CI içinde idempotent migration step ekle.

2. **Veri aktarımı seçenekleri:**
   - **Küçük tablolar (<100K satır):** `pg_dump | pg_restore` veya `bacpac` export/import
   - **Büyük tablolar:** Azure Database Migration Service (DMS) — online mode ile zero-downtime
   - **Outbox tablosu:** Migration sırasında **dondur**, taşıma sonrası publisher'ı tek instance ile başlat

3. **Connection string yönetimi:**
   - Asla appsettings.json'a yazma
   - **Key Vault** + Managed Identity + `Azure.Extensions.AspNetCore.Configuration.Secrets` paketi
   - Format: `Server={server}.postgres.database.azure.com;Database=issue_db;Port=5432;User Id={app-identity};Ssl Mode=Require;`

4. **Backup & PITR:**
   - Flexible Server'da default 7 gün PITR yeterli
   - Identity DB için 14 gün (compliance)

5. **Private endpoint zorunlu:** Public access kapat, AKS subnet'inden private link ile bağlan.

**Cache for Redis:**
- **Basic C0 (250 MB)** thesis için yeterli
- Distributed cache + SignalR backplane için tek instance
- Mevcut Redis client kodunu değiştirmeye gerek yok

### 2.2 RabbitMQ vs Azure Service Bus Karşılaştırması

| Kriter | Azure Service Bus (Standard) | AKS üzerinde RabbitMQ (StatefulSet) |
|---|---|---|
| **Yönetim yükü** | Tam yönetimli, sıfır operasyon | Cluster mgmt, disk, upgrade, monitoring sizde |
| **Maliyet (dev)** | ~10 USD/ay base + msg ücreti | 1 node = AKS pool maliyeti (paylaşımlı) |
| **Maliyet (prod 1M msg/gün)** | ~30-40 USD/ay | Disk + node maliyeti, ~50+ USD |
| **MassTransit uyumu** | Native transport mevcut | AMQP 0-9-1 ile mevcut kod değişmez |
| **Dead Letter Queue** | Native, otomatik | Manuel queue + policy |
| **Throughput** | Standard: ~1000 msg/s, Premium: yüksek | Node SKU'ya göre, 10K+ msg/s |
| **Mesaj boyutu** | Standard 256 KB, Premium 100 MB | Default 128 MB |
| **Mevcut kodu değiştirme** | MassTransit transport değişimi gerekir | **Sıfır kod değişikliği** |

**Önerim:** Thesis kapsamında **Service Bus Standard**. Sebepleri:
1. MassTransit transport swap tek satır
2. AKS node bütçesini servislere ayır, broker'a verme
3. Dead letter, scheduled messages, sessions native
4. Premium'a upgrade path mevcut, kod değişmez

**Alternatif:** RabbitMQ'da kalmak istersen Azure Marketplace'ten **CloudAMQP** managed servisi al — operasyon yükü olmaz.

**Topic/Queue topology:**
```
sb-kanban-prod (namespace)
├── topics/
│   ├── issue-events       → subs: notification, ai, sprint
│   ├── project-events     → subs: issue, notification
│   └── identity-events    → subs: project, notification
└── queues/
    ├── ai-enrichment-requests  (Sprint AI analytics)
    └── notification-outbox-dlq
```

---

## Phase 3 — Orkestrasyon ve Dağıtım

### 3.1 AKS Cluster Minimum Gereksinimleri

**20 mikroservis için node sizing hesabı:**
- Servis pod'u tipik: 200m CPU request / 256 Mi memory request
- 20 servis × 1 replica (dev) = 4 CPU / 5 GB
- Production'da kritik servisler 2 replica → 6 CPU / 8 GB
- System pods (CoreDNS, kube-proxy, metrics-server, ingress, cert-manager) ~1.5 CPU / 2 GB

**Önerilen node pool yapısı:**

```yaml
# System pool (taint: CriticalAddonsOnly=true:NoSchedule)
nodepool: system
  vmSize: Standard_B2s        # 2 vCPU, 4 GB
  minCount: 1, maxCount: 2
  
# Application pool
nodepool: apps
  vmSize: Standard_D2s_v3     # 2 vCPU, 8 GB
  minCount: 2, maxCount: 4    # cluster autoscaler
  
# AI pool (opsiyonel — AI servis bp-agent için)
nodepool: ai
  vmSize: Standard_D4s_v3     # 4 vCPU, 16 GB
  minCount: 0, maxCount: 1
  taints: ["workload=ai:NoSchedule"]
```

**Cluster kurulum komutu:**

```powershell
az aks create `
  --resource-group rg-kanban-platform-weu `
  --name aks-kanban-prod `
  --kubernetes-version 1.29.0 `
  --node-resource-group rg-kanban-aks-nodes-weu `
  --enable-managed-identity `
  --network-plugin azure `
  --network-policy calico `
  --enable-addons monitoring,azure-keyvault-secrets-provider `
  --workload-identity-enabled `
  --oidc-issuer-enabled `
  --enable-cluster-autoscaler `
  --node-count 2 --min-count 2 --max-count 4 `
  --node-vm-size Standard_D2s_v3 `
  --vnet-subnet-id /subscriptions/.../subnets/aks-nodes `
  --pod-cidr 10.244.0.0/16 `
  --service-cidr 10.0.0.0/16 `
  --dns-service-ip 10.0.0.10 `
  --attach-acr acrkanbanprod
```

**Kritik addon'lar:**
- **Workload Identity** (Pod Identity v2): Key Vault, SQL, Service Bus erişimi
- **Azure Monitor for Containers**: Log + metric collection
- **CSI driver for Key Vault**: Secret mount

### 3.2 YARP + Ingress Controller Entegrasyonu

**Seçenek A — YARP'i Ingress olarak kullan (önerilen):**

```
[Internet] → [Azure Public IP] → [Service: LoadBalancer]
              → [YARP Pod (2 replica)] 
              → [BFF / Identity / Project / ... services (ClusterIP)]
```

YARP'in `appsettings.json` route config'ini ConfigMap olarak mount et:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: yarp-routes
data:
  appsettings.json: |
    {
      "ReverseProxy": {
        "Routes": {
          "identity": {
            "ClusterId": "identity-cluster",
            "Match": { "Path": "/api/identity/{**catch-all}" }
          },
          "issue": {
            "ClusterId": "issue-cluster",
            "Match": { "Path": "/api/issue/{**catch-all}" }
          }
        },
        "Clusters": {
          "identity-cluster": {
            "Destinations": {
              "d1": { "Address": "http://identity-service.default.svc.cluster.local/" }
            }
          }
        }
      }
    }
```

**Seçenek B — NGINX Ingress + YARP'i BFF'in içinde tut:**
NGINX TLS termination + rate limiting, BFF içinde YARP business-level routing. Çift hop var ama sorumluluk net.

**TLS + Custom Domain:**
```powershell
# cert-manager + Let's Encrypt
helm install cert-manager jetstack/cert-manager `
  --namespace cert-manager --create-namespace `
  --set installCRDs=true

# DNS: Azure DNS Zone'a A record
az network dns record-set a add-record `
  --resource-group rg-kanban-shared-weu `
  --zone-name kanban.app `
  --record-set-name "@" `
  --ipv4-address <ingress-public-ip>
```

### 3.3 Outbox Publisher — Sidecar Pattern

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: issue-service
spec:
  replicas: 2
  template:
    spec:
      serviceAccountName: issue-workload-sa  # Workload Identity
      containers:
      
      # Main container — API
      - name: api
        image: acrkanbanprod.azurecr.io/issue-service:abc123
        ports: [{ containerPort: 8080 }]
        env:
        - name: ConnectionStrings__IssueDb
          valueFrom: { secretKeyRef: { name: issue-secrets, key: db-conn } }
        resources:
          requests: { cpu: 200m, memory: 256Mi }
          limits:   { cpu: 1000m, memory: 512Mi }
        readinessProbe:
          httpGet: { path: /health/ready, port: 8080 }
        livenessProbe:
          httpGet: { path: /health/live, port: 8080 }
      
      # Sidecar — Outbox Publisher
      - name: outbox-publisher
        image: acrkanbanprod.azurecr.io/issue-outbox-publisher:abc123
        env:
        - name: ConnectionStrings__IssueDb
          valueFrom: { secretKeyRef: { name: issue-secrets, key: db-conn } }
        - name: ServiceBus__ConnectionString
          valueFrom: { secretKeyRef: { name: shared-secrets, key: sb-conn } }
        resources:
          requests: { cpu: 100m, memory: 128Mi }
          limits:   { cpu: 300m, memory: 256Mi }
```

**Dikkat edilecekler:**
1. **Leader election:** İki replica varsa iki publisher aynı outbox satırını publish edebilir. Çözüm:
   - PostgreSQL'de `SELECT ... FOR UPDATE SKIP LOCKED` (tercih edilen)
   - Veya **ayrı Deployment** olarak `replicas: 1` ile koş — separate deployment pattern
2. **Graceful shutdown:** `terminationGracePeriodSeconds: 60` — publisher inflight mesajları drain etsin
3. **Resource isolation:** Publisher CPU spike'ı API'yi etkilemesin

**Alternatif (önerilen prod yapı):** Dedicated outbox publisher deployment + KEDA scaling:

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: issue-outbox-publisher-scaler
spec:
  scaleTargetRef:
    name: issue-outbox-publisher
  minReplicaCount: 1
  maxReplicaCount: 3
  triggers:
  - type: postgresql
    metadata:
      query: "SELECT COUNT(*) FROM outbox_messages WHERE published_at IS NULL"
      targetQueryValue: "50"
      connectionFromEnv: PG_CONNECTION
```

---

## Phase 4 — Otomasyon ve CI/CD

### 4.1 GitHub Actions Pipeline Akışı

**Repo yapısı varsayımı:**
```
src/services/{Identity,Project,Issue,Notification,AI,Sprint}/
src/gateway/Bff/
src/frontend/web/
.github/workflows/
deploy/k8s/{base,overlays/{dev,prod}}/   # Kustomize
```

**3 katmanlı workflow stratejisi:**

**1. `ci-service.yml`** (path-filter ile sadece değişen servis build edilir):

```yaml
name: CI - Service Build

on:
  push:
    branches: [main, develop]
    paths: ['src/services/**', 'src/gateway/**']
  pull_request:

jobs:
  detect-changes:
    runs-on: ubuntu-latest
    outputs:
      services: ${{ steps.filter.outputs.changes }}
    steps:
    - uses: actions/checkout@v4
    - uses: dorny/paths-filter@v3
      id: filter
      with:
        filters: |
          identity: 'src/services/Identity/**'
          project:  'src/services/Project/**'
          issue:    'src/services/Issue/**'
          notification: 'src/services/Notification/**'
          ai:       'src/services/AI/**'
          sprint:   'src/services/Sprint/**'
          bff:      'src/gateway/Bff/**'

  build-and-push:
    needs: detect-changes
    if: ${{ needs.detect-changes.outputs.services != '[]' }}
    runs-on: ubuntu-latest
    strategy:
      matrix:
        service: ${{ fromJSON(needs.detect-changes.outputs.services) }}
    permissions:
      id-token: write   # OIDC için
      contents: read
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: '8.0.x' }
    
    - name: Test
      run: dotnet test src/services/${{ matrix.service }} --logger trx
    
    - name: Azure Login (OIDC, no secret)
      uses: azure/login@v2
      with:
        client-id: ${{ secrets.AZURE_CLIENT_ID }}
        tenant-id: ${{ secrets.AZURE_TENANT_ID }}
        subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    
    - name: ACR Login
      run: az acr login --name acrkanbanprod
    
    - name: Build & Push
      run: |
        IMAGE=acrkanbanprod.azurecr.io/${{ matrix.service }}:${{ github.sha }}
        docker build -t $IMAGE -f src/services/${{ matrix.service }}/Dockerfile .
        docker push $IMAGE
        docker tag $IMAGE acrkanbanprod.azurecr.io/${{ matrix.service }}:latest
        docker push acrkanbanprod.azurecr.io/${{ matrix.service }}:latest
    
    - name: Trivy Scan
      uses: aquasecurity/trivy-action@master
      with:
        image-ref: acrkanbanprod.azurecr.io/${{ matrix.service }}:${{ github.sha }}
        severity: 'CRITICAL,HIGH'
        exit-code: '1'
```

**2. `cd-dev.yml`** (auto deploy to dev on main):

```yaml
name: CD - Deploy Dev
on:
  workflow_run:
    workflows: ["CI - Service Build"]
    types: [completed]
    branches: [main]

jobs:
  deploy:
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    runs-on: ubuntu-latest
    environment: dev
    steps:
    - uses: actions/checkout@v4
    - uses: azure/login@v2
      with: { client-id: ..., tenant-id: ..., subscription-id: ... }
    
    - name: Set AKS context
      uses: azure/aks-set-context@v4
      with:
        resource-group: rg-kanban-platform-weu
        cluster-name: aks-kanban-dev
    
    - name: Kustomize set image
      working-directory: deploy/k8s/overlays/dev
      run: |
        kustomize edit set image \
          issue-service=acrkanbanprod.azurecr.io/issue:${{ github.sha }} \
          project-service=acrkanbanprod.azurecr.io/project:${{ github.sha }}
    
    - name: Apply
      run: kubectl apply -k deploy/k8s/overlays/dev
    
    - name: Verify rollout
      run: |
        kubectl rollout status deployment/issue-service -n kanban --timeout=5m
        kubectl rollout status deployment/project-service -n kanban --timeout=5m
```

**3. `cd-prod.yml`** (manual approval ile):

```yaml
name: CD - Deploy Prod
on:
  workflow_dispatch:
    inputs:
      git_sha:
        required: true

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production   # GitHub Environment'da approval required
    steps:
      # ... aynı dev gibi ama overlay=prod
```

**Migration step — EF Core migration:**

Production DB'ye migration'ı **deploy öncesi ayrı job**'da çalıştır:

```yaml
  ef-migrate:
    runs-on: ubuntu-latest
    steps:
    - uses: azure/login@v2
    - name: Get conn string from Key Vault
      run: |
        CONN=$(az keyvault secret show --vault-name kv-kanban-prod --name issue-db-migration-conn --query value -o tsv)
        dotnet ef database update --project src/services/Issue --connection "$CONN"
```

Migration job production deploy'undan **önce** ve **idempotent** olmalı; başarısızsa deploy abort.

---

## 5. Risk ve İzleme

### 5.1 Observability Stack

**Üç pillar:**

| Sinyal | Toplama | Saklama | Sorgulama |
|---|---|---|---|
| Logs | OTEL → Container Insights | Log Analytics Workspace (30 gün) | KQL, App Insights |
| Metrics | Prometheus metrics → Azure Monitor managed Prometheus | 18 ay | Grafana managed |
| Traces | OpenTelemetry SDK → App Insights | 90 gün | App Insights, Service Map |

**.NET servislerinde OpenTelemetry kurulumu:**

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "issue-service"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MassTransit")
        .AddAzureMonitorTraceExporter(o => 
            o.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());
```

**Distributed tracing kritik:** Issue → Service Bus → Notification → AI servis akışını tek trace ID ile takip etmek için `traceparent` header propagation. MassTransit native destekliyor.

### 5.2 Alert ve SLO Kuralları

**Önerilen alert kuralları:**

| Alert | Eşik | Action | Severity |
|---|---|---|---|
| Pod CrashLoopBackOff | 1 olay | Email + Webhook | 1 |
| API p95 latency | > 2s, 5 dk | Email | 2 |
| 5xx error rate | > 1%, 5 dk | Email + SMS | 1 |
| Outbox unpublished count | > 1000 | Webhook → Slack | 2 |
| Service Bus DLQ message count | > 10 | Email | 2 |
| DB CPU | > 80%, 10 dk | Email | 3 |
| AKS node not ready | > 5 dk | Email + Auto-remediation runbook | 1 |
| ACR pull failed | > 5 olay | Email | 2 |

**KQL örneği:**
```kql
// 5xx oranı %1'i geçerse
requests
| where timestamp > ago(5m)
| summarize 
    total = count(),
    failed = countif(resultCode startswith "5")
  by cloud_RoleName
| where failed * 100.0 / total > 1
```

### 5.3 Risk Matrisi ve Mitigation

| Risk | Olasılık | Etki | Mitigation |
|---|---|---|---|
| Azure for Students kredisi tükenmesi | Yüksek | Yüksek | Cost alert @ %50, %75, %90; Dev cluster gece scale-to-zero; Spot instances |
| Outbox duplicate publish (multi-replica) | Orta | Orta | `FOR UPDATE SKIP LOCKED`, idempotency key consumer'da |
| Migration sırasında veri kaybı | Düşük | Yüksek | DMS online mode, paralel okuma kontrolü, rollback script hazır |
| Service Bus throttling | Düşük | Orta | Standard → Premium upgrade path, retry policy MassTransit'te |
| AKS upgrade breaking change | Orta | Yüksek | Önce dev cluster'da test, blue-green node pool upgrade |
| Secrets leakage | Düşük | Kritik | Key Vault + Workload Identity, secret asla repo'da yok, gitleaks pre-commit |
| Region outage | Düşük | Kritik | Thesis kapsamı dışı — sadece DB backup geo-redundant |

**Disaster recovery checklist:**
- DB backup geo-redundant storage (RA-GRS)
- ACR replication (Premium gerekir — thesis için skip)
- Infrastructure-as-Code (Bicep/Terraform) ile cluster'ı 1 saat içinde yeniden kurabilme
- Runbook dokümantasyonu: "Identity DB down", "Service Bus DLQ overflow" senaryoları

---

## Önerilen Sıralama (Thesis Timeline)

| Hafta | Aktivite |
|---|---|
| 1 | RG, ACR, Key Vault, Log Analytics kurulumu + naming/tagging policy |
| 2 | AKS cluster (dev) + network, ingress, cert-manager |
| 3 | Identity + Project servisleri migration |
| 4 | Service Bus geçişi + Outbox Publisher sidecar/separate deployment |
| 5 | Issue + Sprint + Notification servisleri |
| 6 | AI servisi (bp-agent için ayrı node pool) |
| 7 | YARP/BFF + Frontend deploy + custom domain + TLS |
| 8 | GitHub Actions pipeline'ları + production cluster |
| 9 | Observability, alert kuralları, load testing |
| 10 | DR test, dokümantasyon, thesis defansına hazırlık |

---

## Sonraki Adımlar (Bu Plan İçin)

- [ ] Hibrit yaklaşım için **showcase deployment scope**'unu belirle (hangi servisler buluta gidecek)
- [ ] Bicep/Terraform template hazırla (manuel `az` komutları yerine reproducible IaC)
- [ ] Cost estimation tablosu çıkar (Azure Pricing Calculator export)
- [ ] [docs/erd/service-event-flow.md](erd/service-event-flow.md) bağımlılık grafiğine göre migration ordering revize et
- [ ] Mevcut [docs/bugfix-sprint-plan.md](bugfix-sprint-plan.md) ve [memory/todo_production_readiness.md](../memory/todo_production_readiness.md) Azure migration'dan önce tamamlanmalı

---

## İlgili Dokümanlar

- [docs/erd/service-event-flow.md](erd/service-event-flow.md) — Servis bağımlılık grafiği
- [docs/bugfix-sprint-plan.md](bugfix-sprint-plan.md) — Migration öncesi temizlenecek bug listesi
- [docs/ARCHITECTURE_BUG_ANALYSIS_2026-04-19.md](ARCHITECTURE_BUG_ANALYSIS_2026-04-19.md) — Mevcut mimari sorunlar
- [docs/KODTABANI_MIMARI_ANALIZ_RAPORU_2026-03-30.md](KODTABANI_MIMARI_ANALIZ_RAPORU_2026-03-30.md) — Kodbase mimari raporu
