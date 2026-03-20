namespace MockHealthSystem.Api.Models.Auth;

public sealed class AuthSettingsViewModel
{
    public string Mode { get; set; } = "None";
    public string? BearerToken { get; set; }
    public string? OAuthClientId { get; set; }
    public string? OAuthClientSecret { get; set; }
    public int AccessTokenLifetimeMinutes { get; set; }
    public int RefreshTokenLifetimeDays { get; set; }
    public bool HasAnyTokens { get; set; }
}

public sealed class AuthSettingsUpdateModel
{
    public string Mode { get; set; } = "None";
    public string? BearerToken { get; set; }
    public string? OAuthClientId { get; set; }
    public string? OAuthClientSecret { get; set; }
    public int? AccessTokenLifetimeMinutes { get; set; }
    public int? RefreshTokenLifetimeDays { get; set; }
}

