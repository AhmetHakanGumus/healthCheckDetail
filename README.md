# HealthCheckApi

.NET 8 tabanlı **health gateway**. Uygulamanın ve kritik bağımlılıkların (SQL Server, Redis) durumunu JSON ve görsel arayüz olarak sunar.

## Neler kontrol edilir?

* **self**: Uygulama ayakta mı? (liveness)
* **sqlserver**: SQL Server’a bağlantı
* **custom-sql**: Basit SQL sorgusu (`SELECT 1`)
* **ApplicationWriteDbContext**: EF Core DbContext ile erişim
* **redis**: Redis’e bağlantı (PING)

## Endpoint’ler

* `GET /` → `health-ui`’ya yönlendirir
* `GET /health/live` → yalnızca **liveness**
* `GET /health/ready` → servisler **hazır** mı (JSON)
* `GET /health/details` → ayrıntılı JSON (TR açıklamalar)
* `GET /health-ui` → görsel arayüz

> Durumlar: **Healthy / Degraded / Unhealthy**. En kötü sonuç genel durumu belirler.

## Gereksinimler

* **.NET 8 SDK**
* **SQL Server** (Express/Developer) – `DemoDb` veritabanı
* **Redis** (örn. **Memurai** – Windows için, veya uzak bir Redis)

## Kurulum

1. **Ayarlar (`appsettings.json`)**

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost,1433;Database=DemoDb;User Id=sa;Password=YourStrongPw;Encrypt=False;TrustServerCertificate=True",
    "Redis": "localhost:6379"
  },
  "HealthChecksUI": {
    "EvaluationTimeInSeconds": 15,
    "MaximumHistoryEntriesPerEndpoint": 60,
    "HealthChecks": [{ "Name": "All details", "Uri": "/health/details" }]
  }
}
```

2. **Veritabanı**

* SSMS:

```sql
IF DB_ID('DemoDb') IS NULL CREATE DATABASE DemoDb;
```

*(Ya da uygulamada `EnsureCreated/Migrate` kullanabilirsin.)*

3. **Paketler** (zaten ekliyse atla)

```
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.Redis
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.Client
```

4. **Çalıştır**

```
dotnet restore
dotnet run --launch-profile https
```

Arayüz: `https://localhost:<port>/health-ui`

## Mimari notlar

* **TurkishHealthResponseWriter** ile `/health/ready` ve `/health/details` çıkışı **Türkçe açıklamalar** döner.
* Etiketler: `live` (liveness), `ready` (hazırlık). Kubernetes/Load Balancer bu ayrımı kullanabilir.

## Sorun giderme (kısa)

* **Login failed for user 'sa'** → Mixed Mode açık mı, `sa` etkin mi, parola doğru mu? `localhost,1433` kullandığından emin ol.
* **Redis bağlanamıyor** → Servis çalışıyor mu (6379)? Memurai/Redis’te parola varsa connection string’e `password=` ekle.
* **UI boş** → Endpoint’lerin 200 dönüp dönmediğini kontrol et (`/health/details`).

## Güvenlik (prod)

Health endpoint’lerini **iç ağa** sınırla veya **kimlik doğrulama/IP kısıtı** ekle. Hata/çevresel bilgileri dışarı açma.
