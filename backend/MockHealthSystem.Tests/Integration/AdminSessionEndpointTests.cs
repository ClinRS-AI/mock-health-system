using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

[Collection("EnvironmentMutating")]
public sealed class AdminSessionEndpointTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public AdminSessionEndpointTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostAdminSession_Returns400_WhenAdminKeyNotConfigured()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/admin/sessions", new { adminKey = "anything" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task PostAdminSession_Returns403_WhenAdminKeyWrong()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "correct-secret");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/admin/sessions", new { adminKey = "wrong" });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task PostAdminSession_ReturnsToken_ThenAuthSettingsAcceptsXAdminSession()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "mint-test-key");
        var client = _factory.CreateClient();

        var mintResp = await client.PostAsJsonAsync("/api/v1/admin/sessions", new { adminKey = "mint-test-key" });
        mintResp.EnsureSuccessStatusCode();
        var mintBody = await mintResp.Content.ReadFromJsonAsync<AdminSessionMintResponseDto>();
        Assert.False(string.IsNullOrEmpty(mintBody?.AccessToken));

        using var get = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth-settings");
        get.Headers.Add("X-Admin-Session", mintBody!.AccessToken);
        var getResp = await client.SendAsync(get);

        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    [Fact]
    public async Task AuthSettingsGet_Returns403_WhenXAdminSessionIsTampered()
    {
        using var _ = new EnvironmentVariableScope("AUTH_SETTINGS_ADMIN_KEY", "tamper-test-key");
        var client = _factory.CreateClient();

        var mintResp = await client.PostAsJsonAsync("/api/v1/admin/sessions", new { adminKey = "tamper-test-key" });
        mintResp.EnsureSuccessStatusCode();
        var mintBody = await mintResp.Content.ReadFromJsonAsync<AdminSessionMintResponseDto>();
        var token = mintBody!.AccessToken;
        var tampered = token.Length > 4 ? token[..^1] + (token[^1] == 'a' ? 'b' : 'a') : token + "x";

        using var get = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth-settings");
        get.Headers.Add("X-Admin-Session", tampered);
        var getResp = await client.SendAsync(get);

        Assert.Equal(HttpStatusCode.Forbidden, getResp.StatusCode);
    }

    private sealed class AdminSessionMintResponseDto
    {
        public string AccessToken { get; set; } = "";
        public DateTime ExpiresAtUtc { get; set; }
    }
}
