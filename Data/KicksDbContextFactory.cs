using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KicksMvc.Data;

// Explicit tooling construction; no connection is opened until an authorized database command runs.
public sealed class KicksDbContextFactory : IDesignTimeDbContextFactory<KicksDbContext>
{
    public KicksDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection before using EF tools.");
        return new KicksDbContext(new DbContextOptionsBuilder<KicksDbContext>().UseSqlServer(connection).Options);
    }
}
