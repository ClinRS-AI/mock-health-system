namespace MockHealthSystem.Api.Services.AdminSession;

public sealed class AdminRequestValidator : IAdminRequestValidator
{
    private const string AdminKeyHeader = "X-Admin-Key";
    private const string AdminSessionHeader = "X-Admin-Session";

    private readonly IAdminSessionJwtService _adminSessionJwt;

    public AdminRequestValidator(IAdminSessionJwtService adminSessionJwt)
    {
        _adminSessionJwt = adminSessionJwt;
    }

    public bool IsAdminRequest(HttpContext httpContext, bool bypassAdminChecksInDevelopment)
    {
        if (bypassAdminChecksInDevelopment &&
            string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var requiredKey = Environment.GetEnvironmentVariable("AUTH_SETTINGS_ADMIN_KEY");
        if (string.IsNullOrWhiteSpace(requiredKey))
        {
            return true;
        }

        if (httpContext.Request.Headers.TryGetValue(AdminKeyHeader, out var keyHeader))
        {
            var provided = keyHeader.ToString();
            if (string.Equals(provided, requiredKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (httpContext.Request.Headers.TryGetValue(AdminSessionHeader, out var sessionHeader))
        {
            var token = sessionHeader.ToString().Trim();
            if (_adminSessionJwt.TryValidateSessionToken(token, out _))
            {
                return true;
            }
        }

        return false;
    }
}
