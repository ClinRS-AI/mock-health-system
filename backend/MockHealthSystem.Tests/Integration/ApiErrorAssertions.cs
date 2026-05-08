using System.Net;
using System.Text.Json;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

internal static class ApiErrorAssertions
{
    public static async Task AssertApiErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        string? titleContains = null,
        string? detailContains = null)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);

        var rawBody = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            Assert.True(expectedStatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
            return;
        }

        ApiErrorDto? payload = null;
        if (rawBody.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            payload = JsonSerializer.Deserialize<ApiErrorDto>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        if (payload is not null)
        {
            Assert.Equal((int)expectedStatusCode, payload.Status);
            Assert.False(string.IsNullOrWhiteSpace(payload.Title));
            Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
        }

        if (!string.IsNullOrWhiteSpace(titleContains))
        {
            Assert.Contains(titleContains, payload?.Title ?? rawBody, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(detailContains))
        {
            Assert.Contains(detailContains, payload?.Detail ?? rawBody, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class ApiErrorDto
    {
        public int Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public string? TraceId { get; set; }
    }
}
