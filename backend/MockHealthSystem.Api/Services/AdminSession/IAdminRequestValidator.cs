namespace MockHealthSystem.Api.Services.AdminSession;

public interface IAdminRequestValidator
{
    /// <param name="httpContext">Current HTTP context (headers).</param>
    /// <param name="bypassAdminChecksInDevelopment">
    /// When true, allows all requests in Development environment (matches legacy TestDataController behavior).
    /// </param>
    bool IsAdminRequest(HttpContext httpContext, bool bypassAdminChecksInDevelopment);
}
