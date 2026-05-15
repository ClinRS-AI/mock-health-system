using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using MockHealthSystem.Api.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MockHealthSystem.Api.Swagger;

/// <summary>
/// Strips security scheme definitions that don't belong to the current document.
/// Health API doc keeps Bearer/CCAPIKey; Admin API doc keeps AdminSession.
/// </summary>
internal sealed class DocumentSecuritySchemesFilter : IDocumentFilter
{
    private static readonly HashSet<Type> AdminControllers =
    [
        typeof(AdminSessionsController),
        typeof(AuthSettingsController),
        typeof(MonitoringController),
        typeof(TestDataController),
    ];

    private static readonly HashSet<string> HealthSchemes =
        [SwaggerSecuritySchemeNames.Bearer, SwaggerSecuritySchemeNames.CcApiKey];

    private static readonly HashSet<string> AdminSchemes =
        [SwaggerSecuritySchemeNames.AdminSession];

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var isAdminDoc = context.ApiDescriptions.Any(d =>
            d.ActionDescriptor is ControllerActionDescriptor cad &&
            AdminControllers.Contains(cad.ControllerTypeInfo.AsType()));

        var schemesToKeep = isAdminDoc ? AdminSchemes : HealthSchemes;

        var toRemove = swaggerDoc.Components?.SecuritySchemes?.Keys
            .Where(k => !schemesToKeep.Contains(k))
            .ToList() ?? [];

        foreach (var key in toRemove)
            swaggerDoc.Components!.SecuritySchemes.Remove(key);
    }
}
