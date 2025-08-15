using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class CustomSqlHealthCheck : IHealthCheck
{
    private readonly string _conn;
    private readonly string _sql;
    public CustomSqlHealthCheck(string conn, string sql = "SELECT 1")
    {
        _conn = conn; _sql = sql;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new SqlConnection(_conn);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(_sql, conn);
            await cmd.ExecuteScalarAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Custom SQL failed", ex);
        }
    }
}
