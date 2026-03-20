using MockHealthSystem.Infrastructure.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MockHealthSystem.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        LoadBackendEnv();

        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
                               ?? throw new InvalidOperationException(
                                   "Postgres connection string is not configured. Set POSTGRES_CONNECTION_STRING in backend/.env (see backend/.env.example).");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Load backend/.env so POSTGRES_CONNECTION_STRING is available when running dotnet ef from various working directories.
    /// </summary>
    private static void LoadBackendEnv()
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            var candidates = new[]
            {
                Path.Combine(currentDir, ".env"),
                Path.Combine(currentDir, "..", ".env"),
                Path.Combine(currentDir, "backend", ".env")
            };
            foreach (var path in candidates)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    Env.Load(fullPath);
                    return;
                }
            }
        }
        catch
        {
            // Ignore; caller will throw if POSTGRES_CONNECTION_STRING is missing
        }
    }
}
