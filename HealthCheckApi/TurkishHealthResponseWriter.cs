using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class TurkishHealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var entries = report.Entries.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var entry = kvp.Value;

                // description yoksa exception mesajını baz alıp TR’ye çeviriyoruz
                var descSource = entry.Description ?? entry.Exception?.Message;
                var descriptionTr = Translate(descSource);

                return new
                {
                    data = entry.Data,
                    description = descriptionTr,                //  Türkçe açıklama
                    duration = entry.Duration.ToString(),
                    exception = entry.Exception?.Message,       // orijinal hata (UI uyumu bozulmasın)
                    status = entry.Status.ToString(),           // "Healthy/Unhealthy/Degraded" KALMALI
                    tags = entry.Tags
                };
            });

        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        return context.Response.WriteAsync(json);
    }

    private static string? Translate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        var t = s;

        // Sık görülen mesajlar
        t = t.Replace("Custom SQL failed", "Özel SQL sorgusu başarısız oldu");
        t = t.Replace("App is running", "Uygulama çalışıyor");

        // SQL giriş hataları:  Login failed for user 'sa'.
        t = Regex.Replace(t, @"Login failed for user '([^']+)'",
                          m => $"'{m.Groups[1].Value}' kullanıcısı için oturum açılamadı.");

        // SQL genel bağlantı/görünür hatalar
        t = t.Replace("A network-related or instance-specific error occurred while establishing a connection to SQL Server.",
                      "SQL Server'a bağlanırken ağ veya örnek (instance) ile ilgili bir hata oluştu.");
        t = t.Replace("The server was not found or was not accessible.",
                      "Sunucu bulunamadı veya erişilemedi.");
        t = t.Replace("Cannot open database", "Veritabanı açılamıyor");

        // Redis tipik hata
        t = t.Replace(
            "It was not possible to connect to the redis server(s). Error connecting right now. To allow this multiplexer to continue retrying until it's able to connect, use abortConnect=false in your connection string or AbortOnConnectFail=false; in your code.",
            "Redis sunucusuna bağlanılamadı. Şu anda bağlantı kurulamıyor. Bağlantı kurulana kadar yeniden denemesi için connection string'e abortConnect=false ekleyebilirsiniz."
        );

        return t;
    }
}
