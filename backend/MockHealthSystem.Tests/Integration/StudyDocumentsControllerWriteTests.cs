using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockHealthSystem.Infrastructure.Data;
using MockHealthSystem.Infrastructure.Data.Entities;
using Xunit;

namespace MockHealthSystem.Tests.Integration;

public sealed class StudyDocumentsControllerWriteTests : IClassFixture<IsolatedWebApplicationFactory>
{
    private readonly IsolatedWebApplicationFactory _factory;

    public StudyDocumentsControllerWriteTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDocument_Returns201_AndCreatesInitialStatusHistoryRow()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Document Write Study");
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync($"/api/v1/studies/{studyId}/documents", new { statusName = "Draft" });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var documentId = doc.RootElement.GetProperty("id").GetInt32();

        var historyResp = await client.GetAsync($"/api/v1/studies/{studyId}/documents/{documentId}/history");
        using var historyDoc = JsonDocument.Parse(await historyResp.Content.ReadAsStringAsync());
        Assert.Single(historyDoc.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task UpdateDocument_StatusChange_AppendsHistoryRow()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Document Update Study");
        var documentId = await SeedDocumentAsync(studyId, "Draft");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/documents/{documentId}", new { statusName = "Approved" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var historyResp = await client.GetAsync($"/api/v1/studies/{studyId}/documents/{documentId}/history");
        using var historyDoc = JsonDocument.Parse(await historyResp.Content.ReadAsStringAsync());
        Assert.Equal(2, historyDoc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task UpdateDocument_NoStatusChange_DoesNotAppendHistoryRow()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Document NoChange Study");
        var documentId = await SeedDocumentAsync(studyId, "Draft");
        var client = _factory.CreateClient();

        var resp = await client.PutAsJsonAsync($"/api/v1/studies/{studyId}/documents/{documentId}", new { statusName = "Draft", description = "Updated desc" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var historyResp = await client.GetAsync($"/api/v1/studies/{studyId}/documents/{documentId}/history");
        using var historyDoc = JsonDocument.Parse(await historyResp.Content.ReadAsStringAsync());
        Assert.Single(historyDoc.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task DeleteDocument_RemovesRecord()
    {
        var studyId = await StudySeedHelpers.SeedStudyAsync(_factory, "Document Delete Study");
        var documentId = await SeedDocumentAsync(studyId, "Draft");
        var client = _factory.CreateClient();

        var resp = await client.DeleteAsync($"/api/v1/studies/{studyId}/documents/{documentId}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/v1/studies/{studyId}/documents/{documentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    private async Task<int> SeedDocumentAsync(int studyId, string statusName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var document = new StudyDocument { Uid = Guid.NewGuid(), StudyId = studyId, StatusName = statusName };
        db.StudyDocuments.Add(document);
        await db.SaveChangesAsync();
        db.StudyDocumentStatusHistories.Add(new StudyDocumentStatusHistory { StudyDocumentId = document.Id, StatusName = statusName, ChangedOn = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return document.Id;
    }
}
