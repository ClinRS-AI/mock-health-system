namespace MockHealthSystem.Api.Swagger;

/// <summary>
/// Marks an endpoint as requiring an X-Admin-Session JWT in Swagger UI.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresAdminAuthAttribute : Attribute;
