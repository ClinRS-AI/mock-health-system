using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using MockHealthSystem.Api.Authentication;
using MockHealthSystem.Api.Middleware;
using MockHealthSystem.Api.RateLimiting;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Api.Services.AdminSession;
using MockHealthSystem.Api.Controllers;
using MockHealthSystem.Api.Soap;
using MockHealthSystem.Api.Swagger;
using MockHealthSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;

// Load backend/.env before building the host so ASPNETCORE_ENVIRONMENT is set for middleware (e.g. HTTPS redirect)
try
{
    var currentDir = Directory.GetCurrentDirectory();
    var envPath = Path.Combine(currentDir, ".env");
    if (!File.Exists(envPath))
    {
        envPath = Path.Combine(currentDir, "..", ".env");
    }
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
    }
}
catch
{
    // Ignore; app will use appsettings and system environment variables
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("health-api", new OpenApiInfo
    {
        Title = "Mock Health System API",
        Version = "v1",
        Description = "External-facing mock healthcare API. Exposes patients, conditions, medications, procedures, and supporting reference data. Supports configurable authentication modes: None, Bearer, CCAPIKey, and OAuth (client credentials)."
    });
    options.SwaggerDoc("admin-api", new OpenApiInfo
    {
        Title = "Mock Health System — Admin API",
        Version = "v1",
        Description = "Internal administration endpoints for managing auth configuration, monitoring request logs, generating synthetic test data, and minting short-lived admin session tokens."
    });

    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (apiDesc.ActionDescriptor is not ControllerActionDescriptor cad) return false;
        var controller = cad.ControllerTypeInfo.AsType();
        var isAdmin = controller == typeof(AdminSessionsController)
                   || controller == typeof(AuthSettingsController)
                   || controller == typeof(MonitoringController)
                   || controller == typeof(TestDataController);
        return docName == (isAdmin ? "admin-api" : "health-api");
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition(SwaggerSecuritySchemeNames.Bearer, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT or shared token",
        Description = "Bearer or OAuth access token (when auth mode is Bearer or OAuth)."
    });

    options.AddSecurityDefinition(SwaggerSecuritySchemeNames.CcApiKey, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "CCAPIKey",
        Description = "Shared API key when auth mode is CCAPIKey."
    });

    options.AddSecurityDefinition(SwaggerSecuritySchemeNames.AdminSession, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Admin-Session",
        Description = "Short-lived admin JWT from POST /api/v1/admin/sessions."
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
    options.DocumentFilter<DocumentSecuritySchemesFilter>();
});
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

if (builder.Environment.IsEnvironment("Testing"))
{
    var useSqliteForTests = builder.Configuration.GetValue<bool>("Testing:UseSqlite");
    if (useSqliteForTests)
    {
        var sqliteConnectionString = builder.Configuration["Testing:SqliteConnectionString"];
        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            throw new InvalidOperationException("Testing:SqliteConnectionString is required when Testing:UseSqlite is enabled.");
        }

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(sqliteConnectionString);
        });
    }
    else
    {
        var testDatabaseName = builder.Configuration["Testing:InMemoryDatabaseName"] ?? "MockHealthSystemTests";
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase(testDatabaseName);
        });
    }
}
else
{
    var connectionString =
        Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Postgres connection string is not configured. Set POSTGRES_CONNECTION_STRING in backend/.env (see backend/.env.example).");

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
    });
}

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<MockHealthSystem.Api.Filters.ModelValidationActionFilter>();
});
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IAuthSettingsService, AuthSettingsService>();
builder.Services.AddScoped<IReportExecutionService, ReportExecutionService>();
builder.Services.AddScoped<IReportSoapService, ReportSoapService>();

builder.Services.Configure<AdminSessionOptions>(
    builder.Configuration.GetSection(AdminSessionOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAdminSessionJwtService, AdminSessionJwtService>();
builder.Services.AddSingleton<IAdminRequestValidator, AdminRequestValidator>();
builder.Services.AddSingleton<IRateLimitCounterStore, RateLimitCounterStore>();

builder.Services.AddAuthentication("Mock")
    .AddScheme<AuthenticationSchemeOptions, MockAuthHandler>("Mock", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("Mock")
        .RequireAuthenticatedUser()
        .Build();
});

// Honor explicit URL configuration from environment (.env or host)
var urls =
    Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    ?? Environment.GetEnvironmentVariable("BACKEND_URL");
if (!string.IsNullOrWhiteSpace(urls))
{
    builder.WebHost.UseUrls(urls);
}

var app = builder.Build();

// When migrations run after the server starts listening (Cloud Run startup), block HTTP traffic
// until MigrateAsync completes so auth/DB middleware never hits a half-migrated schema.
var deferDatabaseUntilMigrated =
    !app.Environment.IsEnvironment("Testing") &&
    string.Equals(Environment.GetEnvironmentVariable("APPLY_EFMIGRATIONS_ON_STARTUP"), "true", StringComparison.OrdinalIgnoreCase);

if (!deferDatabaseUntilMigrated)
{
    Program.HttpTrafficAllowed = true;
}

// Global exception handling (log and return consistent error response)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (deferDatabaseUntilMigrated)
{
    app.Use(async (context, next) =>
    {
        if (!Program.HttpTrafficAllowed)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "5";
            await context.Response.WriteAsync("Database migrations in progress.", context.RequestAborted);
            return;
        }

        await next(context);
    });
}

if (!app.Environment.IsDevelopment())
{
    // Cloud Run terminates TLS; the container sees HTTP. Trust X-Forwarded-Proto for HTTPS redirects and link generation.
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

var enableSwagger =
    app.Environment.IsDevelopment() ||
    string.Equals(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/health-api/swagger.json", "Mock Health System API");
        options.SwaggerEndpoint("/swagger/admin-api/swagger.json", "Admin API");
        options.EnablePersistAuthorization();
    });
}

app.UseCors("Default");
app.UseAuthentication();

// Log API requests before authorization so 401s and other short-circuited responses are still logged.
app.UseMiddleware<RequestLoggingMiddleware>();

// Rate limiting runs after logging (so 429s are recorded) and before authorization.
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthorization();
app.MapControllers();

// Root health for backward compatibility (explicit opt-out of fallback authorization)
app.MapGet("/", () => Results.Ok("Mock Health System API is running.")).AllowAnonymous();

// Start listening before EF migrations so Cloud Run's startup probe sees PORT open. Migrations can take longer than the startup timeout.
await app.StartAsync();
try
{
    if (deferDatabaseUntilMigrated)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogInformation("Applying EF Core database migrations...");
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        Program.HttpTrafficAllowed = true;
        logger.LogInformation("EF Core database migrations completed; HTTP traffic is now allowed.");
    }
}
catch
{
    await app.StopAsync();
    throw;
}

await app.WaitForShutdownAsync();

// Expose entry point for WebApplicationFactory in integration tests.
public partial class Program
{
    /// <summary>
    /// When startup defers HTTP until EF migrations finish, middleware returns 503 until migrations set this to true.
    /// Use a volatile field so request threads always see the flag without relying on TaskCompletionSource completion edge cases.
    /// </summary>
    internal static volatile bool HttpTrafficAllowed;
}
