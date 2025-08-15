using Microsoft.EntityFrameworkCore;

public class ApplicationWriteDbContext : DbContext
{
    public ApplicationWriteDbContext(DbContextOptions<ApplicationWriteDbContext> options)
        : base(options) { }
}
