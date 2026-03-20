using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using MockHealthSystem.Api.Authentication;
using MockHealthSystem.Api.Middleware;
using MockHealthSystem.Api.Services;
using MockHealthSystem.Infrastructure.Data;

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
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
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
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseInMemoryDatabase("MockHealthSystemTests");
    });
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

// Global exception handling (log and return consistent error response)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Default");
app.UseAuthentication();

// Log API requests before authorization so 401s and other short-circuited responses are still logged.
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthorization();
app.MapControllers();

// Root health for backward compatibility
app.MapGet("/", () => Results.Ok("Mock Health System API is running."));

app.Run();

// Expose entry point for WebApplicationFactory in integration tests.
public partial class Program
{
}
