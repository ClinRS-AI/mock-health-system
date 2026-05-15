namespace MockHealthSystem.Api.Swagger;

/// <summary>
/// Marks an endpoint as requiring admin credentials (X-Admin-Key or X-Admin-Session) in Swagger UI.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresAdminAuthAttribute : Attribute;
