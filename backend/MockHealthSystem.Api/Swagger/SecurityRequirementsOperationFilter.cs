using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MockHealthSystem.Api.Swagger;

internal sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;
        var declaringType = methodInfo.DeclaringType;

        var requiresAdmin =
            methodInfo.IsDefined(typeof(RequiresAdminAuthAttribute), inherit: true) ||
            (declaringType?.IsDefined(typeof(RequiresAdminAuthAttribute), inherit: true) ?? false);

        if (requiresAdmin)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [SchemeRef(SwaggerSecuritySchemeNames.AdminSession)] = []
                },
            ];
            return;
        }

        var allowsAnonymous =
            methodInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true) ||
            (declaringType?.IsDefined(typeof(AllowAnonymousAttribute), inherit: true) ?? false);

        if (allowsAnonymous)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [SchemeRef(SwaggerSecuritySchemeNames.Bearer)] = []
            },
            new OpenApiSecurityRequirement
            {
                [SchemeRef(SwaggerSecuritySchemeNames.CcApiKey)] = []
            },
        ];
    }

    private static OpenApiSecurityScheme SchemeRef(string schemeId) =>
        new() { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schemeId } };
}
