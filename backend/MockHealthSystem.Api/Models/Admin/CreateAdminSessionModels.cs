namespace MockHealthSystem.Api.Models.Admin;

public sealed class CreateAdminSessionRequest
{
    public string? AdminKey { get; set; }
}

public sealed class CreateAdminSessionResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
}
