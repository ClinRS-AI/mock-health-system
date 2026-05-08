using MockHealthSystem.Infrastructure;
using Xunit;

namespace MockHealthSystem.Tests.Unit;

/// <summary>
/// Unit tests for the EF design-time factory that exercise the env-loading and connection-string
/// branches without requiring a live PostgreSQL instance.
/// </summary>
[Collection("EnvironmentMutating")]
public sealed class DesignTimeDbContextFactoryTests : IDisposable
{
    private readonly string? _originalConnectionString;
    private readonly string _originalCwd;
    private readonly string _tempDir;

    public DesignTimeDbContextFactoryTests()
    {
        _originalConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        _originalCwd = Directory.GetCurrentDirectory();

        _tempDir = Path.Combine(Path.GetTempPath(), $"mhs-design-time-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalCwd);
        Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", _originalConnectionString);

        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    [Fact]
    public void CreateDbContext_Throws_WhenNoConnectionStringConfigured()
    {
        using var _ = new EnvironmentVariableScope("POSTGRES_CONNECTION_STRING", null);
        Directory.SetCurrentDirectory(_tempDir);

        var factory = new DesignTimeDbContextFactory();

        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext(Array.Empty<string>()));
        Assert.Contains("POSTGRES_CONNECTION_STRING", ex.Message);
    }

    [Fact]
    public void CreateDbContext_Returns_WhenConnectionStringConfigured()
    {
        using var _ = new EnvironmentVariableScope(
            "POSTGRES_CONNECTION_STRING",
            "Host=localhost;Port=5432;Database=mhs_test;Username=u;Password=p");
        // The factory only configures Npgsql; it does not actually connect, so a placeholder
        // connection string is sufficient to verify the factory builds a DbContext.
        Directory.SetCurrentDirectory(_tempDir);

        var factory = new DesignTimeDbContextFactory();
        using var ctx = factory.CreateDbContext(Array.Empty<string>());

        Assert.NotNull(ctx);
    }

    [Fact]
    public void CreateDbContext_LoadsBackendEnv_WhenEnvFileInBackendSubdirectory()
    {
        // Simulate running `dotnet ef` from the repository root by placing the .env in backend/.
        using var _ = new EnvironmentVariableScope("POSTGRES_CONNECTION_STRING", null);

        var backendDir = Path.Combine(_tempDir, "backend");
        Directory.CreateDirectory(backendDir);
        File.WriteAllText(
            Path.Combine(backendDir, ".env"),
            "POSTGRES_CONNECTION_STRING=Host=localhost;Port=5432;Database=design_time_db;Username=u;Password=p\n");

        Directory.SetCurrentDirectory(_tempDir);

        var factory = new DesignTimeDbContextFactory();
        using var ctx = factory.CreateDbContext(Array.Empty<string>());

        Assert.NotNull(ctx);
        Assert.Contains("design_time_db",
            Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") ?? string.Empty);
    }

    [Fact]
    public void CreateDbContext_LoadsBackendEnv_WhenEnvFileInCurrentDirectory()
    {
        using var _ = new EnvironmentVariableScope("POSTGRES_CONNECTION_STRING", null);

        File.WriteAllText(
            Path.Combine(_tempDir, ".env"),
            "POSTGRES_CONNECTION_STRING=Host=localhost;Port=5432;Database=cwd_db;Username=u;Password=p\n");

        Directory.SetCurrentDirectory(_tempDir);

        var factory = new DesignTimeDbContextFactory();
        using var ctx = factory.CreateDbContext(Array.Empty<string>());

        Assert.NotNull(ctx);
        Assert.Contains("cwd_db",
            Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") ?? string.Empty);
    }
}
